# 現品票テストの入力アプリ

`GenpinTicketConsumer` は、保存済みの「現品票テスト」レイアウトを使う WinForms アプリです。品番・ロット・QRコード・バーコードを入力して「PDFを生成」を押すと、保存先を選んでPDFを作成します。初期値は同梱レイアウトのサンプル値です。

```powershell
dotnet run --project GenpinTicketConsumer\GenpinTicketConsumer.csproj
```

レイアウト、背景PDF、フォントはこのプロジェクトに同梱しています。アプリは[生成された値クラス](Layouts/現品票テスト.Values.cs)に画面の入力値を入れ、`JsonSerializer.Serialize` でJSON文字列に変換し、次のAPIでPDFを作ります。

```csharp
await new ReportGenerator().GenerateFromJsonAsync(layoutPath, json, outputPath);
```

この例の実装は [TicketForm.cs](TicketForm.cs) にあります。レイアウトを変更した場合は、編集アプリで保存して再生成された `.Values.cs` と `.layout.json` を、このプロジェクトの `Layouts` にコピーしてください。背景PDFを変更した場合は `Assets` のPDFも更新してください。

ReleaseビルドにはPDFmeランナーとNode.jsが同梱されます。配布時は `GenpinTicketConsumer\bin\Release\net10.0-windows\` フォルダー全体を使用します。

