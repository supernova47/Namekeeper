param([Parameter(Mandatory)][string]$ValheimDir)
$ErrorActionPreference = 'Stop'
$manifest = Join-Path $ValheimDir 'valheim_Data\StreamingAssets\SoftRef\manifest_extended'
if (-not (Test-Path -LiteralPath $manifest)) {
    Write-Warning 'Extended native asset manifest unavailable; runtime prefab lookup will validate geometry.'
    return
}
$manifestFiles = @($manifest, (Join-Path ([IO.Path]::GetDirectoryName($manifest)) 'manifest')) |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
$paths = $manifestFiles | ForEach-Object { [IO.File]::ReadAllLines($_) } | Where-Object { $_ -match '^  path in bundle: ' } |
    ForEach-Object { $_.Substring(18).Trim() }
$requiredPrefabs = @('wood_floor','wood_pole','wood_beam','woodwall','wood_wall_half','darkwood_roof','darkwood_beam','sign','bed','piece_dvergr_lantern','wood_door','Ruby','Amber','AmberPearl','Coins','TrophyDeer','piece_chest_wood','ShieldWood','SwordBronze','FineWood','RoundLog','Player','Hair6','ArmorDress10','Boar','ForceField')
$requiredPrefabs += @('TraderRune','hildir_tent2','hildir_barrel','wood_stack','dvergrprops_crate','piece_bench01','piece_table','Bread','CookedMeat','Tankard','Hildir_camp','fire_pit_haldor')
foreach ($name in $requiredPrefabs) {
    if (-not ($paths | Where-Object { [IO.Path]::GetFileName($_) -ceq "$name.prefab" })) {
        throw "Installed native prefab not found in asset manifest: $name"
    }
}
foreach ($model in @('fi_vil_container_bucket01.fbx', 'turnip.obj')) {
    if (-not ($paths | Where-Object { [IO.Path]::GetFileName($_) -ceq $model })) {
        throw "Native Hildir meal model is unavailable: $model"
    }
}
Write-Output "PASS all $($requiredPrefabs.Count) native workshop, recipe, pet and force-field prefabs exist in installed extended asset manifest"
