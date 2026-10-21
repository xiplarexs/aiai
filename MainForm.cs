using System.Diagnostics;
using System.Text.Json;

namespace KonuBot;

public partial class MainForm : Form
{
    // ── Sabit API Anahtarı ────────────────────────────────────────────────────
    private const string DEFAULT_BOT_API_KEY = "burayaapigir:D";
    private const string DEFAULT_API_URL = "https://xipsoft.net";

    private CancellationTokenSource? _bulkCts;
    private List<BlogPost>? _loadedPosts;
    private readonly HashSet<BlogPost> _removedPosts = new();
    private string? _postsFilePath;
    private string? _apiKey;
    private string? _apiUrl;

    public MainForm()
    {
        InitializeComponent();
        WireEvents();
        LoadSettings();
        Shown += MainForm_Shown;
    }

    private void WireEvents()
    {
        btnTest.Click += BtnTest_Click;
        btnClear.Click += (_, _) => { txtLog.Clear(); };
        btnSelectJson.Click += BtnSelectJson_Click;
        btnSend.Click += BtnSend_Click;
        btnBulkCancel.Click += (_, _) => _bulkCts?.Cancel();

        FormClosing += (_, _) =>
        {
            _bulkCts?.Cancel();
            SaveSettings();
        };
    }

    private async void BtnTest_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtApiUrl.Text) || string.IsNullOrWhiteSpace(txtApiKey.Text))
        {
            MessageBox.Show("URL ve API Key girilmeli.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnTest.Enabled = false;
        btnTest.Text = "...";
        AppendLog("Bağlantı test ediliyor...");

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(txtApiUrl.Text.Trim().TrimEnd('/') + "/") };
            http.DefaultRequestHeaders.Add("X-Bot-Key", txtApiKey.Text.Trim());
            var res = await http.GetAsync("api/bot");
            AppendLog(res.IsSuccessStatusCode
                ? $"✓ Bağlantı başarılı (HTTP {(int)res.StatusCode})"
                : $"✗ Bağlantı başarısız (HTTP {(int)res.StatusCode})");
        }
        catch (Exception ex)
        {
            AppendLog($"✗ Test hatası: {ex.Message}");
        }
        finally
        {
            btnTest.Enabled = true;
            btnTest.Text = "Test";
        }
    }

    private async void BtnSelectJson_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "JSON Dosyası|*.json|Tüm Dosyalar|*.*",
            Title = "Blog JSON Seç"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        await LoadPostsAsync(dlg.FileName);
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        var postsPath = Path.Combine(AppContext.BaseDirectory, "posts.json");
        if (!File.Exists(postsPath))
        {
            lblFileInfo.Text = "posts.json exe klasöründe bulunamadı";
            lblFileInfo.ForeColor = Color.FromArgb(239, 68, 68);
            lblBulkStatus.Text = "JSON dosyası bulunamadı";
            AppendLog($"✗ Başlangıç JSON dosyası bulunamadı: {postsPath}");
            return;
        }

        await LoadPostsAsync(postsPath);
    }

    private async Task<bool> LoadPostsAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var posts = JsonSerializer.Deserialize<List<BlogPost>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (posts == null || posts.Count == 0)
            {
                AppendLog("✗ JSON dosyası boş veya geçersiz formatta");
                lblBulkStatus.Text = "JSON dosyası boş veya geçersiz";
                return false;
            }

            _loadedPosts = posts;
            _removedPosts.Clear();
            _postsFilePath = filePath;
            lstFiles.Items.Clear();

            foreach (var p in posts)
            {
                var wc = p.Content?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                lstFiles.Items.Add($"{p.Title}  ({wc} kelime)");
            }

            lblFileInfo.Text = $"{posts.Count} post JSON'dan okundu — {Path.GetFileName(filePath)}";
            lblFileInfo.ForeColor = Color.FromArgb(34, 197, 94);
            btnSend.Enabled = true;
            lblBulkStatus.Text = $"{posts.Count} post hazır";

            AppendLog($"✓ {posts.Count} post JSON dosyasından okundu");
            return true;
        }
        catch (Exception ex)
        {
            AppendLog($"✗ JSON okuma hatası: {ex.Message}");
            lblBulkStatus.Text = "JSON okunamadı";
            return false;
        }
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        if (_loadedPosts == null || _loadedPosts.Count == 0)
        {
            MessageBox.Show("Önce bir JSON dosyası seçin.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtApiUrl.Text) || string.IsNullOrWhiteSpace(txtApiKey.Text))
        {
            MessageBox.Show("API URL ve API Key girilmeli.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await SendLoadedPostsAsync();
    }

    private async Task SendLoadedPostsAsync()
    {
        if (_loadedPosts == null || _loadedPosts.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(txtApiUrl.Text) || string.IsNullOrWhiteSpace(txtApiKey.Text))
        {
            AppendLog("✗ Otomatik gönderim yapılamadı: API URL ve API Key girilmeli.");
            lblBulkStatus.Text = "API ayarları eksik";
            lblBulkStatus.ForeColor = Color.FromArgb(239, 68, 68);
            return;
        }

        foreach (var p in _loadedPosts)
            p.IsPublished = btnPublishAll.Checked;

        _bulkCts = new CancellationTokenSource();
        _apiUrl = txtApiUrl.Text.Trim();
        _apiKey = txtApiKey.Text.Trim();

        btnSend.Enabled = false;
        btnBulkCancel.Enabled = true;
        btnSelectJson.Enabled = false;
        progressBulk.Value = 0;
        progressBulk.Maximum = _loadedPosts.Count;
        lblBulkStatus.Text = $"0 / {_loadedPosts.Count} gönderildi";
        lblBulkStatus.ForeColor = Color.FromArgb(230, 230, 235);

        AppendLog($"═══ Toplu gönderim başlıyor: {_loadedPosts.Count} post ═══");

        var startTime = DateTime.Now;

        try
        {
            var svc = new ApiService(_apiKey, _apiUrl);
            var progress = new Progress<(int sent, int total, string message, bool success)>(OnProgress);

            await svc.SendPostsAsync(
                _loadedPosts,
                progress,
                _bulkCts.Token,
                RemoveResolvedPostFromJsonAsync);

            var elapsed = DateTime.Now - startTime;
            AppendLog($"Gönderim isteği tamamlandı ({elapsed.TotalSeconds:F1}s)");

            // Site otomatik açılmasın
            // if (chkOpenBrowser.Checked)
            //     Process.Start(new ProcessStartInfo($"{_apiUrl}/blog") { UseShellExecute = true });
        }
        catch (OperationCanceledException)
        {
            AppendLog("⚠ İptal edildi");
        }
        catch (Exception ex)
        {
            AppendLog($"✗ Hata: {ex.Message}");
            lblBulkStatus.Text = "Hata oluştu";
            lblBulkStatus.ForeColor = Color.FromArgb(239, 68, 68);
        }
        finally
        {
            if (_removedPosts.Count > 0)
            {
                _loadedPosts = _loadedPosts?
                    .Where(post => !_removedPosts.Contains(post))
                    .ToList();
                RefreshLoadedPostsList();
            }

            btnSend.Enabled = _loadedPosts?.Count > 0;
            btnBulkCancel.Enabled = false;
            btnSelectJson.Enabled = true;
            _bulkCts?.Dispose();
            _bulkCts = null;
        }
    }

    private async Task RemoveResolvedPostFromJsonAsync(BlogPost resolvedPost, bool alreadyExists)
    {
        if (_loadedPosts == null || _postsFilePath == null)
            return;

        var remainingPosts = _loadedPosts
            .Where(post => !_removedPosts.Contains(post) && !ReferenceEquals(post, resolvedPost))
            .ToList();
        var tempPath = _postsFilePath + ".tmp";

        try
        {
            var json = JsonSerializer.Serialize(remainingPosts, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _postsFilePath, overwrite: true);
            _removedPosts.Add(resolvedPost);
            string outcome = alreadyExists ? "API'de zaten mevcut" : "API tarafından kaydedildi";
            AppendLog($"✓ \"{resolvedPost.Title}\" {outcome}; {_postsFilePath} dosyasından kaldırıldı");
        }
        catch (Exception ex)
        {
            AppendLog($"✗ \"{resolvedPost.Title}\" JSON dosyasından kaldırılamadı: {ex.Message}");
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception cleanupException)
                {
                    AppendLog($"✗ Geçici JSON dosyası silinemedi ({tempPath}): {cleanupException.Message}");
                }
            }
        }
    }

    private void RefreshLoadedPostsList()
    {
        lstFiles.Items.Clear();

        if (_loadedPosts == null)
            return;

        foreach (var post in _loadedPosts)
        {
            var wordCount = post.Content?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
            lstFiles.Items.Add($"{post.Title}  ({wordCount} kelime)");
        }

        if (_postsFilePath != null)
            lblFileInfo.Text = $"{_loadedPosts.Count} post kaldı — {Path.GetFileName(_postsFilePath)}";
    }

    private void OnProgress((int sent, int total, string message, bool success) p)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnProgress(p));
            return;
        }

        AppendLog(p.message);

        if (p.total > 0)
        {
            progressBulk.Value = Math.Min(p.sent, progressBulk.Maximum);
            if (p.message.StartsWith("─── Gönderim tamamlandı:", StringComparison.Ordinal))
            {
                lblBulkStatus.Text = $"Tamamlandı: {p.sent} / {p.total} başarılı";
                lblBulkStatus.ForeColor = p.success
                    ? Color.FromArgb(34, 197, 94)
                    : Color.FromArgb(239, 68, 68);
            }
            else
            {
                lblBulkStatus.Text = $"{p.sent} / {p.total} gönderildi";
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private enum LogLevel { Info, Success, Warning, Error, Section }

    private readonly Font _logFont = new("Consolas", 9F);
    private readonly Font _logFontBold = new("Consolas", 9F, FontStyle.Bold);

    private static LogLevel ClassifyLog(string msg)
    {
        if (msg.Contains("═══") || msg.Contains("───"))
            return LogLevel.Section;

        if (msg.Contains("[HATA]") || msg.Contains("[VALIDASYON HATASI]") ||
            msg.Contains("✗") || msg.Contains("başarısız") || msg.Contains("Hata"))
            return LogLevel.Error;

        if (msg.Contains("[DUPLICATE]") || msg.Contains("⚠") || msg.Contains("İptal"))
            return LogLevel.Warning;

        if (msg.Contains("✓") || msg.Contains("[OK") || msg.Contains("başarılı"))
            return LogLevel.Success;

        return LogLevel.Info;
    }

    /// <summary>
    /// Log satırını tek biçimde yazar: sabit zaman damgası + seviyeye göre renk.
    /// Tüm log çıktısı bu yoldan geçer; böylece görünüm tutarlı kalır.
    /// </summary>
    private void AppendLog(string msg)
    {
        if (InvokeRequired) { BeginInvoke(() => AppendLog(msg)); return; }

        var level = ClassifyLog(msg);
        var msgColor = level switch
        {
            LogLevel.Success => Color.FromArgb(52, 211, 153),
            LogLevel.Warning => Color.FromArgb(245, 158, 11),
            LogLevel.Error   => Color.FromArgb(248, 113, 113),
            LogLevel.Section => Color.FromArgb(167, 139, 250),
            _                => Color.FromArgb(200, 200, 210),
        };

        string ts = DateTime.Now.ToString("HH:mm:ss");

        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.SelectionLength = 0;
        txtLog.SelectionColor = Color.FromArgb(120, 120, 132);
        txtLog.SelectionFont = _logFont;
        txtLog.AppendText($"[{ts}] ");

        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.SelectionLength = 0;
        txtLog.SelectionColor = msgColor;
        txtLog.SelectionFont = level == LogLevel.Section ? _logFontBold : _logFont;
        txtLog.AppendText(msg + Environment.NewLine);

        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.SelectionLength = 0;
        txtLog.ScrollToCaret();
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "konubot", "settings.json");

    private void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(dir);
            var data = new Dictionary<string, string>
            {
                ["apiUrl"] = txtApiUrl.Text,
                ["apiKey"] = txtApiKey.Text,
                ["openBrowser"] = chkOpenBrowser.Checked.ToString(),
                ["publishAll"] = btnPublishAll.Checked.ToString(),
            };
            File.WriteAllText(_settingsPath,
                JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void LoadSettings()
    {
        try
        {
            // Default değerlerle başla
            txtApiUrl.Text = DEFAULT_API_URL;
            txtApiKey.Text = DEFAULT_BOT_API_KEY;

            // Kaydedilmiş ayarları yükle ve override et
            if (!File.Exists(_settingsPath)) return;
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(_settingsPath));
            if (data == null) return;
            if (data.TryGetValue("apiUrl", out var url) && !string.IsNullOrEmpty(url)) txtApiUrl.Text = url;
            if (data.TryGetValue("apiKey", out var key) && !string.IsNullOrEmpty(key)) txtApiKey.Text = key;
            if (data.TryGetValue("openBrowser", out var ob)) chkOpenBrowser.Checked = ob == "True";
            if (data.TryGetValue("publishAll", out var pa)) btnPublishAll.Checked = pa == "True";
        }
        catch { }
    }
}
