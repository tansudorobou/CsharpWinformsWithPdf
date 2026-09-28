using System.Diagnostics;
using PdfmeCore;

namespace PdfmeWinForms;

public sealed class ReportDesignerForm : Form
{
    private readonly IReportGenerator _reportGenerator;
    private readonly TextBox _reportNameTextBox = new() { Dock = DockStyle.Fill, Text = "新しい帳票" };
    private readonly TextBox _basePdfPathTextBox = new() { Dock = DockStyle.Fill, PlaceholderText = "未指定なら白紙A4" };
    private readonly TextBox _fontPathTextBox = new() { Dock = DockStyle.Fill, PlaceholderText = "日本語にはTTFフォントを指定" };
    private readonly ComboBox _paperPresetComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly ComboBox _orientationComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75 };
    private readonly NumericUpDown _pageWidthInput = new() { Minimum = 1, Maximum = 2000, DecimalPlaces = 1, Value = 210, Width = 85 };
    private readonly NumericUpDown _pageHeightInput = new() { Minimum = 1, Maximum = 2000, DecimalPlaces = 1, Value = 297, Width = 85 };
    private readonly Label _paperHintLabel = new() { Text = "元PDFを選択中は元PDFのサイズを使用", AutoSize = true, Padding = new Padding(8, 6, 0, 0) };
    private bool _isUpdatingPageDimensions;
    private readonly LayoutFieldGridAdapter _fieldGridAdapter = new();
    private readonly Button _generateButton = new() { Text = "PDFを生成", AutoSize = true };
    private readonly Button _visualEditorButton = new() { Text = "PDF上で配置を編集", AutoSize = true };
    private PdfDocumentMetadata _pdfMetadata = new();
    private string? _currentLayoutPath;
    private bool _isUpdatingReferencedPaths;

    public ReportDesignerForm() : this(new ReportGenerator()) { }

    public ReportDesignerForm(IReportGenerator reportGenerator)
    {
        _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
        Text = "PDFme 帳票デザイナー";
        MinimumSize = new Size(950, 500);
        Size = new Size(1100, 620);
        Font = new Font("Yu Gothic UI", 9);
        BuildUi();
        AddExampleFields();
        if (File.Exists(LayoutStore.ResolvePath(LayoutStore.BundledFontPath))) _fontPathTextBox.Text = LayoutStore.BundledFontPath;
        ConfigurePaperControls();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        var files = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3 };
        files.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        files.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        files.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        for (var row = 0; row < 3; row++) files.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3));
        files.Controls.Add(new Label { Text = "帳票名", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        files.Controls.Add(_reportNameTextBox, 1, 0);
        files.Controls.Add(new Label { Text = "元のPDF", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        files.Controls.Add(_basePdfPathTextBox, 1, 1);
        files.Controls.Add(new Label { Text = "日本語フォント", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        files.Controls.Add(_fontPathTextBox, 1, 2);
        files.Controls.Add(BrowseButton("参照...", "PDF (*.pdf)|*.pdf", _basePdfPathTextBox), 2, 1);
        files.Controls.Add(BrowseButton("参照...", "TrueType (*.ttf)|*.ttf", _fontPathTextBox), 2, 2);
        root.Controls.Add(files, 0, 0);

        var paper = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        paper.Controls.Add(new Label { Text = "白紙PDFの用紙", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        paper.Controls.Add(_paperPresetComboBox);
        paper.Controls.Add(_orientationComboBox);
        paper.Controls.Add(new Label { Text = "幅", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
        paper.Controls.Add(_pageWidthInput);
        paper.Controls.Add(new Label { Text = "× 高さ", AutoSize = true, Padding = new Padding(4, 6, 0, 0) });
        paper.Controls.Add(_pageHeightInput);
        paper.Controls.Add(new Label { Text = "mm", AutoSize = true, Padding = new Padding(4, 6, 0, 0) });
        paper.Controls.Add(_paperHintLabel);
        root.Controls.Add(paper, 0, 1);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var fresh = new Button { Text = "新規", AutoSize = true };
        var save = new Button { Text = "配置を保存", AutoSize = true };
        var saveAs = new Button { Text = "名前を付けて保存", AutoSize = true };
        var load = new Button { Text = "配置を開く", AutoSize = true };
        var metadata = new Button { Text = "PDF文書情報", AutoSize = true };
        fresh.Click += (_, _) => NewLayout();
        save.Click += (_, _) => SaveLayout();
        saveAs.Click += (_, _) => SaveLayout(saveAs: true);
        load.Click += (_, _) => LoadLayout();
        metadata.Click += (_, _) => OpenMetadataEditor();
        bar.Controls.Add(fresh);
        bar.Controls.Add(save);
        bar.Controls.Add(saveAs);
        bar.Controls.Add(load);
        bar.Controls.Add(metadata);
        _visualEditorButton.Click += (_, _) => OpenVisualEditor();
        bar.Controls.Add(_visualEditorButton);
        bar.Controls.Add(new Label { Text = "1行が1つの可変項目です。PDFの固定部分は元PDFを使用します。", AutoSize = true, Padding = new Padding(16, 7, 0, 0) });
        root.Controls.Add(bar, 0, 2);
        root.Controls.Add(_fieldGridAdapter.Grid, 0, 3);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _generateButton.Click += async (_, _) => await GenerateAsync();
        bottom.Controls.Add(_generateButton);
        bottom.Controls.Add(new Label { Text = "位置と大きさの単位は mm。左上が (0, 0) です。", AutoSize = true, Padding = new Padding(16, 7, 0, 0) });
        root.Controls.Add(bottom, 0, 4);
    }

    private void ConfigurePaperControls()
    {
        _paperPresetComboBox.Items.AddRange(["A4", "A5", "Letter", "カスタム"]);
        _orientationComboBox.Items.AddRange(["縦", "横"]);
        _paperPresetComboBox.SelectedIndex = 0;
        _orientationComboBox.SelectedIndex = 0;
        _paperPresetComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_isUpdatingPageDimensions) return;
            if (_paperPresetComboBox.SelectedIndex != 3) ApplyPaperPresetDimensions();
            UpdatePaperControlAvailability();
        };
        _orientationComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_isUpdatingPageDimensions) return;
            if (_paperPresetComboBox.SelectedIndex == 3)
            {
                _isUpdatingPageDimensions = true;
                try
                {
                    var width = _pageWidthInput.Value;
                    _pageWidthInput.Value = _pageHeightInput.Value;
                    _pageHeightInput.Value = width;
                }
                finally { _isUpdatingPageDimensions = false; }
            }
            else ApplyPaperPresetDimensions();
        };
        void SyncCustomOrientation(object? _, EventArgs __)
        {
            if (_isUpdatingPageDimensions || _paperPresetComboBox.SelectedIndex != 3 || _pageWidthInput.Value == _pageHeightInput.Value) return;
            _isUpdatingPageDimensions = true;
            try { _orientationComboBox.SelectedIndex = _pageWidthInput.Value > _pageHeightInput.Value ? 1 : 0; }
            finally { _isUpdatingPageDimensions = false; }
        }
        _pageWidthInput.ValueChanged += SyncCustomOrientation;
        _pageHeightInput.ValueChanged += SyncCustomOrientation;
        _basePdfPathTextBox.TextChanged += (_, _) =>
        {
            UpdatePaperControlAvailability();
            if (!_isUpdatingReferencedPaths && _reportNameTextBox.Text == "新しい帳票" && File.Exists(_basePdfPathTextBox.Text))
                _reportNameTextBox.Text = Path.GetFileNameWithoutExtension(_basePdfPathTextBox.Text);
        };
        UpdatePaperControlAvailability();
    }

    private void ApplyPaperPresetDimensions()
    {
        var (shortSide, longSide) = _paperPresetComboBox.SelectedIndex switch
        {
            1 => (148m, 210m),
            2 => (215.9m, 279.4m),
            _ => (210m, 297m),
        };
        _pageWidthInput.Value = _orientationComboBox.SelectedIndex == 1 ? longSide : shortSide;
        _pageHeightInput.Value = _orientationComboBox.SelectedIndex == 1 ? shortSide : longSide;
    }

    private void UpdatePaperControlAvailability()
    {
        var blank = string.IsNullOrWhiteSpace(_basePdfPathTextBox.Text);
        _paperPresetComboBox.Enabled = blank;
        _orientationComboBox.Enabled = blank;
        _pageWidthInput.Enabled = blank && _paperPresetComboBox.SelectedIndex == 3;
        _pageHeightInput.Enabled = blank && _paperPresetComboBox.SelectedIndex == 3;
        _paperHintLabel.Visible = !blank;
    }

    private static Button BrowseButton(string title, string filter, TextBox target)
    {
        var button = new Button { Text = title, Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.FileName;
        };
        return button;
    }

    private void AddExampleFields()
    {
        _fieldGridAdapter.AddField(new LayoutField("partNo", LayoutFieldTypes.Text, 20, 25, 80, 10, 14, "PART-001"));
        _fieldGridAdapter.AddField(new LayoutField("lot", LayoutFieldTypes.Text, 20, 40, 80, 10, 12, "LOT-2026-001"));
        _fieldGridAdapter.AddField(new LayoutField("qr", LayoutFieldTypes.QrCode, 130, 22, 30, 30, 12, "PART-001|LOT-2026-001"));
        _fieldGridAdapter.AddField(new LayoutField("barcode", LayoutFieldTypes.Code128, 20, 65, 100, 20, 12, "PART-001"));
    }

    private LayoutDocument ReadLayoutFromControls(bool allowEmpty = false, bool validatePaths = true)
    {
        var fields = _fieldGridAdapter.ReadFields(allowEmpty);
        var pdfPath = _basePdfPathTextBox.Text.Trim();
        var fontPath = _fontPathTextBox.Text.Trim();
        var layout = new LayoutDocument(
            pdfPath, fontPath, fields, (double)_pageWidthInput.Value, (double)_pageHeightInput.Value,
            ReportName: _reportNameTextBox.Text.Trim(), Metadata: _pdfMetadata);
        return validatePaths ? LayoutReferencedFiles.ResolveAndValidate(layout, _currentLayoutPath) : layout;
    }

    private void OpenMetadataEditor()
    {
        using var dialog = new MetadataEditorForm(_pdfMetadata);
        if (dialog.ShowDialog(this) == DialogResult.OK) _pdfMetadata = dialog.EditedMetadata;
    }

    private static string GetDefaultLayoutDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents.Length > 0 ? documents : AppContext.BaseDirectory, "PdfmeWinForms", "Layouts");
    }

    private static string SanitizeFileNameStem(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
        return safe.Length > 0 ? safe : "帳票";
    }

    private void SaveLayout(bool saveAs = false)
    {
        try
        {
            var layout = ReadLayoutFromControls(validatePaths: false);
            LayoutValidator.Validate(layout, requireReportName: true);
            var path = _currentLayoutPath;
            if (saveAs || path is null)
            {
                var directory = path is null ? GetDefaultLayoutDirectory() : Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(directory);
                using var dialog = new SaveFileDialog
                {
                    Filter = "帳票レイアウト (*.layout.json)|*.layout.json|JSON (*.json)|*.json",
                    InitialDirectory = directory,
                    FileName = SanitizeFileNameStem(layout.ReportName) + ".layout.json",
                    DefaultExt = "json",
                    OverwritePrompt = true,
                };
                if (dialog.ShowDialog() != DialogResult.OK) return;
                path = dialog.FileName;
            }
            var resolvedLayout = LayoutReferencedFiles.ResolveAndValidate(layout, _currentLayoutPath ?? path);
            ReportLayoutSaveCoordinator.Save(resolvedLayout, path);
            _currentLayoutPath = path;
            // 保存先を変えても画面に残る参照が同じファイルを指すようにする。
            _isUpdatingReferencedPaths = true;
            try
            {
                _basePdfPathTextBox.Text = resolvedLayout.BasePdfPath;
                _fontPathTextBox.Text = resolvedLayout.FontPath;
            }
            finally { _isUpdatingReferencedPaths = false; }
            Text = $"PDFme 帳票デザイナー - {layout.ReportName}";
            MessageBox.Show(this,
                $"配置を保存しました。\n{path}\n\n値クラスを生成しました。\n{ReportValuesClassGenerator.GetOutputPath(path)}",
                "保存完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void NewLayout()
    {
        _currentLayoutPath = null;
        _reportNameTextBox.Text = "新しい帳票";
        _basePdfPathTextBox.Clear();
        _fontPathTextBox.Text = LayoutStore.BundledFontPath;
        _fieldGridAdapter.ClearFields();
        _pdfMetadata = new PdfDocumentMetadata();
        SetPageDimensions(210, 297);
        Text = "PDFme 帳票デザイナー";
    }

    private void LoadLayout()
    {
        try
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "帳票レイアウト (*.layout.json)|*.layout.json|JSON (*.json)|*.json",
                InitialDirectory = Directory.Exists(GetDefaultLayoutDirectory()) ? GetDefaultLayoutDirectory() : AppContext.BaseDirectory,
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var layout = LayoutStore.Load(dialog.FileName);
            _basePdfPathTextBox.Text = layout.BasePdfPath;
            _fontPathTextBox.Text = layout.FontPath;
            _reportNameTextBox.Text = layout.ReportName;
            _pdfMetadata = layout.Metadata ?? new PdfDocumentMetadata();
            SetPageDimensions(layout.PageWidth, layout.PageHeight);
            _fieldGridAdapter.ReplaceFields(layout.Fields);
            _currentLayoutPath = Path.GetFullPath(dialog.FileName);
            Text = $"PDFme 帳票デザイナー - {layout.ReportName}";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SetPageDimensions(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width < 1 || height < 1 || width > 2000 || height > 2000)
            throw new InvalidOperationException("用紙サイズは1～2000 mmで指定してください。");
        var shortSide = Math.Min(width, height);
        var longSide = Math.Max(width, height);
        static bool Same(double left, double right) => Math.Abs(left - right) < 0.05;
        var preset = (shortSide, longSide) switch
        {
            (var s, var l) when Same(s, 210) && Same(l, 297) => 0,
            (var s, var l) when Same(s, 148) && Same(l, 210) => 1,
            (var s, var l) when Same(s, 215.9) && Same(l, 279.4) => 2,
            _ => 3,
        };
        _isUpdatingPageDimensions = true;
        try
        {
            _paperPresetComboBox.SelectedIndex = preset;
            _pageWidthInput.Value = (decimal)width;
            _pageHeightInput.Value = (decimal)height;
            _orientationComboBox.SelectedIndex = width > height ? 1 : 0;
        }
        finally { _isUpdatingPageDimensions = false; }
        UpdatePaperControlAvailability();
    }

    private async Task GenerateAsync()
    {
        try
        {
            var layout = ReadLayoutFromControls();
            using var dialog = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = SanitizeFileNameStem(layout.ReportName) + ".pdf" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            _generateButton.Enabled = false;
            UseWaitCursor = true;
            await _reportGenerator.GenerateAsync(layout, dialog.FileName);
            var open = MessageBox.Show("PDFを生成しました。開きますか？", "完了", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { ShowError(ex); }
        finally { _generateButton.Enabled = true; UseWaitCursor = false; }
    }

    private void OpenVisualEditor()
    {
        try
        {
            var layout = ReadLayoutFromControls(allowEmpty: true);
            _visualEditorButton.Enabled = false;
            using var editor = new PdfmeVisualEditorForm(layout);
            if (editor.ShowDialog(this) != DialogResult.OK || editor.EditedFields is null) return;
            _fieldGridAdapter.ReplaceFields(editor.EditedFields);
        }
        catch (Exception ex) { ShowError(ex); }
        finally { _visualEditorButton.Enabled = true; }
    }

    private static void ShowError(Exception ex) => MessageBox.Show(ex.Message, "PDFme 帳票デザイナー", MessageBoxButtons.OK, MessageBoxIcon.Error);
}


