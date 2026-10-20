param(
    [string]$DllPath = (Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'),
    [string]$CecilDll = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\core\Mono.Cecil.dll'),
    [string]$JotunnDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\plugins\ValheimModding-Jotunn'),
    [string]$ValheimDir = 'D:\Games\SteamLibrary\steamapps\common\Valheim'
)
$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath $CecilDll
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory([IO.Path]::GetFullPath($JotunnDir))
$reader = New-Object Mono.Cecil.ReaderParameters
$reader.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($DllPath), $reader)
function TypeNamed($name) { $assembly.MainModule.Types | Where-Object FullName -eq ("SuperNova.ValheimNamekeeper." + $name) }
function CodeOf($type, $methodName) {
    $method = $type.Methods | Where-Object Name -eq $methodName | Select-Object -First 1
    if (-not $method -or -not $method.HasBody) { throw "Missing compiled method $($type.Name).$methodName" }
    return [string]::Join(' ', $method.Body.Instructions)
}
try {
    $damage = CodeOf (TypeNamed 'NamekeeperSanctuaryDamageGuard') 'Prefix'
    $buildingDamage = CodeOf (TypeNamed 'NamekeeperSanctuaryBuildingDamageGuard') 'Prefix'
    if ($damage -notmatch 'ShopSanctuary::Contains' -or $damage -notmatch 'Character::IsPlayer' -or
        $buildingDamage -notmatch 'ShopSanctuary::Contains') {
        throw 'The camp must reject networked damage to objects and non-player characters inside its radius.'
    }
    Write-Output 'PASS damage guards protect camp pieces and the boar on client/server damage paths'

    $playerInput = CodeOf (TypeNamed 'NamekeeperSanctuaryPlayerInputGuard') 'Prefix'
    $attacks = CodeOf (TypeNamed 'NamekeeperSanctuaryHumanoidAttackGuard') 'Prefix'
    $powers = CodeOf (TypeNamed 'NamekeeperSanctuaryGuardianPowerGuard') 'Prefix'
    $locationSetup = CodeOf (TypeNamed 'WorldCamp') 'InstallTerrainAndArea'
    $sourcePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\ShopSanctuary.cs'))
    $source = Get-Content -LiteralPath $sourcePath -Raw
    if ($locationSetup -notmatch 'ShopSanctuary::InstallLocationFeatures' -or
        $playerInput -notmatch 'ShopSanctuary::Contains' -or $attacks -notmatch 'ShopSanctuary::Contains' -or
        $powers -notmatch 'ShopSanctuary::Contains' -or
        $source -notmatch '"StartGuardianPower"' -or $source -notmatch '"ActivateGuardianPower"') {
        throw 'Weapons, queued attacks and guardian powers must be blocked inside the shop dome.'
    }
    Write-Output 'PASS combat input, weapon attacks and guardian powers are blocked in the camp'

    $capture = CodeOf (TypeNamed 'WorldCamp') 'CaptureGeneration'
    $terrain = CodeOf (TypeNamed 'WorldCamp') 'InstallTerrainAndArea'
    $context = CodeOf (TypeNamed 'NamekeeperCampGenerationContext') 'Prefix'
    $source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\WorldCamp.cs') -Raw
    if ($capture -notmatch 'NewCampDepth' -or $capture -notmatch 'ZDO::Set' -or
        $context -notmatch 'ShopSanctuary::IsLocationName' -or
        $terrain -notmatch 'ZDO::GetInt' -or $source -notmatch 'mode == ZoneSystem.SpawnMode.Client\) WorldCamp.NewCampDepth = 0' -or
        $source -notmatch 'ClutterSystem.instance\?\.ResetGrass') {
        throw 'Terrain leveling must require a new-generation ZDO key; old camps must only clear grass.'
    }
    Write-Output 'PASS terrain changes are gated by new generation and grass refresh follows live shop loading'

    $clutter = CodeOf (TypeNamed 'NamekeeperCampClutterGuard') 'Prefix'
    $containsActive = CodeOf (TypeNamed 'ShopSanctuary') 'ContainsActiveCamp'
    if ($clutter -notmatch 'ShopSanctuary::ContainsActiveCamp' -or
        $containsActive -notmatch 'CampFootprint::Contains') {
        throw 'The clutter exclusion must use active camp X/Z bounds, independently of terrain paint.'
    }
    # Check the actual installed generator consumes GetGroundInfo=false by skipping a
    # candidate. A structural check on our patch alone cannot establish that contract.
    $gamePath = Join-Path $ValheimDir 'valheim_Data\Managed\assembly_valheim.dll'
    $gameAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($gamePath)
    try {
        $nativeClutter = $gameAssembly.MainModule.Types | Where-Object Name -eq 'ClutterSystem'
        $generate = $nativeClutter.Methods | Where-Object Name -eq 'GenerateVegPatch'
        $call = $generate.Body.Instructions | Where-Object { [string]$_.Operand -match 'ClutterSystem::GetGroundInfo' }
        if (-not $call -or $call.Next.OpCode.Code.ToString() -notmatch '^Brfalse') {
            throw 'Installed clutter generation no longer skips candidates when ground info returns false.'
        }
    } finally { $gameAssembly.Dispose() }
    Write-Output 'PASS native clutter generator skips rejected points and camp bounds use only X/Z'

    $initialize = (TypeNamed 'WorldCamp').Methods | Where-Object Name -eq 'Update'
    $ensureBoars = CodeOf (TypeNamed 'WorldCamp') 'EnsureBoars'
    $buildDecor = CodeOf (TypeNamed 'WorldCamp') 'BuildDecor'
    $meal = CodeOf (TypeNamed 'CampMeal') 'Create'
    $copyItem = CodeOf (TypeNamed 'NativeWorkshopAssets') 'AddItemVisual'
    if ($initialize.Body.ExceptionHandlers.Count -lt 2 -or
        $ensureBoars -notmatch 'CampBoar::Create' -or $ensureBoars -notmatch '_boars' -or
        $copyItem -notmatch 'attach' -or $copyItem -match 'SelectMeshes' -or
        $meal -notmatch 'CampMeal::Copy' -or $meal -match 'AddItemVisual|ComputePenetration' -or
        $buildDecor -notmatch 'Camp shared bowl unavailable') {
        throw 'Optional meal failures must be isolated from boar initialization; food must reuse the native Hildir composition.'
    }
    Write-Output 'PASS food and animal failures are isolated; bowl and food use the authored Hildir group'
}
finally { $assembly.Dispose(); $resolver.Dispose() }
