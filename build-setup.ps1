$ErrorActionPreference = 'Stop'

$workspacePath = $PSScriptRoot
$distributionPath = Join-Path $workspacePath 'dist'
$bundleProjectPath = Join-Path $workspacePath 'Installer\Bundle\PictureTransformer.Bundle.wixproj'
$packageOutputPath = Join-Path $workspacePath 'Installer\Package\bin\x64\Release\PictureTransformer.msi'
$bundleOutputPath = Join-Path $workspacePath 'Installer\Bundle\bin\x64\Release\PictureTransformer-Setup.exe'

& (Join-Path $workspacePath 'publish-bin.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
dotnet build $bundleProjectPath -c Release
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }

New-Item -ItemType Directory -Path $distributionPath -Force | Out-Null
Copy-Item -LiteralPath $packageOutputPath -Destination (Join-Path $distributionPath 'PictureTransformer.msi') -Force
Copy-Item -LiteralPath $bundleOutputPath -Destination (Join-Path $distributionPath 'PictureTransformer-Setup.exe') -Force

Write-Host "Installer created: $distributionPath"
