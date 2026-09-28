using System.Text.Json;
using PdfmeCore;
using PdfmeReports;

namespace GenpinTicketConsumer;

public sealed class TicketForm : Form
{
    private readonly IReportGenerator _reportGenerator;
    private readonly TextBox _partNoTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _lotTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _qrTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _barcodeTextBox = new() { Dock = DockStyle.Fill };
    private readonly Button _generateButton = new() { Text = "PDFを生成", AutoSize = true };
    private readonly Label _statusLabel = new() { AutoSize = true, Padding = new Padding(12, 8, 0, 0) };
    private readonly string _ticketLayoutPath = Path.Combine(AppContext.BaseDirectory, "Layouts", "現品票テスト.layout.json");

    public TicketForm() : this(new ReportGenerator()) { }

    public TicketForm(IReportGenerator reportGenerator)
    {
        _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
        Text = "現品票テスト - PDF生成";
        Font = new Font("Yu Gothic UI", 10);
        MinimumSize = new Size(650, 330);
        Size = new Size(760, 360);
        StartPosition = FormStartPosition.CenterScreen;

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 6,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        for (var row = 0; row < 4; row++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(grid);

        var heading = new Label
        {
            Text = "現品票テストに印字する値を入力してください。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
        };
        grid.Controls.Add(heading, 0, 0);
        grid.SetColumnSpan(heading, 2);
        AddInputRow(grid, 1, "品番 (partNo)", _partNoTextBox);
        AddInputRow(grid, 2, "ロット (lot)", _lotTextBox);
        AddInputRow(grid, 3, "QRコード (qr)", _qrTextBox);
        AddInputRow(grid, 4, "バーコード (barcode)", _barcodeTextBox);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _generateButton.Click += async (_, _) => await GenerateTicketAsync();
        actions.Controls.Add(_generateButton);
        actions.Controls.Add(_statusLabel);
        grid.Controls.Add(actions, 0, 5);
        grid.SetColumnSpan(actions, 2);

        try
        {
            var defaults = LayoutStore.Load(_ticketLayoutPath).Fields.ToDictionary(field => field.Name, field => field.Value);
            _partNoTextBox.Text = defaults.GetValueOrDefault("partNo", "");
            _lotTextBox.Text = defaults.GetValueOrDefault("lot", "");
            _qrTextBox.Text = defaults.GetValueOrDefault("qr", "");
            _barcodeTextBox.Text = defaults.GetValueOrDefault("barcode", "");
        }
        catch (Exception ex)
        {
            _generateButton.Enabled = false;
            _statusLabel.Text = $"レイアウトを読み込めません: {ex.Message}";
        }
    }

    private static void AddInputRow(TableLayoutPanel grid, int row, string caption, TextBox input)
    {
        grid.Controls.Add(new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, row);
        grid.Controls.Add(input, 1, row);
    }

    private async Task GenerateTicketAsync()
    {
        var partNo = _partNoTextBox.Text.Trim();
        if (partNo.Length == 0)
        {
            MessageBox.Show(this, "品番を入力してください。", "入力確認", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _partNoTextBox.Focus();
            return;
        }

        var values = new 現品票テストValues
        {
            PartNo = partNo,
            Lot = _lotTextBox.Text.Trim(),
            Qr = _qrTextBox.Text.Trim(),
            Barcode = _barcodeTextBox.Text.Trim(),
        };
        var json = JsonSerializer.Serialize(values);
        var safePartNo = string.Concat(partNo.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"現品票_{safePartNo}.pdf",
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _generateButton.Enabled = false;
            UseWaitCursor = true;
            _statusLabel.Text = "PDFを生成しています...";
            await _reportGenerator.GenerateFromJsonAsync(_ticketLayoutPath, json, dialog.FileName);
            _statusLabel.Text = $"保存しました: {dialog.FileName}";
            MessageBox.Show(this, "PDFを生成しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "PDF生成に失敗しました。";
            MessageBox.Show(this, ex.Message, "PDF生成エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _generateButton.Enabled = true;
            UseWaitCursor = false;
        }
    }
}

