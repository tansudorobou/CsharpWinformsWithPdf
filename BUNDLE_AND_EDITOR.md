# PDFmeの同梱とエディターのビルド

この文書では、`bundle-pdfme.ps1` と `pdfme-editor` のビルド処理を説明します。両者は別のものを配布先へ用意します。`bundle-pdfme.ps1` が用意するのはPDF生成用のNode.jsランナーです。画面で使うエディターは `PdfmeWinForms.csproj` のビルド処理が用意します。

## 最初にフォルダー名を整理する

`pdfme-editor` は、編集画面用のnpmプロジェクトです。その中の `editor` は、HTMLと編集処理のJavaScriptを置いている**ソースフォルダー**です。ビルドを始める前から存在します。

```text
pdfme-editor/
├─ package.json           npmの依存とbuildコマンド
├─ build-editor.mjs        ブラウザー用ファイルを作るスクリプト
└─ editor/                編集画面のソースを置く既存フォルダー
   ├─ editor.html          WebView2が開くHTML
   ├─ editor.js            編集処理のソース
   └─ dist/                ビルドで作る配布用ファイル
      ├─ editor.js         esbuildが作るブラウザー用JS
      └─ assets/
         └─ clawpdf-worker-*.js  PDF表示用ワーカー
```

`build-editor.mjs` の `mkdir('./editor/dist/', { recursive: true })` は、出力先の `dist` を確保するための処理です。通常のビルドで `editor` 自体を新規作成しているわけではありません。`editor` が空でソースの `editor.js` がなければ、その後のesbuildで失敗します。

現在は `editor/dist` の生成済みファイルもGitで管理しています。そのため、リポジトリを取得した直後から `dist` が見えることがあります。それでも `PdfmeWinForms` のビルド時には `npm run build` を実行し、インストール済みの依存から作り直します。

## エディターのビルド

`PdfmeWinForms/PdfmeWinForms.csproj` の `BuildPdfmeEditor` ターゲットが、`pdfme-editor` を作業ディレクトリにして `npm run build` を実行します。npmは `package.json` に従って `node build-editor.mjs` を起動します。`npm ci` はこのターゲットでは実行しないので、開発環境では先に `pdfme-editor` で `npm ci` が必要です。

`build-editor.mjs` は、次の二つを行います。

1. esbuildで `editor/editor.js` を `editor/dist/editor.js` に変換します。ソースの `import { Designer } from '@pdfme/ui'` などの依存をまとめ、WebView2で読み込めるJavaScriptにします。`bundle: true` が依存の取り込み、`platform: 'browser'` がブラウザー向けの出力指定です。`minify: true` でファイルを小さくし、`format: 'esm'` でHTMLの `<script type="module">` に合わせます。
2. `@pdfme/converter` が提供するPDF表示ワーカーを `editor/dist/assets/` へコピーします。ワーカーは `editor.js` とは別ファイルとして読み込まれるため、JavaScriptをまとめるだけでは足りません。

`new URL('./editor/...', import.meta.url)` は、ビルドスクリプト自身の場所を基準に入力と出力のパスを決めます。`mkdir(..., { recursive: true })` は、まだ `dist` がない環境でも書き込めるようにするためです。ワーカーのコピーでは `{ recursive: true, force: true }` を使い、`assets` の中身を再ビルド時にも更新します。

`external: ['node:*']` は、依存の中にあるNode.js組み込みモジュールへの参照をesbuildが解決しないようにする指定です。これでNode.jsのAPIがブラウザーで使えるようになるわけではありません。実行時にその参照へ到達するコードがあれば、ブラウザー側で失敗します。

`dist` を `editor` の中に置いている理由は、`editor.html` から `<script type="module" src="./dist/editor.js">` という相対パスで読めるようにするためです。HTML、完成したJavaScript、ワーカーを `editor` の下にまとめれば、WebView2へ渡すフォルダーも一つで済みます。pdfmeやesbuildが要求する固定のフォルダー名ではありません。別の配置に変えることはできますが、HTMLの参照先、ビルドの出力先、`.csproj` のコピー先、C#側の参照先を合わせて変更する必要があります。

## .NETの出力先にある `editor` フォルダー

上の `editor/dist` はソースツリー側のビルド出力です。実行時にWebView2が読むのは、さらに `.NET` の出力フォルダーへコピーされたファイルです。例えばReleaseビルド後は次のようになります。

```text
PdfmeWinForms/bin/Release/net10.0-windows/
├─ PdfmeWinForms.exe
├─ pdfme-editor/
│  └─ editor/
│     ├─ editor.html
│     └─ dist/
│        ├─ editor.js
│        └─ assets/clawpdf-worker-*.js
└─ pdfme-runner/
   ├─ runner.mjs
   ├─ node.exe
   ├─ package.json
   ├─ package-lock.json
   └─ node_modules/
```

