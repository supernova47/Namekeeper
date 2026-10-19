param(
    [string]$DllPath = (Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'),
    [string]$CecilDll = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\core\Mono.Cecil.dll')
)
$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath $CecilDll
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($DllPath))
function TypeNamed($name) { $assembly.MainModule.Types | Where-Object FullName -eq ("SuperNova.ValheimNamekeeper." + $name) }
function HasConstant($instruction, $value) { $instruction.OpCode.Code.ToString() -eq ("Ldc_I4_" + $value) }
try {
    foreach ($language in @('ru','en')) {
        $resource = $assembly.MainModule.Resources | Where-Object Name -eq "Namekeeper.Assets.sign_$language.png"
        if (-not $resource -or $resource.GetResourceData().Length -lt 100) { throw "Missing embedded handwritten sign: $language" }
    }
    Write-Output 'PASS compiled DLL embeds both handwritten sign textures'
    $ink = (TypeNamed 'WorkshopSign').Methods | Where-Object Name -eq 'InkMaterial'
    foreach ($setting in @(@('_ZWrite',1),@('_Cull',2))) {
        $instruction = $ink.Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldstr' -and $_.Operand -eq $setting[0] }
        if (-not $instruction -or -not (HasConstant $instruction.Next $setting[1])) { throw "Incorrect ink depth/culling setting: $($setting[0])" }
    }
    $inkCode = [string]::Join(' ', $ink.Body.Instructions)
    if ($inkCode -match 'Shader::Find|GUI/Text Shader|GUIManager|TextMesh' -or $inkCode -notmatch 'NativeWorkshopAssets::SignMaterial') {
        throw 'World sign must clone the real native sign material without shader-name lookup'
    }
    Write-Output 'PASS compiled sign clones native world material with depth write and back-face culling'
    $lighting = (TypeNamed 'WorkshopLighting').Methods | Where-Object Name -eq 'Configure'
    foreach ($setting in @(@('set_type',2),@('set_renderMode',1))) {
        $instruction = $lighting.Body.Instructions | Where-Object { [string]$_.Operand -match ('Light::' + $setting[0] + '\(') }
        if (-not $instruction -or -not (HasConstant $instruction.Previous $setting[1])) { throw "Incorrect point light setting: $($setting[0])" }
    }
    Write-Output 'PASS compiled lights use omnidirectional point type and pixel lighting'
    $prepare = (TypeNamed 'ShopRegistry').Methods | Where-Object Name -eq 'PrepareRoot'
    $clip = $prepare.Body.Instructions | Where-Object { [string]$_.Operand -match 'Piece::m_clipGround$' }
    $overlap = $prepare.Body.Instructions | Where-Object { [string]$_.Operand -match 'Piece::m_noClipping$' }
    if (-not (HasConstant $clip.Previous 0) -or $overlap.Previous.OpCode.Code.ToString() -ne 'Ceq' -or
        -not (HasConstant $overlap.Previous.Previous 0) -or $overlap.Previous.Previous.Previous.OpCode.Code.ToString() -ne 'Ldarg_3') {
        throw 'Body placement must disable ground clipping and enable native intersection checks'
    }
    Write-Output 'PASS compiled body placement disables ground clipping and checks intersections'
    $native = TypeNamed 'NativeWorkshopAssets'
    $nativeCode = [string]::Join(' ', @($native.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions }))
    if ($nativeCode -match '::Instantiate|AddComponent<ZNetView>|AddComponent<WearNTear>|AddComponent<Piece>|AddComponent<Container>|AddComponent<ItemDrop>') {
        throw 'Native geometry copier must not instantiate game behaviours or network objects'
    }
    foreach ($required in @('GetLODs','get_sharedMesh','get_sharedMaterials','CopyCollider')) {
        if ($nativeCode -notmatch $required) { throw "Native geometry copier missing $required" }
    }
    Write-Output 'PASS compiled native geometry copies mesh/material/colliders and selects LOD without game scripts'
    $refresh = (TypeNamed 'WorkshopSign').Methods | Where-Object Name -eq 'Refresh'
    $refreshCode = [string]::Join(' ', $refresh.Body.Instructions)
    if ($refreshCode -notmatch 'GetComponent<UnityEngine.MeshFilter>' -or $refreshCode -notmatch 'GetComponent<UnityEngine.MeshRenderer>') {
        throw 'Cloned sign must reuse serialized components'
    }
    Write-Output 'PASS compiled sign refresh reuses components after prefab cloning'
    Write-Output '6 visual/placement IL checks passed; rendering and physics require game tests.'
}
finally { $assembly.Dispose() }
