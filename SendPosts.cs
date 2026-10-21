using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        try
        {
            var json = await File.ReadAllTextAsync("posts.json", Encoding.UTF8);
            var posts = JsonSerializer.Deserialize<JsonElement>(json);
            
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("X-Bot-Key", "zBS3hwfXzNHMtt71TLKB6KmtkReGUlHLI_Ko1xNYABlWu_PZ393S8oqTfprOHm4S");
            
            Console.WriteLine("🚀 10 makale gönderiliyor...");
            Console.WriteLine("---");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://xipsoft.net/api/bot/posts/bulk", content);
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            Console.WriteLine("✓ BAŞARILI!");
            Console.WriteLine("");
            
            var results = result.GetProperty("results");
            Console.WriteLine($"Toplam: {results.GetProperty("total").GetInt32()}");
            Console.WriteLine($"Başarılı: {results.GetProperty("success_count").GetInt32()}");
            Console.WriteLine($"Başarısız: {results.GetProperty("failed_count").GetInt32()}");
            
            if (results.TryGetProperty("posts", out var postsArray) && postsArray.ValueKind == JsonValueKind.Array)
            {
                Console.WriteLine("");
                Console.WriteLine("Yüklenen Makaleler:");
                foreach (var post in postsArray.EnumerateArray())
                {
                    var id = post.GetProperty("id").GetInt32();
                    var slug = post.GetProperty("slug").GetString();
                    Console.WriteLine($"  - [{id}] {slug}");
                }
            }
            
            if (results.TryGetProperty("errors", out var errorsArray) && errorsArray.ValueKind == JsonValueKind.Array && errorsArray.GetArrayLength() > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Hatalar:");
                foreach (var err in errorsArray.EnumerateArray())
                {
                    var idx = err.GetProperty("index").GetInt32();
                    var error = err.GetProperty("error").GetString();
                    Console.WriteLine($"  ✗ Index {idx}: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ HATA: {ex.Message}");
        }
    }
}
