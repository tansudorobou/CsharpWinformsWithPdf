using System.Text.Json;
using PdfmeCore;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PdfmeWinForms;

internal sealed class PdfmeVisualEditorForm : Form
{
    private const string EditorUrl = "https://pdfme.local/editor.html";
    private readonly LayoutDocument _layout;
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, Text = "エディターを読み込んでいます...", TextAlign = ContentAlignment.MiddleLeft };

    public List<LayoutField>? EditedFields { get; private set; }

    public PdfmeVisualEditorForm(LayoutDocument layout)
    {
        _layout = layout;
        Text = "PDF上で配置を編集";
        Size = new Size(1300, 850);
        MinimumSize = new Size(850, 600);
        Controls.Add(_webView);
        Controls.Add(_status);
        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var editorDir = Path.Combine(AppContext.BaseDirectory, "pdfme-runner", "editor");
            if (!File.Exists(Path.Combine(editorDir, "dist", "editor.js")))
                throw new FileNotFoundException("pdfmeエディターが見つかりません。pdfme-runnerで npm run build:editor を実行してください。");
            var workerDir = Path.Combine(editorDir, "dist", "assets");
            if (!Directory.Exists(workerDir) || !Directory.EnumerateFiles(workerDir, "clawpdf-worker-*.js").Any())
                throw new FileNotFoundException("PDF描画ワーカーが見つかりません。pdfme-runnerで npm run build:editor を実行し、アプリを再ビルドしてください。");

            await _webView.EnsureCoreWebView2Async();
            if (IsDisposed) return;
            _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping("pdfme.local", editorDir, CoreWebView2HostResourceAccessKind.DenyCors);
            _webView.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (!e.Uri.StartsWith("https://pdfme.local/", StringComparison.OrdinalIgnoreCase)) e.Cancel = true;
            };
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.CoreWebView2.Navigate(EditorUrl);
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "配置エディター", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!e.Source.Equals(EditorUrl, StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            using var message = JsonDocument.Parse(e.WebMessageAsJson);
            var root = message.RootElement;
            var type = root.GetProperty("type").GetString();
            if (type == "ready")
            {
                var basePdf = _layout.BasePdfPath.Length > 0
                    ? Convert.ToBase64String(await File.ReadAllBytesAsync(LayoutStore.ResolvePath(_layout.BasePdfPath)))
                    : null;
                var font = _layout.FontPath.Length > 0
                    ? Convert.ToBase64String(await File.ReadAllBytesAsync(LayoutStore.ResolvePath(_layout.FontPath)))
                    : null;
                if (IsDisposed) return;
                _webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(
                    new { type = "load", layout = _layout, basePdf, font }, PdfmeJsonSerializerOptions.Default));
                _status.Text = "PDFを読み込んでいます...";
            }
            else if (type == "loaded")
            {
                _status.Text = "項目を選ぶと右側で色・回転・行間・枠線などを設定できます。完了したら「配置を反映」を押してください。";
            }
            else if (type == "apply")
            {
                var fields = root.GetProperty("fields").Deserialize<List<LayoutField>>(PdfmeJsonSerializerOptions.Default)
                    ?? throw new InvalidOperationException("配置データを読み取れません。");
                LayoutValidator.ValidateFields(fields);
                EditedFields = fields;
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (type == "cancel")
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (type == "error")
            {
                _status.Text = root.GetProperty("message").GetString() ?? "エディターでエラーが発生しました。";
                MessageBox.Show(this, _status.Text, "配置エディター", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "配置エディター", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

}

