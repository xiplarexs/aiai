using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace KonuBot
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        private const int DelayBetweenPostsMs = 300;

        public ApiService(string apiKey, string baseUrl = "https://xipsoft.net")
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
                Timeout     = TimeSpan.FromSeconds(120)
            };

            _http.DefaultRequestHeaders.Add("X-Bot-Key", apiKey);
            _http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented               = false,
                DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task SendPostsAsync(
            List<BlogPost> posts,
            IProgress<(int sent, int total, string message, bool success)> progress,
            CancellationToken ct = default,
            Func<BlogPost, bool, Task>? onPostResolved = null)
        {
            if (posts.Count == 0)
                throw new InvalidOperationException("Gönderilecek post yok.");

            int total = posts.Count;

            foreach (var post in posts)
            {
                if (string.IsNullOrWhiteSpace(post.Slug))
                    post.Slug = null;

                if (!post.IsPublished)
                    post.PublishedAt = null;
            }

            var validationErrors = new List<string>();
            for (int i = 0; i < posts.Count; i++)
            {
                var vr = PostValidator.Validate(posts[i], i);
                if (!vr.IsValid)
                    validationErrors.AddRange(vr.Errors);
            }

            if (validationErrors.Count > 0)
            {
                foreach (var err in validationErrors)
                    progress.Report((0, total, $"[VALIDASYON HATASI] {err}", false));

                throw new InvalidOperationException(
                    $"{validationErrors.Count} validasyon hatası bulundu. Gönderim iptal edildi.");
            }

            progress.Report((0, total, $"Validasyon başarılı. {total} post gönderilmeye hazır.", true));

            int sent = 0;

            for (int i = 0; i < posts.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var post = posts[i];
                progress.Report((sent, total,
                    $"[{i + 1}/{total}] \"{TruncateTitle(post.Title)}\" gönderiliyor…", true));

                try
                {
                    var payload = new Dictionary<string, object>
                    {
                        ["type"] = "Blog",
                        ["title"] = post.Title,
                        ["content"] = post.Content,
                        ["publish"] = post.IsPublished,
                    };

                    if (!string.IsNullOrEmpty(post.Description))
                        payload["description"] = post.Description;

                    if (post.Tags != null && post.Tags.Count > 0)
                        payload["tags"] = post.Tags;

                    if (!string.IsNullOrEmpty(post.CoverImageUrl))
                        payload["cover_image_url"] = post.CoverImageUrl;

                    if (!string.IsNullOrEmpty(post.CoverImage))
                        payload["cover_image"] = post.CoverImage;

                    if (!string.IsNullOrEmpty(post.Slug))
                        payload["slug"] = post.Slug;

                    if (post.IsPublished && post.PublishedAt.HasValue)
                        payload["published_at"] = post.PublishedAt.Value.ToString("o");

                    string bodyJson = JsonSerializer.Serialize(payload, _jsonOptions);
                    using var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var response = await _http.PostAsync("api/bot", content, ct);

                    int statusCode = (int)response.StatusCode;
                    string rawBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        sent++;
                        var result = JsonSerializer.Deserialize<JsonElement>(rawBody);
                        string slug = result.TryGetProperty("slug", out var s) ? s.GetString() ?? "" : "";
                        string url = result.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                        string status = result.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";

                        progress.Report((sent, total,
                            $"[OK {statusCode}] \"{TruncateTitle(post.Title)}\" → /blog/{slug} ({status})",
                            true));
                        if (onPostResolved != null)
                            await onPostResolved(post, false);
                    }
                    else
                    {
                        string errorMsg = TryExtractError(rawBody);
                        bool isDuplicate = statusCode == 409 &&
                            (errorMsg.Contains("DUPLICATE_SLUG", StringComparison.OrdinalIgnoreCase) ||
                             errorMsg.Contains("zaten mevcut", StringComparison.OrdinalIgnoreCase));
                        string prefix = isDuplicate ? "[DUPLICATE]" : "[HATA]";

                        progress.Report((sent, total,
                            $"{prefix} [{statusCode}] \"{TruncateTitle(post.Title)}\" → {errorMsg}",
                            !isDuplicate));

                        if (isDuplicate && onPostResolved != null)
                            await onPostResolved(post, true);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    progress.Report((sent, total,
                        $"[HATA] \"{TruncateTitle(post.Title)}\" → {ex.Message}", false));
                }

                if (i < posts.Count - 1)
                    await Task.Delay(DelayBetweenPostsMs, ct);
            }

            progress.Report((sent, total,
                $"─── Gönderim tamamlandı: {sent}/{total} post başarıyla gönderildi ───",
                sent == total));
        }

        private static string TryExtractError(string rawBody)
        {
            try
            {
                var doc = JsonDocument.Parse(rawBody);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? "bilinmeyen hata";
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return err.GetString() ?? "bilinmeyen hata";
            }
            catch { }
            return rawBody.Length > 200 ? rawBody[..200] + "…" : rawBody;
        }

        private static string TruncateTitle(string title, int max = 50)
            => title.Length > max ? title[..max] + "…" : title;
    }
}
