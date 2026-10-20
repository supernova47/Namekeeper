param(
    [string]$DllPath = (Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'),
    [string]$CecilDll = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\core\Mono.Cecil.dll'),
    [Parameter(Mandatory)][string]$ValheimDir
)
$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath $CecilDll
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($DllPath))
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ValheimDir 'valheim_Data\Managed\assembly_valheim.dll'))
function Code($assembly, $type, $method) {
    $target = ($assembly.MainModule.Types | Where-Object Name -eq $type).Methods | Where-Object Name -eq $method
    if (-not $target.HasBody) { throw "Missing method $type.$method" }
    [string]::Join(' ', $target.Body.Instructions)
}
try {
    $chair = $game.MainModule.Types | Where-Object Name -eq 'Chair'
    $interact = Code $game 'Chair' 'Interact'
    $occupancy = Code $game 'Chair' 'IsInUse'
    if ($interact -notmatch 'Character::AttachStart' -or $occupancy -notmatch 'Player::GetClosestPlayer' -or
        $chair.Methods.Name -contains 'Awake' -or
        (Code $mod 'CampSeating' 'Install') -notmatch 'Chair::m_attachPoint') {
        throw 'Installed native Chair no longer supports the lightweight copied seats.'
    }
    Write-Output 'PASS native bench attachment and occupancy contract supports copied Chair seats without furniture ZDOs'

    $pins = ($game.MainModule.Types | Where-Object Name -eq 'Minimap').Fields | Where-Object Name -eq 'm_locationPins'
    $notice = Code $mod 'ShopDiscovery' 'Postfix'
    if ($pins.FieldType.FullName -ne 'System.Collections.Generic.Dictionary`2<UnityEngine.Vector3,Minimap/PinData>' -or
        $notice -notmatch 'Dictionary`2<UnityEngine.Vector3,Minimap/PinData>::TryGetValue' -or
        $notice -notmatch 'CampDiscoveryMemory::Remember' -or $notice -notmatch 'Player::m_customData' -or
        $notice -match 'Minimap::AddPin') {
        throw 'Discovery must confirm the real native pin and save personal history without creating duplicate pins.'
    }
    Write-Output 'PASS notification uses an existing native location marker and persistent character history'

    $mealCopy = Code $mod 'CampMeal' 'Copy'
    $mealCreate = Code $mod 'CampMeal' 'Create'
    $mealCapture = Code $mod 'CampMeal' 'Capture'
    if (($mod.MainModule.Types | Where-Object Name -eq 'CampDisplayPacking') -or
        $mealCapture -notmatch 'fi_vil_container_bucket01' -or $mealCapture -notmatch 'turnips' -or
        $mealCopy -notmatch 'Transform::get_localPosition' -or $mealCopy -notmatch 'Transform::get_localScale' -or
        $mealCopy -notmatch 'Transform::get_localRotation' -or
        $mealCreate -match 'ComputePenetration|Settle|AddItemVisual') {
        throw 'The complete native meal must retain both sibling poses/scales without procedural food packing.'
    }
    Write-Output 'PASS native bowl/turnips preserve authored poses/scales; procedural carrot packing is absent'
    Write-Output '3 camp-presentation contract checks passed; in-game contact/seating validation remains required.'
}
finally { $mod.Dispose(); $game.Dispose() }
