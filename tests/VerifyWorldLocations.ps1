param(
    [string]$DllPath = (Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'),
    [string]$CecilDll = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\core\Mono.Cecil.dll'),
    [string]$JotunnDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\plugins\ValheimModding-Jotunn')
)
$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath $CecilDll
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory([IO.Path]::GetFullPath($JotunnDir))
$reader = New-Object Mono.Cecil.ReaderParameters
$reader.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($DllPath),$reader)
function TypeNamed($name) { $assembly.MainModule.Types | Where-Object FullName -eq ("SuperNova.ValheimNamekeeper." + $name) }
function CodeOf($type, $methodName) {
    $method = $type.Methods | Where-Object Name -eq $methodName
    if (-not $method -or -not $method.HasBody) { throw "Missing compiled method $($type.Name).$methodName" }
    return [string]::Join(' ', $method.Body.Instructions)
}
try {
    $locations = TypeNamed 'ShopWorldLocations'
    $allCode = [string]::Join(' ', @($locations.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions }))
    foreach ($name in @('SN_Namekeeper_Meadows_01','SN_Namekeeper_Meadows_02','SN_Namekeeper_Meadows_03')) {
        if ($allCode -notmatch [regex]::Escape($name)) { throw "Missing generated location: $name" }
    }
    if ($allCode -notmatch 'Vendor_BlackForest' -or $allCode -notmatch 'CreateClonedLocation') {
        throw 'World locations must clone the native Vendor_BlackForest camp.'
    }
    Write-Output 'PASS three distinct Meadow locations clone Haldor camp'

    $prepare = CodeOf $locations 'PrepareLocation'
    foreach ($required in @('ZoneLocation::m_biome','m_minDistanceFromSimilar','m_noBuild','m_clearArea','WorldCamp','ZNetView::m_persistent','ShopState::set_IsWorldLocation')) {
        if ($prepare -notmatch [regex]::Escape($required)) { throw "World location setup missing $required" }
    }
    $sourcePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\ShopWorldLocations.cs'))
    if ((Get-Content -LiteralPath $sourcePath -Raw) -notmatch 'm_biome\s*=\s*Heightmap\.Biome\.Meadows') {
        throw 'World locations must target the Meadows biome.'
    }
    if ($allCode -notmatch 'ForceField') { throw 'No native ForceField visual is retained or available as fallback.' }
    Write-Output 'PASS location biome, spacing, safety dome, pet and shop persistence are configured'

    $sourcePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\ShopWorldLocations.cs'))
    $source = Get-Content -LiteralPath $sourcePath -Raw
    if ($source -notmatch 'm_maximumVegetation\s*=\s*1f' -or
        $source -notmatch 'm_iconAlways\s*=\s*false' -or
        $source -notmatch 'm_iconPlaced\s*=\s*true' -or
        $source -notmatch 'm_discoverLabel\s*=\s*""' -or
        (CodeOf (TypeNamed 'ShopDiscovery') 'Postfix') -notmatch 'MessageHud::ShowBiomeFoundMsg') {
        throw 'Locations must accept normal vegetation and keep Haldor-style map discovery and notification.'
    }
    Write-Output 'PASS normal Meadow vegetation is allowed and vanilla location discovery is configured'

    $prepareSanctuary = CodeOf (TypeNamed 'ShopSanctuary') 'InstallLocationFeatures'
    $prepareBoar = CodeOf (TypeNamed 'CampBoar') 'Create'
    $locationSource = Get-Content -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\ShopSanctuary.cs'))) -Raw
    $worldSource = Get-Content -LiteralPath ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\ShopWorldLocations.cs'))) -Raw
    if ($prepareSanctuary -notmatch 'TerrainModifier' -or $prepareSanctuary -notmatch 'EffectArea' -or
        $locationSource -notmatch 'm_paintCleared\s*=\s*true' -or
        $locationSource -notmatch 'PaintType\.ClearVegetation' -or
        $locationSource -notmatch 'EffectArea\.Type\.NoMonsters') {
        throw 'The generated camp must clear grass with TerrainModifier and block monster spawns with a native EffectArea.'
    }
    $boarType = TypeNamed 'CampBoar'
    if ($prepareBoar -notmatch 'Animator::set_applyRootMotion' -or $prepareBoar -notmatch 'MonoBehaviour' -or
        $boarType.Interfaces.InterfaceType.FullName -notcontains 'Interactable' -or
        $boarType.BaseType.FullName -ne 'UnityEngine.MonoBehaviour') {
        throw 'Camp boars must be stationary animated interactables, not player-owned Characters/Tameables.'
    }
    Write-Output 'PASS camp clears grass, blocks monster spawning and uses stationary animated pet displays'

    $map = CodeOf $locations 'TickMapPins'
    $findServerLocations = CodeOf $locations 'FindServerMapPositions'
    $serverReveal = CodeOf $locations 'ServerReceiveMapReveal'
    $adminCheck = TypeNamed 'ShopWorldLocations'
    $adminCheckMethod = $adminCheck.Methods | Where-Object Name -eq 'IsPeerAdmin'
    $adminCheckCode = if ($adminCheckMethod) { [string]::Join(' ', $adminCheckMethod.Body.Instructions) } else { '' }
    $addMapPin = CodeOf $locations 'AddMapPin'
    if ($addMapPin -notmatch 'Minimap::AddPin' -or $map -notmatch 'HasServerMapPosition' -or
        $findServerLocations -notmatch 'ZoneSystem::FindLocations' -or
        $serverReveal -notmatch 'ShopWorldLocations::IsPeerAdmin' -or
        $adminCheckCode -notmatch 'ISocket::GetHostName' -or
        $adminCheckCode -notmatch 'ZNet::IsAdmin' -or $adminCheckCode -match 'm_playerID' -or
        $serverReveal -notmatch 'ZRpc::Invoke') {
        throw 'Admin map reveal must use the authenticated socket, authoritative generated positions, and a direct connection reply.'
    }
    $command = TypeNamed 'RevealNamekeeperShopsCommand'
    $run = CodeOf $command 'RunCommand'
    if ($run -notmatch 'SetMapMarkersVisible' -or $run -notmatch 'MapRequestPending' -or
        $run -notmatch 'MapRevealRpcReady' -or $run -notmatch 'on' -or $run -notmatch 'off') {
        throw 'Map reveal must be an admin-only on/off console command.'
    }
    Write-Output 'PASS admin-only command requests server locations and reports local map-pin status'

    $icon = TypeNamed 'NamekeeperLocationMapIconPatch'
    $iconPrefix = CodeOf $icon 'Prefix'
    $sanctuary = TypeNamed 'ShopSanctuary'
    $contains = CodeOf $sanctuary 'Contains'
    if ($iconPrefix -notmatch 'Vendor_BlackForest' -or
        $iconPrefix -notmatch 'ShopSanctuary::IsLocationName' -or
        $contains -notmatch 'Location::GetLocation') {
        throw 'Generated locations must use the native Haldor map icon and shop-location bounds.'
    }
    Write-Output 'PASS generated-location discovery can resolve Haldor map icons for each client'

    $registry = TypeNamed 'ShopRegistry'
    $mode = CodeOf $registry 'ApplyOperatingMode'
    $availability = CodeOf $registry 'SetHammerAvailability'
    if ($availability -notmatch 'Piece::m_enabled' -or $mode -notmatch 'IsBuildableMode') {
        throw 'Both legacy pieces must be available only in hammer-buildable mode.'
    }
    $state = TypeNamed 'ShopState'
    if ((CodeOf $state 'Update') -notmatch 'get_IsWorldLocation') {
        throw 'World-spawned shops must retain immediate arrival behavior.'
    }
    Write-Output 'PASS Hammer visibility switches by mode and world merchants arrive immediately'
    Write-Output '7 compiled world-location checks passed; actual world generation and visuals require in-game tests.'
}
finally { $assembly.Dispose(); $resolver.Dispose() }
