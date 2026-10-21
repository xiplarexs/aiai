using System;
using System.Text.RegularExpressions;

namespace KonuBot
{
    /// <summary>
    /// /api/bot validasyon kurallarını sunucuyla birebir uygular.
    /// Hatalı post'u API'ye göndermeden önce yakalar, kullanıcıya bildirir.
    /// </summary>
    public static class PostValidator
    {
        // Maksimumlar — sunucuyla eşleşir
        private const int TitleMin       = 5;
        private const int TitleMax       = 500;
        private const int ContentWordMin = 200;
        private const int ContentCharMax = 100_000;
        private const int DescMax        = 500;
        private const int TagsMax        = 10;
        private const int TagCharMax     = 80;
        private const int SlugCharMax    = 255; // Sunucu (api/bot) uzun slug'ları kabul ediyor; 60 sabiti yerine 255
        private const int CoverUrlMax    = 2048;

        // Sunucu taraması doğrulanmış gerçek kurallara göre gevşetildi:
        // - sunucu 60 karakteri aşan slug'ları reddetmez (91 karakterli slug testte kabul edildi)
        // - sunucu "izmir-web-tasarim-hizmeti-xipsoft--mp4..." gibi çift tireli slug'ları reddetmez
        private static readonly Regex SlugRegex     = new(@"^[a-z0-9-]+$", RegexOptions.Compiled);
        private static readonly Regex PrivateIpv4    = new(@"^(127\.|10\.|172\.(1[6-9]|2\d|3[01])\.|192\.168\.|169\.254\.|0\.)", RegexOptions.Compiled);

        /// <summary>
        /// Tek bir BlogPost'u doğrular.
        /// Tüm kurallar kontrol edilir; ilk hatada durulmaz.
        /// </summary>
        public static ValidationResult Validate(BlogPost post, int index)
        {
            var result = new ValidationResult();
            string label = $"[{index + 1}]";

            // ── title ────────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(post.Title))
            {
                result.AddError($"{label} title zorunludur.");
            }
            else
            {
                int len = post.Title.Trim().Length;
                if (len < TitleMin || len > TitleMax)
                    result.AddError($"{label} title {TitleMin}–{TitleMax} karakter olmalıdır. (şu an: {len})");
            }

            // ── content ──────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(post.Content))
            {
                result.AddError($"{label} content zorunludur.");
            }
            else
            {
                string trimmed   = post.Content.Trim();
                int charLen      = trimmed.Length;
                int wordCount    = CountWords(trimmed);

                if (wordCount < ContentWordMin)
                    result.AddError($"{label} content en az {ContentWordMin} kelime olmalıdır. (şu an: {wordCount} kelime)");

                if (charLen > ContentCharMax)
                    result.AddError($"{label} content max {ContentCharMax:N0} karakter olabilir. (şu an: {charLen:N0})");
            }

            // ── description (opsiyonel) ───────────────────────────────────────
            if (post.Description != null && post.Description.Length > DescMax)
                result.AddError($"{label} description max {DescMax} karakter olabilir. (şu an: {post.Description.Length})");

            // ── tags (opsiyonel) ─────────────────────────────────────────────
            if (post.Tags != null)
            {
                if (post.Tags.Count > TagsMax)
                    result.AddError($"{label} tags listesi en fazla {TagsMax} eleman içerebilir. (şu an: {post.Tags.Count})");

                for (int i = 0; i < post.Tags.Count; i++)
                {
                    var tag = post.Tags[i];
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        result.AddError($"{label} tags[{i}] boş olamaz.");
                        continue;
                    }
                    if (tag.Length > TagCharMax)
                        result.AddError($"{label} tags[{i}] max {TagCharMax} karakter olabilir. (\"{Truncate(tag, 30)}\")");
                }
            }

            // ── cover_image_url (opsiyonel) ───────────────────────────────────
            if (!string.IsNullOrEmpty(post.CoverImageUrl))
            {
                string url = post.CoverImageUrl;

                if (url.Length > CoverUrlMax)
                {
                    result.AddError($"{label} cover_image_url max {CoverUrlMax} karakter olabilir.");
                }
                else if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    result.AddError($"{label} cover_image_url geçerli bir URL değil.");
                }
                else if (uri.Scheme != "https")
                {
                    result.AddError($"{label} cover_image_url sadece HTTPS desteklenir. (şu an: {uri.Scheme})");
                }
                else if (uri.Port != 443 && uri.Port != -1)
                {
                    result.AddError($"{label} cover_image_url 443 dışı port kullanılamaz. (port: {uri.Port})");
                }
                else if (IsPrivateHost(uri.Host))
                {
                    result.AddError($"{label} cover_image_url iç ağ adresi olamaz. (host: {uri.Host})");
                }
            }

            // ── cover_image (opsiyonel, base64 data URL) ─────────────────────
            if (!string.IsNullOrEmpty(post.CoverImage))
            {
                if (!post.CoverImage.StartsWith("data:image"))
                {
                    result.AddError($"{label} cover_image gecerli bir data URL olmalidir (ornek: data:image/jpeg;base64,...)");
                }
                if (!string.IsNullOrEmpty(post.CoverImageUrl))
                {
                    result.AddError($"{label} cover_image ve cover_image_url ayni anda gonderilemez");
                }
            }

            // ── slug (opsiyonel) ──────────────────────────────────────────────
            if (!string.IsNullOrEmpty(post.Slug))
            {
                string slug = post.Slug;
                if (slug.Length > SlugCharMax)
                    result.AddError($"{label} slug max {SlugCharMax} karakter olabilir. (şu an: {slug.Length})");
                else if (!SlugRegex.IsMatch(slug))
                    result.AddError($"{label} slug sadece küçük harf, rakam ve tire içerebilir: \"{Truncate(slug, 40)}\"");
            }

            // ── published_at (opsiyonel, sadece is_published=true iken anlamlı) ──
            if (post.IsPublished && post.PublishedAt.HasValue)
            {
                // DateTime? zaten C# tarafında doğru parse edilmişse sorun yok.
                // Sadece aşırı eski/gelecek tarih uyarısı ver (zorunlu hata değil)
                var diff = post.PublishedAt.Value - DateTime.UtcNow;
                if (diff.TotalDays > 365)
                    result.AddError($"{label} published_at 1 yıldan fazla ileri bir tarih: {post.PublishedAt.Value:yyyy-MM-dd}");
            }

            return result;
        }

        // ── Yardımcılar ───────────────────────────────────────────────────────

        private static int CountWords(string text)
        {
            // Sunucu, kelime sayımından önce HTML tag'lerini boşluk EKLEMEDEN kaldırır (PHP strip_tags davranışı).
            // Bu yüzden tag'lere bitişik kelimeler birleşir ve sayım client'ta daha düşük çıkabilir.
            // Sayımın sunucuyla birebir eşleşmesi için burada da boşluk eklemiyoruz.
            string plain = Regex.Replace(text, @"<[^>]+>", "");
            return Regex.Matches(plain, @"\S+").Count;
        }

        private static bool IsPrivateHost(string host)
        {
            host = host.ToLowerInvariant();
            if (host == "localhost") return true;
            if (host.EndsWith(".local") || host.EndsWith(".internal") || host.EndsWith(".corp"))
                return true;
            if (host == "::1") return true;
            if (PrivateIpv4.IsMatch(host)) return true;
            return false;
        }

        private static string Truncate(string s, int max)
            => s.Length > max ? s[..max] + "…" : s;
    }
}
