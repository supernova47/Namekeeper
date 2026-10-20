$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($PSScriptRoot)
$manifestPath = Join-Path $root 'manifest.json'
$readmePath = Join-Path $root 'README.md'
$iconPath = Join-Path $root 'icon.png'
$dllPath = Join-Path $root 'src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = [string]$manifest.version_number
if ($manifest.name -cnotmatch '^[A-Za-z0-9_]+$') { throw 'Thunderstore package name must contain only ASCII letters, digits, or underscores.' }
if ([string]$manifest.author -cne 'SuperNova') { throw 'The shared Thunderstore/r2modman package author must be SuperNova.' }
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid semantic version in manifest.' }
if ([string]::IsNullOrWhiteSpace([string]$manifest.description) -or $manifest.description.Length -gt 250) { throw 'Thunderstore description must contain 1-250 characters.' }
foreach ($required in @($manifestPath,$readmePath,$iconPath,$dllPath,(Join-Path $root 'changelog.md'),(Join-Path $root 'LICENSE'),(Join-Path $root 'FONT_LICENSE.txt'))) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required package file missing: $required" }
}
if ([Reflection.AssemblyName]::GetAssemblyName($dllPath).Version.ToString() -ne "$version.0") { throw 'DLL assembly version does not match manifest.' }
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try { if ($icon.Width -ne 256 -or $icon.Height -ne 256) { throw 'Thunderstore icon must be exactly 256x256.' } }
finally { $icon.Dispose() }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$outputs = @(
    # One shared release archive, also used for local r2modman imports.
    [pscustomobject]@{ Folder = 'thunderstore'; FileName = "ValheimNamekeeper-$version.zip" }
)
foreach ($output in $outputs) {
    $dist = Join-Path $root "dist\$($output.Folder)"
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    $archivePath = Join-Path $dist $output.FileName
    $resolvedArchive = [IO.Path]::GetFullPath($archivePath)
    if (-not $resolvedArchive.StartsWith([IO.Path]::GetFullPath($dist) + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write archive outside this project's $($output.Folder) output folder."
    }
    if (Test-Path -LiteralPath $resolvedArchive) { Remove-Item -LiteralPath $resolvedArchive -Force }
    $archive = [IO.Compression.ZipFile]::Open($resolvedArchive,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $packageManifest = [ordered]@{
            name = [string]$manifest.name
            author = [string]$manifest.author
            version_number = $version
            website_url = [string]$manifest.website_url
            description = [string]$manifest.description
            dependencies = @($manifest.dependencies)
        }
        $manifestEntry = $archive.CreateEntry('manifest.json',[IO.Compression.CompressionLevel]::Optimal)
        $manifestWriter = [IO.StreamWriter]::new($manifestEntry.Open(),[Text.UTF8Encoding]::new($false))
        try { $manifestWriter.Write(($packageManifest | ConvertTo-Json -Depth 5)) }
        finally { $manifestWriter.Dispose() }

        foreach ($name in @('README.md','icon.png','changelog.md','LICENSE','FONT_LICENSE.txt')) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,(Join-Path $root $name),$name,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        $pluginEntry = 'BepInEx/plugins/SuperNova.ValheimNamekeeper/SuperNova.ValheimNamekeeper.dll'
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$dllPath,$pluginEntry,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $testPlan = Join-Path $root "docs\TEST_PLAN_$version.md"
        if (Test-Path -LiteralPath $testPlan -PathType Leaf) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$testPlan,"docs/TEST_PLAN_$version.md",[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        $localizedTestPlan = Join-Path $root "docs\TEST_PLAN_$version.ru.md"
        if (Test-Path -LiteralPath $localizedTestPlan -PathType Leaf) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$localizedTestPlan,"docs/TEST_PLAN_$version.ru.md",[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        $networkGuide = Join-Path $root "docs\NETWORKING_$version.md"
        if (Test-Path -LiteralPath $networkGuide -PathType Leaf) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$networkGuide,"docs/NETWORKING_$version.md",[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $archive.Dispose() }
    Write-Output "Shared Thunderstore/r2modman package created: $resolvedArchive"
}

