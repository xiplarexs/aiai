using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KonuBot
{
    /// <summary>
    /// /api/bot/posts/bulk endpoint'inin beklediği tam veri modeli.
    /// Alan adları ve kısıtlamalar sunucu validasyonuyla birebir eşleşir.
    /// </summary>
    public class BlogPost
    {
        // ── Zorunlu alanlar ───────────────────────────────────────────────

        /// <summary>5 – 500 karakter. Zorunlu.</summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Min 200 kelime, max 100.000 karakter. Zorunlu. Düz metin veya HTML kabul edilir.</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        // ── Opsiyonel alanlar ─────────────────────────────────────────────

        /// <summary>SEO açıklaması. Max 500 karakter. Opsiyonel.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Max 10 etiket. Her biri max 80 karakter. Opsiyonel.</summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>Kapak görseli. Sadece HTTPS URL kabul edilir. Opsiyonel.</summary>
        [JsonPropertyName("cover_image_url")]
        public string? CoverImageUrl { get; set; }

        /// <summary>Kapak görseli (base64 data URL). ImgBB'ye otomatik yüklenir. Opsiyonel.</summary>
        [JsonPropertyName("cover_image")]
        public string? CoverImage { get; set; }

        /// <summary>true = published, false/null = draft. Varsayılan: false.</summary>
        [JsonPropertyName("is_published")]
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Özel URL slug'ı. Boş bırakılırsa sunucu title'dan üretir.
        /// Max 60 karakter, sadece a-z 0-9 ve tire.
        /// </summary>
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        /// <summary>
        /// ISO 8601 yayın tarihi. Sadece is_published=true iken geçerli.
        /// null ise sunucu o anki zamanı kullanır.
        /// </summary>
        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
    }

    /// <summary>
    /// Gönderim öncesi C# tarafında yapılan validasyon sonucu.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new();

        public void AddError(string msg) => Errors.Add(msg);
    }
}
