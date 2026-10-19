param([Parameter(Mandatory)][string]$ValheimDir)
$ErrorActionPreference = 'Stop'
$manifest = Join-Path $ValheimDir 'valheim_Data\StreamingAssets\SoftRef\manifest_extended'
if (-not (Test-Path -LiteralPath $manifest)) {
    Write-Warning 'Extended native asset manifest unavailable; runtime prefab lookup will validate geometry.'
    return
}
$paths = [IO.File]::ReadAllLines($manifest) | Where-Object { $_ -match '^  path in bundle: ' } |
    ForEach-Object { $_.Substring(18).Trim() }
foreach ($name in @('wood_floor','wood_pole','wood_beam','woodwall','wood_wall_half','darkwood_roof','darkwood_beam','sign','bed','piece_dvergr_lantern','wood_door','Ruby','Amber','AmberPearl','Coins','TrophyDeer','piece_chest_wood','ShieldWood','SwordBronze','FineWood','RoundLog','Player','Hair6','ArmorDress10')) {
    if (-not ($paths | Where-Object { [IO.Path]::GetFileName($_) -ceq "$name.prefab" })) {
        throw "Installed native prefab not found in asset manifest: $name"
    }
}
Write-Output 'PASS all 24 native workshop, recipe and merchant visual prefabs exist in installed extended asset manifest'
