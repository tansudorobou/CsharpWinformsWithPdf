param([Parameter(Mandatory = $true)][string]$OutputDir)

$ErrorActionPreference = 'Stop'
$runnerSource = Join-Path $PSScriptRoot 'pdfme-runner'
$runnerFile = Join-Path $runnerSource 'runner.mjs'
$moduleDir = Join-Path $runnerSource 'node_modules'
if (-not (Test-Path -LiteralPath $runnerFile)) { throw "PDFme runner not found: $runnerFile" }

$nodeCommand = Get-Command node.exe -ErrorAction Stop
$nodeExe = $nodeCommand.Source
$npmCommand = Get-Command npm.cmd -ErrorAction Stop
if (-not (Test-Path -LiteralPath $moduleDir)) {
    Push-Location -LiteralPath $runnerSource
    try {
        & $npmCommand.Source ci --omit=dev
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
    }
    finally { Pop-Location }
}

$destination = [System.IO.Path]::GetFullPath($OutputDir)
[System.IO.Directory]::CreateDirectory($destination) | Out-Null
$bundle = Join-Path $destination 'pdfme-runner'
[System.IO.Directory]::CreateDirectory($bundle) | Out-Null
Copy-Item -LiteralPath $runnerFile -Destination $bundle -Force
Copy-Item -LiteralPath (Join-Path $runnerSource 'editor') -Destination $bundle -Recurse -Force
Copy-Item -LiteralPath (Join-Path $runnerSource 'package.json') -Destination $bundle -Force
Copy-Item -LiteralPath (Join-Path $runnerSource 'package-lock.json') -Destination $bundle -Force
Copy-Item -LiteralPath $nodeExe -Destination (Join-Path $bundle 'node.exe') -Force
Copy-Item -LiteralPath $moduleDir -Destination $bundle -Recurse -Force
Write-Host "Bundled PDFme and Node.js into $bundle"
