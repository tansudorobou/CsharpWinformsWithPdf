param([Parameter(Mandatory = $true)][string]$OutputDir)

$ErrorActionPreference = 'Stop'
# MSBuildからどの作業ディレクトリで呼ばれても、スクリプトの隣にある生成用ソースを参照する。
$runnerSource = Join-Path $PSScriptRoot 'pdfme-runner'
$runnerFile = Join-Path $runnerSource 'runner.mjs'
if (-not (Test-Path -LiteralPath $runnerFile)) { throw "PDFme runner not found: $runnerFile" }

$nodeCommand = Get-Command node.exe -ErrorAction Stop
$nodeExe = $nodeCommand.Source
$npmCommand = Get-Command npm.cmd -ErrorAction Stop

# OutputDirはビルド出力先またはpublish先。runner.mjsから見える位置に依存をまとめる。
$destination = [System.IO.Path]::GetFullPath($OutputDir)
[System.IO.Directory]::CreateDirectory($destination) | Out-Null
$bundle = Join-Path $destination 'pdfme-runner'
[System.IO.Directory]::CreateDirectory($bundle) | Out-Null
Copy-Item -LiteralPath $runnerFile -Destination $bundle -Force
Copy-Item -LiteralPath (Join-Path $runnerSource 'package.json') -Destination $bundle -Force
Copy-Item -LiteralPath (Join-Path $runnerSource 'package-lock.json') -Destination $bundle -Force
# C#側はrunner.mjsと同じ場所のnode.exeを優先するため、配布先にNode.jsのインストールは不要。
Copy-Item -LiteralPath $nodeExe -Destination (Join-Path $bundle 'node.exe') -Force

# Node.jsはrunner.mjsの場所からnode_modulesを探す。配布先でlockfileどおりに生成用依存だけを入れる。
# npmは外部コマンドなので、PowerShellのErrorActionPreferenceだけでは失敗を検出できない。
Push-Location -LiteralPath $bundle
try {
    & $npmCommand.Source ci --omit=dev
    if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
}
# npmが失敗しても、呼び出し元の作業ディレクトリへ戻す。
finally { Pop-Location }
Write-Host "Bundled PDFme and Node.js into $bundle"
