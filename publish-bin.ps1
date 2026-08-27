$ErrorActionPreference = 'Stop'

$workspacePath = $PSScriptRoot
$outputPath = Join-Path $workspacePath 'bin'
$appOutputPath = Join-Path $outputPath 'app'

dotnet publish (Join-Path $workspacePath 'PictureTransformer.Cli\PictureTransformer.Cli.csproj') -c Release -r win-x64 --self-contained true -o $outputPath
if ($LASTEXITCODE -ne 0) { throw 'CLI publish failed.' }
dotnet publish (Join-Path $workspacePath 'PictureTransformer\PictureTransformer.csproj') -c Release -r win-x64 --self-contained true -o $appOutputPath
if ($LASTEXITCODE -ne 0) { throw 'GUI publish failed.' }

Write-Host "Published to: $outputPath"
Write-Host 'Add this bin directory to PATH manually.'
