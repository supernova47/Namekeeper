param([string]$ValheimDir = '', [string]$BepInExDir = '', [string]$JotunnDll = '')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK 8+ is required.' }
if (-not $ValheimDir) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Steam\steamapps\common\Valheim",
        "$env:ProgramFiles\Steam\steamapps\common\Valheim",
        'D:\Games\SteamLibrary\steamapps\common\Valheim',
        'D:\SteamLibrary\steamapps\common\Valheim', 'E:\SteamLibrary\steamapps\common\Valheim'
    )
    $ValheimDir = $candidates | Where-Object { Test-Path -LiteralPath "$_\valheim_Data\Managed\assembly_valheim.dll" } | Select-Object -First 1
}
if (-not $ValheimDir -or -not (Test-Path -LiteralPath "$ValheimDir\valheim_Data\Managed\assembly_valheim.dll")) {
    throw 'Specify -ValheimDir with the folder containing valheim_Data/Managed/assembly_valheim.dll.'
}
if (-not $BepInExDir) {
    $defaultProfile = Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx'
    if (Test-Path -LiteralPath "$defaultProfile\core\BepInEx.dll") { $BepInExDir = $defaultProfile }
    elseif (Test-Path -LiteralPath "$ValheimDir\BepInEx\core\BepInEx.dll") { $BepInExDir = "$ValheimDir\BepInEx" }
}
if (-not $BepInExDir -or -not (Test-Path -LiteralPath "$BepInExDir\core\BepInEx.dll")) {
    throw 'Specify -BepInExDir for the intended profile. No plugin is installed by this script.'
}
if (-not $JotunnDll) {
    $jotunnCandidates = @(Get-ChildItem -LiteralPath "$BepInExDir\plugins" -Filter Jotunn.dll -File -Recurse)
    if ($jotunnCandidates.Count -ne 1) { throw 'Specify -JotunnDll: expected exactly one Jotunn.dll in this profile.' }
    $JotunnDll = $jotunnCandidates[0].FullName
}
if (-not (Test-Path -LiteralPath $JotunnDll)) { throw 'Jotunn.dll does not exist.' }
$project = Join-Path $root 'src\ValheimNamekeeper\Namekeeper.csproj'
@("Valheim: $ValheimDir", "BepInEx: $BepInExDir", "Jotunn: $JotunnDll") | Set-Content -LiteralPath "$root\BuildLog.txt" -Encoding utf8
& (Join-Path $root 'tests\VerifyNativeManifest.ps1') -ValheimDir $ValheimDir 2>&1 | Tee-Object -FilePath "$root\BuildLog.txt" -Append
& dotnet build $project -c Release "-p:ValheimDir=$ValheimDir" "-p:BepInExDir=$BepInExDir" "-p:JotunnDll=$JotunnDll" -p:NuGetAudit=false 2>&1 |
    Tee-Object -FilePath "$root\BuildLog.txt" -Append
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed; no release package was created.' }
& (Join-Path $root 'tests\VerifyRegistration.ps1') -CecilDll (Join-Path $BepInExDir 'core\Mono.Cecil.dll') 2>&1 | Tee-Object -FilePath "$root\BuildLog.txt" -Append
& (Join-Path $root 'tests\VerifyVisualFixes.ps1') -CecilDll (Join-Path $BepInExDir 'core\Mono.Cecil.dll') 2>&1 | Tee-Object -FilePath "$root\BuildLog.txt" -Append
& (Join-Path $root 'tests\VerifyNetworkCompatibility.ps1') -CecilDll (Join-Path $BepInExDir 'core\Mono.Cecil.dll') -JotunnDir (Split-Path -Parent $JotunnDll) 2>&1 | Tee-Object -FilePath "$root\BuildLog.txt" -Append
& (Join-Path $root 'PackageMod.ps1')
