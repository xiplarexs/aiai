using System;
using System.Text;
using System.Text.RegularExpressions;

namespace KonuBot
{
    /// <summary>
    /// Türkçe karakterleri ASCII karşılıklarına dönüştürerek
    /// URL-uyumlu slug üretir.
    /// </summary>
    public static class SlugHelper
    {
        private static readonly (string From, string To)[] TurkishMap =
        {
            ("ğ", "g"), ("Ğ", "g"),
            ("ü", "u"), ("Ü", "u"),
            ("ş", "s"), ("Ş", "s"),
            ("ı", "i"), ("I", "i"),
            ("İ", "i"), ("i", "i"),
            ("ö", "o"), ("Ö", "o"),
            ("ç", "c"), ("Ç", "c"),
        };

        private const int MaxSlugLength = 60;

        /// <summary>
        /// Verilen başlıktan URL-safe slug üretir.
        /// Örn: "Next.js 15 ile Modern Web Geliştirme" → "nextjs-15-ile-modern-web-gelistirme"
        /// Üretilen slug <see cref="MaxSlugLength"/> karakterle sınırlıdır.
        /// </summary>
        public static string Generate(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return $"post-{DateTime.UtcNow.Ticks}";

            string slug = title.Trim().ToLowerInvariant();

            // Türkçe karakterleri dönüştür
            foreach (var (from, to) in TurkishMap)
                slug = slug.Replace(from, to);

            // Harf/rakam dışındaki her şeyi tire ile değiştir
            slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");

            // Baş ve sondaki tireleri temizle
            slug = slug.Trim('-');

            // Çift tireleri tekli yap
            slug = Regex.Replace(slug, @"-{2,}", "-");

            // 60 karakter limitini uygula; kırpılan yerde tire kalmasın
            if (slug.Length > MaxSlugLength)
                slug = slug[..MaxSlugLength].TrimEnd('-');

            return slug;
        }
    }
}
