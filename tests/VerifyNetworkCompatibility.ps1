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
try {
    $plugin = TypeNamed 'NamekeeperPlugin'
    $compatibility = $plugin.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'Jotunn.Utils.NetworkCompatibilityAttribute' }
    if (-not $compatibility -or $compatibility.ConstructorArguments.Count -ne 2 -or
        [int]$compatibility.ConstructorArguments[0].Value -ne 2 -or
        [int]$compatibility.ConstructorArguments[1].Value -ne 3) {
        throw 'The plugin must require EveryoneMustHaveMod and exact patch-version compatibility.'
    }
    Write-Output 'PASS network attribute requires every peer and matching patch version'

    $awake = $plugin.Methods | Where-Object Name -eq 'Awake'
    $syncBindings = @($awake.Body.Instructions | Where-Object { [string]$_.Operand -match 'NamekeeperPlugin::BindSynced' })
    if ($syncBindings.Count -ne 12) { throw 'All 12 gameplay-affecting service, recipe, shop-mode, shop, and text settings must be server-synchronized.' }
    $bind = $plugin.Methods | Where-Object Name -eq 'BindSynced'
    $bindInstructions = @($bind.Body.Instructions)
    $syncCall = -1
    for ($i = 0; $i -lt $bindInstructions.Count; $i++) {
        if ([string]$bindInstructions[$i].Operand -match 'ConfigFileExtensions::BindConfig') { $syncCall = $i; break }
    }
    $syncFlags = @($bindInstructions | Select-Object -First $syncCall | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldc_I4_1' })
    if ($syncCall -lt 1 -or $syncFlags.Count -eq 0) {
        throw 'Gameplay config binding must set Jötunn synced/admin-only mode.'
    }
    Write-Output 'PASS gameplay configuration uses Jötunn server synchronization'

    $awakeCode = [string]::Join(' ', $awake.Body.Instructions)
    if ($awakeCode -notmatch 'OperatingMode' -or $awakeCode -match 'RevealShopLocations' -or
        $awakeCode -notmatch 'CommandManager::AddConsoleCommand') {
        throw 'The operating mode must stay server-synchronized and map visibility must use the local admin command, not a config entry.'
    }
    $command = (TypeNamed 'RevealNamekeeperShopsCommand').Methods | Where-Object Name -eq 'RunCommand'
    $commandCode = [string]::Join(' ', $command.Body.Instructions)
    $mapServer = (TypeNamed 'ShopWorldLocations').Methods | Where-Object Name -eq 'ServerReceiveMapReveal'
    $mapServerCode = [string]::Join(' ', $mapServer.Body.Instructions)
    $adminCheck = (TypeNamed 'ShopWorldLocations').Methods | Where-Object Name -eq 'IsPeerAdmin'
    $adminCheckCode = if ($adminCheck) { [string]::Join(' ', $adminCheck.Body.Instructions) } else { '' }
    if ($commandCode -notmatch 'SetMapMarkersVisible' -or $mapServerCode -notmatch 'ZNet::GetPeers' -or
        $mapServerCode -notmatch 'ShopWorldLocations::IsPeerAdmin' -or
        $adminCheckCode -notmatch 'ISocket::GetHostName' -or
        $adminCheckCode -notmatch 'ZNet::IsAdmin' -or
        $adminCheckCode -match 'm_playerID' -or $mapServerCode -match 'CustomRPC') {
        throw 'The direct map RPC must authorize the authenticated socket with the native admin check; character IDs and routed identities cannot grant access.'
    }
    Write-Output 'PASS operating mode is synced and map requests use authenticated transport identity'

    if ($awakeCode -notmatch 'GUIManager::IsHeadless' -or $awakeCode -notmatch 'NamekeeperGui::.ctor' -or
        $awakeCode.IndexOf('GUIManager::IsHeadless') -gt $awakeCode.IndexOf('NamekeeperGui::.ctor')) {
        throw 'The native HUD must only be constructed after headless detection.'
    }
    Write-Output 'PASS dedicated-server startup detects headless mode before creating client UI'

    $refresh = (TypeNamed 'ShopRegistry').Methods | Where-Object Name -eq 'RefreshSynchronizedRecipes'
    $refreshCode = [string]::Join(' ', $refresh.Body.Instructions)
    if (@($refresh.Body.Instructions | Where-Object { [string]$_.Operand -match 'PieceConfig::GetRequirements' }).Count -ne 0 -or
        @($refresh.Body.Instructions | Where-Object { [string]$_.Operand -match 'RecipeRequirementAmounts::TryApply' }).Count -ne 1 -or
        @($refresh.Body.Instructions | Where-Object { [string]$_.Operand -match 'Piece::m_resources' }).Count -ne 1) {
        throw 'The synchronized shop recipe must update amounts while preserving its resolved resource references.'
    }
    Write-Output 'PASS synchronized shop recipe updates preserve vanilla resource references'
    Write-Output '5 compiled network/headless checks passed; a live dedicated-server handshake still requires in-game validation.'
}
finally { $assembly.Dispose(); $resolver.Dispose() }
