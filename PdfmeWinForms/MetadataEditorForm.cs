using PdfmeCore;

namespace PdfmeWinForms;

internal sealed class MetadataEditorForm : Form
{
    private sealed record LanguageChoice(string Value, string Label);

    private readonly TextBox _titleTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _authorTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _subjectTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _keywordsTextBox = new() { Dock = DockStyle.Fill, PlaceholderText = "カンマ区切り" };
    private readonly ComboBox _languageComboBox = new() { Dock = DockStyle.Left, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _creatorTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _producerTextBox = new() { Dock = DockStyle.Fill };

    public PdfDocumentMetadata EditedMetadata => new(
        _titleTextBox.Text.Trim(),
        _authorTextBox.Text.Trim(),
        _subjectTextBox.Text.Trim(),
        ParseKeywords(_keywordsTextBox.Text),
        Convert.ToString(_languageComboBox.SelectedValue) ?? "ja",
        _creatorTextBox.Text.Trim(),
        _producerTextBox.Text.Trim());

    public MetadataEditorForm(PdfDocumentMetadata metadata)
    {
        Text = "PDF文書情報";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 330);
        MinimumSize = new Size(500, 360);
        Font = new Font("Yu Gothic UI", 9);

        var languages = new[]
        {
            new LanguageChoice("ja", "日本語"),
            new LanguageChoice("en", "英語"),
            new LanguageChoice("zh", "中国語（簡体）"),
            new LanguageChoice("zh-TW", "中国語（繁体）"),
            new LanguageChoice("ko", "韓国語"),
            new LanguageChoice("de", "ドイツ語"),
            new LanguageChoice("es", "スペイン語"),
            new LanguageChoice("fr", "フランス語"),
            new LanguageChoice("it", "イタリア語"),
            new LanguageChoice("pl", "ポーランド語"),
            new LanguageChoice("th", "タイ語"),
            new LanguageChoice("ar", "アラビア語"),
        };
        _languageComboBox.DisplayMember = nameof(LanguageChoice.Label);
        _languageComboBox.ValueMember = nameof(LanguageChoice.Value);
        _languageComboBox.DataSource = languages;

        _titleTextBox.Text = metadata.Title;
        _authorTextBox.Text = metadata.Author;
        _subjectTextBox.Text = metadata.Subject;
        _keywordsTextBox.Text = string.Join(", ", metadata.Keywords ?? []);
        _languageComboBox.SelectedValue = languages.Any(item => item.Value == metadata.Language) ? metadata.Language : "ja";
        _creatorTextBox.Text = metadata.Creator;
        _producerTextBox.Text = metadata.Producer;

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 8,
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 7; row++) fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 7));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        AddField(fields, 0, "タイトル", _titleTextBox);
        AddField(fields, 1, "作成者", _authorTextBox);
        AddField(fields, 2, "件名", _subjectTextBox);
        AddField(fields, 3, "キーワード", _keywordsTextBox);
        AddField(fields, 4, "言語", _languageComboBox);
        AddField(fields, 5, "作成アプリ", _creatorTextBox);
        AddField(fields, 6, "PDF生成元", _producerTextBox);

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        fields.Controls.Add(buttons, 0, 7);
        fields.SetColumnSpan(buttons, 2);

        Controls.Add(fields);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddField(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(control, 1, row);
    }

    private static List<string> ParseKeywords(string value) => value
        .Split([',', '、', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}
