# C#からpdfmeを利用するサンプル

このリポジトリは、JavaScript製の[pdfme](https://pdfme.com/)をC#（.NET 10）から使うためのサンプルです。C#側の `PdfmeCore` がレイアウトと入力値を受け取り、ローカルのNode.jsプロセスでpdfmeを実行してPDFを作ります。PDF生成時にネット接続は不要です。

サンプルには、レイアウトを編集するWinFormsアプリ（`PdfmeWinForms`）、C#からPDFを生成する共通コード（`PdfmeCore`）、CLI（`PdfmeCli`）、別のWinFormsアプリから生成APIを呼ぶ例（`GenpinTicketConsumer`）が含まれます。帳票ごとにレイアウトJSONを保存し、JSON文字列や値JSONファイルで項目の値を渡せます。背景には既存の1ページPDFか白紙の用紙を使います。

コードの責務と依存関係は [ARCHITECTURE.md](ARCHITECTURE.md) にまとめています。
エディターの `dist` とRelease同梱の手順は [BUNDLE_AND_EDITOR.md](BUNDLE_AND_EDITOR.md) で説明しています。

## 開発と実行

必要なもの: Windows、.NET 10 SDK、Node.js 20以降。

```powershell
cd pdfme-editor
npm ci
cd ..
cd pdfme-runner
npm ci --omit=dev
cd ..
dotnet run --project PdfmeWinForms\PdfmeWinForms.csproj
```

初期画面の例は白紙A4に4項目を置きます。白紙PDFでは用紙サイズ（A4、A5、Letter、カスタム）と縦横を設定できます。カスタムでは幅と高さをmmで指定します。「元のPDF」で手持ちの帳票を選ぶと、そのPDFのページサイズをそのまま使い、用紙設定は無効になります。「PDF文書情報」ではタイトル、作成者、件名、キーワード、言語、作成アプリ、PDF生成元を設定できます。タイトルを空欄にすると帳票名が使われます。表のX/Y/幅/高さはミリメートル、原点はページ左上です。テキスト項目は横揃え（`left`、`center`、`right`、`justify`）と縦揃え（`top`、`middle`、`bottom`）を指定できます。「PDF上で配置を編集」を押すと、WebView2上のpdfme Designerで背景PDFを見ながら項目をドラッグして移動・サイズ変更でき、右側の設定欄で文字色、背景色、回転、透明度、行間、文字間隔、下線、枠線、余白、自動文字サイズ、バーコード色などを設定できます。「配置を反映」で基本項目とPDFme固有の詳細設定を表へ戻し、レイアウトJSONへ保存します。

帳票名を付けて「配置を保存」すると、初回は `<帳票名>.layout.json` が提案されます。既定の保存先はドキュメント内の `PdfmeWinForms\Layouts` です。保存時には同じ場所へ `<帳票名>.Values.cs` も生成します。クラスのプロパティはレイアウト項目に対応する PascalCase の `string` 型です。`JsonPropertyName` 属性で元の項目名との対応を維持します。保存後は同じファイルを更新し、「名前を付けて保存」で別の帳票として分けられます。「新規」で空の配置を始め、「配置を開く」で保存済み帳票に切り替えます。以前の `layout.json` も読み込めます。各項目の「値」はサンプル値・既定値として保存され、外部の値JSONで上書きできます。

WebView2 Runtimeが必要です。入っていない環境ではMicrosoftのWebView2 Runtimeをインストールしてください。開発時には `pdfme-editor` で `npm ci` を実行してpdfme UIとビルドツールを導入します。`dotnet build` 時にブラウザー用JavaScriptとPDF描画ワーカーを生成・コピーします。元PDFを表示するときには両方が必要です。PDF生成用の `pdfme-runner` は別のnpmパッケージです。

既存PDFの固定内容は編集しません。可変内容は重ね描きします。日本語フォントの初期値には同梱の `Fonts\MPLUS1p-Regular.ttf` を使います。M PLUS 1pはSIL Open Font License 1.1で、商用利用とアプリへの同梱が許可されています。ライセンス全文は `Fonts\OFL.txt` を参照してください。別のTTFフォントも選択できます。

## Releaseへの同梱

```powershell
dotnet build PdfmeWinForms.slnx -c Release
```

出力先 `PdfmeWinForms\bin\Release\net10.0-windows\` にWinFormsアプリ、`pdfme-editor`、M PLUS 1pフォントとライセンス、生成専用の `pdfme-runner` と `node.exe` が揃います。`PdfmeCli\bin\Release\net10.0-windows\` と `GenpinTicketConsumer\bin\Release\net10.0-windows\` には生成専用ランナーだけを同梱します。使う方のフォルダー全体を配布してください。配布先にNode.jsのインストールは不要です。GUIには.NET 10 Desktop RuntimeとWebView2 Runtime、CLIには.NET 10 Runtimeが必要です。

.NETランタイムも含める場合:

```powershell
dotnet publish PdfmeWinForms\PdfmeWinForms.csproj -c Release -r win-x64 --self-contained true -o publish
```

`publish` フォルダー全体を配布します。Releaseビルド時にNode.jsの実行ファイルをコピーするため、ビルドするPCにはNode.jsが必要です。

## 別アプリからPDFを生成

保存された「現品票テスト」のレイアウトを使う [WinForms入力アプリ](GenpinTicketConsumer/README.md) を `GenpinTicketConsumer` に用意しています。画面の入力値を生成クラスに入れ、JSON文字列にして `GenerateFromJsonAsync` でPDFを生成します。

レイアウトは帳票ごとに1つのJSONファイルです。値JSONは項目名をキーとするオブジェクトにします。[サンプルレイアウト](examples/sample.layout.json)と[サンプル値](examples/sample.values.json)を同梱しています。値JSONにない項目はレイアウト内の `value` が使われ、レイアウトにない項目名はエラーになります。

```powershell
dotnet run --project PdfmeCli\PdfmeCli.csproj -- --layout examples\sample.layout.json --data examples\sample.values.json --output sample.pdf
```

配布版では `PdfmeCli.exe --layout ... --data ... --output ...` を実行できます。成功時の終了コードは0、入力エラーや生成失敗は1、引数エラーは2です。元PDFへのパスはレイアウトJSONを基準とした相対パスか絶対パスで指定できます。同梱フォントは `Fonts\MPLUS1p-Regular.ttf` で参照できます。

値JSONファイルを作らずに渡す場合は `--json` を使います。`--json -` では標準入力からJSONデータを受け取ります。

```powershell
$json = '{"partNo":"PART-001","qr":"PART-001"}'
dotnet run --project PdfmeCli\PdfmeCli.csproj -- --layout examples\sample.layout.json --json $json --output sample.pdf
```

C#からは `PdfmeCore` プロジェクトを参照し、同じレイアウト・値JSONを渡せます。

```csharp
using PdfmeCore;

var generator = new ReportGenerator();
await generator.GenerateFromFilesAsync(
    @"C:\reports\invoice.layout.json",
    @"C:\reports\invoice.values.json",
    @"C:\reports\invoice.pdf");
```

値をコードで渡す場合は `LayoutStore.Load` と `ReportGenerator.GenerateAsync` を使います。

```csharp
var layout = LayoutStore.Load(@"C:\reports\invoice.layout.json");
var values = new Dictionary<string, string> { ["partNo"] = "PART-001" };
var generator = new ReportGenerator();
await generator.GenerateAsync(layout, @"C:\reports\invoice.pdf", values);
```

JSON文字列のまま渡すC# APIもあります。

```csharp
var generator = new ReportGenerator();
await generator.GenerateFromJsonAsync(
    @"C:\reports\invoice.layout.json",
    """{"partNo":"PART-001"}""",
    @"C:\reports\invoice.pdf");
```

`PdfmeCore` はPDF生成時に `pdfme-runner` を使用します。別のC#アプリに組み込むときは、アプリの出力フォルダーへRelease版の `pdfme-runner` を同梱するか、`GenerateAsync` の `runnerPath` 引数で `runner.mjs` を指定してください。このサンプルは1ページの帳票に対応します。

PDFme本体はMITライセンスです。配布物内の各パッケージとNode.jsのライセンス表示は、それぞれの同梱ファイルを参照してください。