出力先の `pdfme-editor/editor` は、`PdfmeWinForms.csproj` の `Content` 項目にある `Link="pdfme-editor\editor\..."` に従って作られます。HTMLと完成した `editor.js` はそこでコピーします。ワーカーはビルド中に生成される名前付きファイルなので、`CopyPdfmeEditorWorker` ターゲットがビルド後に `assets/*.js` を探してコピーします。publish先にも同じファイルが必要なため、`CopyPdfmeEditorWorkerForPublish` もあります。

`PdfmeVisualEditorForm` は、出力先の `pdfme-editor/editor` を `https://pdfme.local/` に対応付けて `editor.html` を開きます。HTMLからの `./dist/editor.js` は、この対応付けによって同じフォルダー内のファイルを指します。エディターが起動時にnpmパッケージを探すことはありません。

画面を開いた後は、C#がレイアウト、背景PDF、フォントをWebView2へ送ります。`editor/editor.js` はそれらをpdfmeの `Designer` が受け取る形式へ変換します。「配置を反映」を押すと、編集後の項目をC#へ送り返します。この通信は `PdfmeVisualEditorForm.cs` の `WebMessageReceived` とJavaScriptの `postMessage` で行います。

### `editor/editor.js` のデータ変換

画面の読み込みと編集結果の受け渡しも、ビルド処理とは別に押さえると分かりやすくなります。

1. HTMLが `dist/editor.js` を読み込むと、JavaScriptは `ready` をC#へ送ります。C#はレイアウトと、必要なら背景PDFとフォントをBase64にして `load` を返します。ファイルをブラウザーから直接開く処理にはしていません。
2. `load` を受けたJavaScriptは、保存用の `layout.fields` をDesigner用の `schemas` へ変換します。`x` と `y` は `position` に、`value` は `content` に移します。`pdfmeOptions` を先に展開し、レイアウトの基本項目で上書きして、同じ設定が二箇所にあるときの値を決めます。
3. 背景PDFは `data:application/pdf;base64,...` として `basePdf` に渡します。フォントはBase64をバイト列に戻し、`ReportFont` の名前でDesignerに登録します。`designer?.destroy()` は、再読み込み時に以前のDesignerを残さないためです。
4. 「配置を反映」を押すと、Designerの `schemas` を保存用の `fields` に戻して `apply` をC#へ送ります。ここでは1ページだけを許可します。Designer内部の `id` と実行時に付けた `fontName` は保存せず、残りの詳細設定は `pdfmeOptions` に残します。`content` がない項目には読み込み時の `originalValues` を使います。

この画面は配置データをC#へ返す役割です。PDFファイルの生成は、後述の `pdfme-runner/runner.mjs` が担当します。

## `bundle-pdfme.ps1` の処理

このスクリプトはPDFを生成するアプリのReleaseビルドとpublishで呼ばれます。`PdfmeWinForms` だけでなく、`PdfmeCli` と `GenpinTicketConsumer` も使います。エディターのHTMLやJavaScriptは扱いません。

| 処理 | 理由 |
| --- | --- |
| `$PSScriptRoot` を基準に `pdfme-runner` を探す | MSBuildから呼ばれたときの作業ディレクトリに依存しないため。 |
| `-OutputDir` を絶対パスにし、その下に `pdfme-runner` を作る | 実行アプリごとの出力先とpublish先に、同じ構成でランナーを置くため。 |
| `runner.mjs` と `package.json`、`package-lock.json` をコピーする | 実行コードと、依存を固定するnpmの情報が必要なため。 |
| ビルドPCの `node.exe` を `runner.mjs` の隣へコピーする | `PdfmeCore/NodePdfmeRunner.cs` は同じ場所の `node.exe` を優先する。配布先にNode.jsを別途インストールせずに済む。 |
| 配布先で `npm ci --omit=dev` を実行する | Node.jsは `runner.mjs` の場所から `node_modules` を探す。生成用パッケージのlockfileに従い、配布先へ実行時の依存を入れるため。 |
| `$LASTEXITCODE` を確認し、`finally` で元の場所へ戻る | npmは外部コマンドなので、PowerShellの `$ErrorActionPreference` だけでは終了コードを判定できない。失敗しても作業ディレクトリを戻す。 |

`pdfme-runner/package.json` には `@pdfme/generator` と `@pdfme/schemas` など、生成側の依存を定義しています。`@pdfme/ui` は `pdfme-editor/package.json` 側です。この分離により、`GenpinTicketConsumer` のRelease出力にはエディターのファイルとUI用npmパッケージを同梱しません。

`npm ci` を配布先で実行するため、Releaseビルドを行うPCにはNode.js、npm、依存を取得できるnpmキャッシュまたはネット接続が必要です。完成した配布フォルダーを実行するPCにはnpmは不要です。

## 処理を追う順番

編集画面を追うなら、`PdfmeWinForms.csproj` の `BuildPdfmeEditor` → `pdfme-editor/build-editor.mjs` → `editor/editor.html` と `editor/editor.js` → `PdfmeVisualEditorForm.cs` の順に読むと、ビルドから画面表示までつながります。

PDF生成を追うなら、`PdfmeCore/NodePdfmeRunner.cs` → `pdfme-runner/runner.mjs` を見てください。Release版のファイル配置は `bundle-pdfme.ps1` が担当します。
