$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$manifest = Get-Content -LiteralPath "$root\manifest.json" -Raw | ConvertFrom-Json
$version = [string]$manifest.version_number
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid manifest version.' }
$dll = Join-Path $root 'src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Compile the mod before packaging.' }
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString() -ne "$version.0") { throw 'DLL version does not match manifest.' }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$staging = Join-Path $dist ('.staging-' + [Guid]::NewGuid().ToString('N'))
$folderName = "ValheimNamekeeper-$version"
try {
    $installRoot = Join-Path $staging "install\$folderName"
    $pluginRoot = Join-Path $installRoot 'plugins\SuperNova.ValheimNamekeeper'
    New-Item -ItemType Directory -Path $pluginRoot -Force | Out-Null
    Copy-Item -LiteralPath $dll -Destination $pluginRoot
    foreach ($name in @('manifest.json','README.md','changelog.md','FONT_LICENSE.txt')) { Copy-Item -LiteralPath "$root\$name" -Destination $installRoot }
    $testPlan = Join-Path $root ("docs\TEST_PLAN_" + $version + ".md")
    if (Test-Path -LiteralPath $testPlan) { Copy-Item -LiteralPath $testPlan -Destination $installRoot }
    Compress-Archive -LiteralPath $installRoot -DestinationPath "$dist\$folderName.zip" -Force

    $sourceRoot = Join-Path $staging "source\$folderName"
    foreach ($relative in @('src\ValheimNamekeeper','tests','docs')) {
        $target = Join-Path $sourceRoot $relative
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        Get-ChildItem -LiteralPath "$root\$relative" -File | Where-Object { $_.Extension -in '.cs','.csproj','.md','.ps1','.py','.txt' } |
            ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $target }
    }
    Copy-Item -LiteralPath (Join-Path $root 'src\ValheimNamekeeper\Assets') -Destination (Join-Path $sourceRoot 'src\ValheimNamekeeper\Assets') -Recurse
    foreach ($name in @('manifest.json','README.md','changelog.md','FONT_LICENSE.txt','PROJECT_CONTEXT.md','BuildMod.bat','BuildMod.ps1','PackageMod.ps1','PackageThunderstore.ps1','BUILD_STATUS.md','.gitignore')) {
        if (Test-Path -LiteralPath "$root\$name") { Copy-Item -LiteralPath "$root\$name" -Destination $sourceRoot }
    }
    Compress-Archive -LiteralPath $sourceRoot -DestinationPath "$dist\$folderName-SOURCE.zip" -Force
    Write-Output "Packages created: $dist\$folderName.zip and $folderName-SOURCE.zip"
}
finally {
    # Delete only the exact uniquely created staging child of this project's dist.
    $resolvedStaging = [IO.Path]::GetFullPath($staging)
    if ([IO.Path]::GetDirectoryName($resolvedStaging) -ne [IO.Path]::GetFullPath($dist) -or
        [IO.Path]::GetFileName($resolvedStaging) -notmatch '^\.staging-[a-f0-9]{32}$') { throw 'Unsafe staging path.' }
    if (Test-Path -LiteralPath $resolvedStaging) { Remove-Item -LiteralPath $resolvedStaging -Recurse -Force }
}


