using System.Globalization;
using System.Text.Json;
using PdfmeCore;

namespace PdfmeWinForms;

/// <summary>グリッドの表示値とレイアウト項目の相互変換を担当する。</summary>
internal sealed class LayoutFieldGridAdapter
{
    private sealed record AlignmentChoice(string Value, string Label);
    private sealed record RowState(string Kind, Dictionary<string, JsonElement>? PdfmeOptions);

    public DataGridView Grid { get; } = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = true,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersWidth = 30,
    };

    public LayoutFieldGridAdapter()
    {
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "項目名", FillWeight = 95 });
        var kind = new DataGridViewComboBoxColumn { Name = "kind", HeaderText = "種類", FillWeight = 75 };
        kind.Items.AddRange(LayoutFieldTypes.All.Select(type => (object)type.Id).ToArray());
        Grid.Columns.Add(kind);
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "x", HeaderText = "X (mm)", FillWeight = 55 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "y", HeaderText = "Y (mm)", FillWeight = 55 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "width", HeaderText = "幅 (mm)", FillWeight = 55 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "height", HeaderText = "高さ (mm)", FillWeight = 55 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "fontSize", HeaderText = "文字 pt", FillWeight = 55 });
        Grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "alignment",
            HeaderText = "横揃え",
            FillWeight = 65,
            DisplayMember = nameof(AlignmentChoice.Label),
            ValueMember = nameof(AlignmentChoice.Value),
            DataSource = new[]
            {
                new AlignmentChoice("left", "左"),
                new AlignmentChoice("center", "中央"),
                new AlignmentChoice("right", "右"),
                new AlignmentChoice("justify", "均等"),
            },
        });
        Grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "verticalAlignment",
            HeaderText = "縦揃え",
            FillWeight = 65,
            DisplayMember = nameof(AlignmentChoice.Label),
            ValueMember = nameof(AlignmentChoice.Value),
            DataSource = new[]
            {
                new AlignmentChoice("top", "上"),
                new AlignmentChoice("middle", "中央"),
                new AlignmentChoice("bottom", "下"),
            },
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "value", HeaderText = "値", FillWeight = 170 });
        Grid.DataError += (_, e) =>
        {
            MessageBox.Show($"{e.RowIndex + 1}行目の入力を確認してください。", "入力エラー");
            e.ThrowException = false;
        };
    }

    public void ClearFields() => Grid.Rows.Clear();

    public void ReplaceFields(IEnumerable<LayoutField> fields)
    {
        ClearFields();
        foreach (var field in fields) AddField(field);
    }

    public void AddField(LayoutField field)
    {
        var index = Grid.Rows.Add(
            field.Name, field.Kind, field.X, field.Y, field.Width, field.Height, field.FontSize,
            field.Alignment, field.VerticalAlignment, field.Value);
        Grid.Rows[index].Tag = new RowState(field.Kind, field.PdfmeOptions);
    }

    public List<LayoutField> ReadFields(bool allowEmpty = false)
    {
        Grid.EndEdit();
        var fields = new List<LayoutField>();
        foreach (DataGridViewRow row in Grid.Rows)
        {
            if (row.IsNewRow) continue;
            string Cell(string column) => Convert.ToString(row.Cells[column].Value, CultureInfo.CurrentCulture)?.Trim() ?? "";
            var name = Cell("name");
            if (name.Length == 0)
            {
                if (row.Cells.Cast<DataGridViewCell>().Any(cell =>
                    !string.IsNullOrWhiteSpace(Convert.ToString(cell.Value, CultureInfo.CurrentCulture))))
                    throw new InvalidOperationException($"{row.Index + 1}行目: 項目名を入力してください。");
                continue;
            }
            var kind = Cell("kind");
            if (LayoutFieldTypes.FindById(kind) is null)
                throw new InvalidOperationException($"{name}: 種類を選択してください。");

            double Number(string column, double fallback = 0)
            {
                var value = Cell(column);
                if (value.Length == 0) return fallback;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var number) ||
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    return number;
                throw new InvalidOperationException($"{name}: {column} は数値で入力してください。");
            }

            var rowState = row.Tag as RowState;
            var pdfmeOptions = rowState?.Kind == kind ? rowState.PdfmeOptions : null;
            var alignment = Cell("alignment");
            var verticalAlignment = Cell("verticalAlignment");
            fields.Add(new LayoutField(
                name, kind, Number("x"), Number("y"), Number("width"), Number("height"),
                Number("fontSize", 12), Cell("value"),
                alignment.Length == 0 ? "left" : alignment,
                verticalAlignment.Length == 0 ? "top" : verticalAlignment,
                pdfmeOptions));
        }

        if (fields.Count == 0 && !allowEmpty)
            throw new InvalidOperationException("項目を1つ以上入力してください。");
        if (fields.Count > 0) LayoutValidator.ValidateFields(fields);
        return fields;
    }
}
