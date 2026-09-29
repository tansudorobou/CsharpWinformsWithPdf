# 構成

このリポジトリは、帳票レイアウトを編集してPDFを生成する小規模なアプリケーションです。帳票のデータ形式は `LayoutDocument`（schemaVersion 1）を共通契約とし、既存の `.layout.json` をそのまま読み込みます。

| 場所 | 責務 |
| --- | --- |
| `PdfmeCore/LayoutDocument.cs` | レイアウト・項目・PDF文書情報のデータ契約 |
| `PdfmeCore/PdfmeJsonSerializerOptions.cs` | C#とエディター・ランナー間で共通に使うJSON設定 |
| `PdfmeCore/LayoutFieldTypes.cs` | C#側で扱う項目種類と種類ごとの書式設定の特性 |
| `PdfmeCore/LayoutValidator.cs` | レイアウトの入力規則とPDFme詳細設定の衝突検査 |
| `PdfmeCore/LayoutStore.cs` | レイアウトJSONの読み書きと、保存場所またはアプリ出力先を基準にした参照ファイルの解決 |
| `PdfmeCore/ReportLayoutSaveCoordinator.cs` | レイアウトJSONと生成値クラスの保存順序・失敗時の復元 |
| `PdfmeCore/ReportValuesJsonReader.cs` | 値JSONの読み込みと文字列値への変換 |
| `PdfmeCore/ReportValuesClassGenerator.cs` | 帳票ごとの値クラスのC#ソース生成 |
| `PdfmeCore/LayoutReferencedFiles.cs` | 元のレイアウトを基準にした参照ファイルの確定と検査 |
| `PdfmeCore/ReportGenerator.cs` | PDF生成の公開API、生成ジョブの組み立てと実行の調整 |
| `PdfmeCore/IPdfmeJobRunner.cs` | Node.js実行処理を差し替える境界 |
| `PdfmeCore/PdfmeJobBuilder.cs` | レイアウトと値からPDFmeジョブを作成 |
| `PdfmeCore/NodePdfmeRunner.cs` | Node.jsプロセス、ジョブ一時ファイル、終了コードの管理 |
| `PdfmeWinForms/ReportDesignerForm.cs` | レイアウト編集画面、ダイアログと画面状態 |
| `PdfmeWinForms/LayoutFieldGridAdapter.cs` | 項目グリッドの構成と `LayoutField` への相互変換 |
| `PdfmeWinForms/PdfmeVisualEditorForm.cs` | WebView2上のpdfme Designerとの通信 |
| `PdfmeCli` | コマンド引数・標準入出力のみ |
| `GenpinTicketConsumer` | 別アプリからの利用例 |
| `pdfme-runner` / `Fonts` | 各実行アプリで共有するPDF生成ランナーとフォントのソース |
| `pdfme-editor` | WinFormsの編集画面だけが使うpdfme Designerとブラウザー用アセット |

依存方向は、WinForms/CLI/利用例 → `PdfmeCore` → `pdfme-runner/runner.mjs` → PDF生成用npmパッケージです。WinFormsの編集画面だけが `pdfme-editor` を使います。CLIと利用例のRelease出力にはUI用アセットとnpm依存を含めません。PDFme固有のジョブ形式は `PdfmeJobBuilder` に閉じ込め、保存するレイアウトJSONとは分けます。

項目種類のC#側の選択肢・検証・ジョブ変換は `LayoutFieldTypes` を共有します。Node.js側のpdfmeプラグイン登録は言語境界にあるため、`runner.mjs` とエディターのJSにも同じ種類を登録します。

生成の流れは `LayoutStore.Load` → `ReportValuesJsonReader.Parse` → `IReportGenerator.GenerateAsync` → `LayoutValidator` → `LayoutReferencedFiles` → `PdfmeJobBuilder` → `IPdfmeJobRunner` です。既定では `ReportGenerator` が `NodePdfmeRunner` を使い、テストや別の実行方法ではコンストラクターから差し替えられます。`PdfReportGenerator`、`PdfmeBridge`、`LayoutClassGenerator`、`ReportValues`、`JsonOptions` と `LayoutStore` の値JSON・検証メソッドは既存呼び出し元向けの委譲です。

編集画面の保存は、元のレイアウトを基準に参照ファイルを確定してから `ReportLayoutSaveCoordinator.Save` を使います。両ファイルの内容を先に生成して一時ファイルへ置き、値クラスを更新してからレイアウトJSONを更新します。後者が失敗した場合は値クラスを元に戻します。`LayoutStore.Save` はレイアウトJSONだけを書きます。

命名上、`PdfmeWinForms` と `PdfmeCore` は配布ファイル名・参照設定と結び付くため維持しました。画面の `Form1` は `ReportDesignerForm` に改名しました。サンプル帳票名「現品票テスト」は保存済みJSON・生成クラス・利用例で共通の識別子なので維持します。
