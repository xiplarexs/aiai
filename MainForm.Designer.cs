namespace KonuBot
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer

        private void InitializeComponent()
        {
            var bgDark = System.Drawing.Color.FromArgb(18, 18, 22);
            var bgPanel = System.Drawing.Color.FromArgb(24, 24, 28);
            var bgInput = System.Drawing.Color.FromArgb(32, 32, 38);
            var fgMain = System.Drawing.Color.FromArgb(230, 230, 235);
            var fgDim = System.Drawing.Color.FromArgb(140, 140, 150);
            var accent = System.Drawing.Color.FromArgb(139, 92, 246);

            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            BackColor = bgDark;
            ForeColor = fgMain;
            MinimumSize = new System.Drawing.Size(700, 580);
            Size = new System.Drawing.Size(760, 640);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "KonuBot — Toplu Blog Gönderici";

            // ── ÜST: API Ayarları ─────────────────────────────────────────────
            var panelTop = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 52, BackColor = bgPanel, Padding = new System.Windows.Forms.Padding(12, 10, 12, 6) };

            var lblApiUrl = new System.Windows.Forms.Label { Text = "API URL:", AutoSize = true, ForeColor = fgDim };
            txtApiUrl = new System.Windows.Forms.TextBox { Text = "https://xipsoft.net", BackColor = bgInput, ForeColor = fgMain, BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle, Width = 200 };
            var lblApiKey = new System.Windows.Forms.Label { Text = "Key:", AutoSize = true, ForeColor = fgDim, Margin = new System.Windows.Forms.Padding(12, 0, 0, 0) };
            txtApiKey = new System.Windows.Forms.TextBox
            {
                Text = "zBS3hwfXzNHMtt71TLKB6KmtkReGUlHLI_Ko1xNYABlWu_PZ393S8oqTfprOHm4S",
                BackColor = bgInput, ForeColor = fgMain, BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Width = 260, UseSystemPasswordChar = true,
            };
            btnTest = MakeBtn("Test", 50, fgDim, bgPanel);
            btnPublishAll = new System.Windows.Forms.CheckBox { Text = "Yayınla", AutoSize = true, ForeColor = fgDim, Checked = true, Margin = new System.Windows.Forms.Padding(12, 0, 0, 0) };

            var apiRow = new System.Windows.Forms.FlowLayoutPanel { Dock = System.Windows.Forms.DockStyle.Top, FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight, Height = 32, WrapContents = false, Padding = new System.Windows.Forms.Padding(0, 4, 0, 0) };
            apiRow.Controls.AddRange(new System.Windows.Forms.Control[] { lblApiUrl, txtApiUrl, lblApiKey, txtApiKey, btnTest, btnPublishAll });
            panelTop.Controls.Add(apiRow);

            // ── ALT: Durum Çubuğu ─────────────────────────────────────────────
            var panelBottom = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 44, BackColor = bgPanel, Padding = new System.Windows.Forms.Padding(12, 8, 12, 6) };
            chkOpenBrowser = new System.Windows.Forms.CheckBox { Text = "Tarayıcıda Aç", AutoSize = true, ForeColor = fgDim, Checked = true };
            btnSend = new System.Windows.Forms.Button
            {
                Text = "GÖNDER", Dock = System.Windows.Forms.DockStyle.Right, Width = 100,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat, BackColor = accent, ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold), Cursor = System.Windows.Forms.Cursors.Hand, Enabled = false,
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnClear = MakeBtn("Temizle", 70, fgDim, bgPanel);
            btnClear.Dock = System.Windows.Forms.DockStyle.Right;
            panelBottom.Controls.Add(btnSend);
            panelBottom.Controls.Add(btnClear);
            panelBottom.Controls.Add(chkOpenBrowser);

            // ── ORTA: Ana İçerik ──────────────────────────────────────────────
            var panelMiddle = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Fill, BackColor = bgDark, Padding = new System.Windows.Forms.Padding(12, 8, 12, 4) };

            var jsonRow = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 40, BackColor = bgDark };
            btnSelectJson = MakeBtn("JSON Seç...", 110, fgMain, accent);
            btnSelectJson.FlatAppearance.BorderSize = 0;
            btnSelectJson.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnSelectJson.Dock = System.Windows.Forms.DockStyle.Left;
            lblFileInfo = new System.Windows.Forms.Label { Text = "Bir JSON dosyası seçin (BlogPost[] formatında)", Dock = System.Windows.Forms.DockStyle.Fill, ForeColor = fgDim, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new System.Windows.Forms.Padding(10, 0, 0, 0) };
            jsonRow.Controls.Add(lblFileInfo);
            jsonRow.Controls.Add(btnSelectJson);

            lstFiles = new System.Windows.Forms.ListBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(14, 14, 16),
                ForeColor = System.Drawing.Color.FromArgb(180, 180, 190),
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                Font = new System.Drawing.Font("Consolas", 9F),
                SelectionMode = System.Windows.Forms.SelectionMode.None,
            };

            progressBulk = new System.Windows.Forms.ProgressBar { Dock = System.Windows.Forms.DockStyle.Bottom, Height = 22, Style = System.Windows.Forms.ProgressBarStyle.Continuous, BackColor = bgInput, ForeColor = accent };
            lblBulkStatus = new System.Windows.Forms.Label { Text = "Hazır", Dock = System.Windows.Forms.DockStyle.Bottom, Height = 24, ForeColor = fgDim, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };

            btnBulkCancel = MakeBtn("İptal", 60, fgDim, bgPanel);
            btnBulkCancel.Enabled = false;
            btnBulkCancel.Dock = System.Windows.Forms.DockStyle.Bottom;

            panelMiddle.Controls.Add(lstFiles);
            panelMiddle.Controls.Add(btnBulkCancel);
            panelMiddle.Controls.Add(lblBulkStatus);
            panelMiddle.Controls.Add(progressBulk);
            panelMiddle.Controls.Add(jsonRow);

            // ── LOG ───────────────────────────────────────────────────────────
            var logAccent = System.Drawing.Color.FromArgb(139, 92, 246);

            var panelLog = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Bottom, Height = 210, BackColor = bgDark,
                Padding = new System.Windows.Forms.Padding(12, 0, 12, 8),
            };

            var logBar = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Left, Width = 3, BackColor = logAccent, Margin = new System.Windows.Forms.Padding(0, 0, 8, 0) };

            var logHeader = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Top, Height = 28, BackColor = bgPanel, Padding = new System.Windows.Forms.Padding(8, 0, 8, 0) };
            lblLogTitle = new System.Windows.Forms.Label
            {
                Text = "GÖNDERİM LOGU", Dock = System.Windows.Forms.DockStyle.Left, Width = 170, AutoSize = false,
                ForeColor = fgMain, Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            };
            lblLogHint = new System.Windows.Forms.Label
            {
                Text = "BİLGİ   •   BAŞARILI   •   UYARI   •   HATA", Dock = System.Windows.Forms.DockStyle.Right, Width = 340, AutoSize = false,
                ForeColor = fgDim, Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            };
            logHeader.Controls.Add(lblLogHint);
            logHeader.Controls.Add(lblLogTitle);

            txtLog = new System.Windows.Forms.RichTextBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill, Multiline = true, ReadOnly = true, WordWrap = false,
                ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both, DetectUrls = false, HideSelection = false,
                BackColor = System.Drawing.Color.FromArgb(12, 12, 14), ForeColor = System.Drawing.Color.FromArgb(200, 200, 210),
                Font = new System.Drawing.Font("Consolas", 9F), BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
            };

            panelLog.Controls.Add(txtLog);
            panelLog.Controls.Add(logBar);
            panelLog.Controls.Add(logHeader);

            // ── Compose ───────────────────────────────────────────────────────
            Controls.Add(panelMiddle);
            Controls.Add(panelLog);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
        }

        private static System.Windows.Forms.Button MakeBtn(string text, int width, System.Drawing.Color fg, System.Drawing.Color bg) =>
            new() { Text = text, Width = width, FlatStyle = System.Windows.Forms.FlatStyle.Flat, BackColor = bg,
                     ForeColor = fg, Cursor = System.Windows.Forms.Cursors.Hand, Height = 28, Margin = new System.Windows.Forms.Padding(4, 0, 0, 0) };

        #endregion

        internal System.Windows.Forms.TextBox txtApiUrl = null!;
        internal System.Windows.Forms.TextBox txtApiKey = null!;
        internal System.Windows.Forms.RichTextBox txtLog = null!;
        internal System.Windows.Forms.Label lblLogTitle = null!;
        internal System.Windows.Forms.Label lblLogHint = null!;
        internal System.Windows.Forms.CheckBox chkOpenBrowser = null!;
        internal System.Windows.Forms.CheckBox btnPublishAll = null!;
        internal System.Windows.Forms.Button btnSend = null!;
        internal System.Windows.Forms.Button btnTest = null!;
        internal System.Windows.Forms.Button btnClear = null!;
        internal System.Windows.Forms.Button btnSelectJson = null!;
        internal System.Windows.Forms.Button btnBulkCancel = null!;
        internal System.Windows.Forms.Label lblFileInfo = null!;
        internal System.Windows.Forms.Label lblBulkStatus = null!;
        internal System.Windows.Forms.ListBox lstFiles = null!;
        internal System.Windows.Forms.ProgressBar progressBulk = null!;
    }
}
