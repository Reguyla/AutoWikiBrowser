$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Twain.Desktop\Twain.Desktop.csproj"
$publishDir = Join-Path $repoRoot "artifacts\Twain\win-x64"
$outputDir = "C:\TwainVelopackTest"

Write-Host "Reading Twain version from NBGV..."
$versionInfo = nbgv get-version --format json | ConvertFrom-Json
$twainVersion = $versionInfo.SimpleVersion

Write-Host "Packaging Twain version $twainVersion"

if (Test-Path $publishDir)
{
    Write-Host "Cleaning previous publish output..."
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Publishing Twain.Desktop..."
dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $publishDir

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $outputDir))
{
    Write-Host "Creating Velopack output directory..."
    New-Item -ItemType Directory -Force $outputDir | Out-Null
}

Write-Host "Creating Velopack package..."
vpk pack `
    --packId Twain `
    --packVersion $twainVersion `
    --packDir $publishDir `
    --mainExe Twain.Desktop.exe `
    --packTitle Twain `
    --outputDir $outputDir

if ($LASTEXITCODE -ne 0)
{
    throw "Velopack packaging failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Twain $twainVersion packaged successfully."
Write-Host "Output: $outputDir"