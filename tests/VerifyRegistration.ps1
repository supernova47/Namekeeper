param(
    [string]$DllPath = (Join-Path $PSScriptRoot '..\src\ValheimNamekeeper\bin\Release\netstandard2.1\SuperNova.ValheimNamekeeper.dll'),
    [string]$CecilDll = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Default\BepInEx\core\Mono.Cecil.dll')
)
$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath $CecilDll
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($DllPath))
try {
    $type = $assembly.MainModule.Types | Where-Object FullName -eq 'SuperNova.ValheimNamekeeper.ShopRegistry'
    $calls = 0
    foreach ($method in $type.Methods) {
        if (-not $method.HasBody) { continue }
        $instructions = @($method.Body.Instructions)
        for ($i = 0; $i -lt $instructions.Count; $i++) {
            if ([string]$instructions[$i].Operand -notmatch 'PieceManager::AddPiece') { continue }
            $calls++
            $earlierIcon = @($instructions | Select-Object -First $i | Where-Object { [string]$_.Operand -match 'ShopIcons::Assign' })
            if ($earlierIcon.Count -eq 0) { throw 'Icon must be assigned BEFORE AddPiece validation.' }
            $configIcon = @($instructions | Select-Object -First $i | Where-Object { [string]$_.Operand -match 'PieceConfig::set_Icon' })
            if ($configIcon.Count -eq 0) { throw 'The final icon must also be retained in PieceConfig before registration.' }
            $next = $instructions[$i].Next
            while ($next.OpCode.Code.ToString() -eq 'Nop') { $next = $next.Next }
            if ($next.OpCode.Code.ToString() -notmatch '^Br(true|false)') { throw 'AddPiece return value must control success/failure.' }
        }
    }
    if ($calls -ne 1) { throw 'All pieces must use the single checked registration path.' }
    Write-Output 'PASS compiled registration assigns icon before AddPiece'
    Write-Output 'PASS compiled registration checks AddPiece rejection'
    Write-Output 'PASS compiled registration retains final icon in PieceConfig'
    $register = $type.Methods | Where-Object Name -eq 'Register'
    $strings = @($register.Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -eq 'Ldstr' } | ForEach-Object Operand)
    $shop = [Array]::IndexOf($strings, 'SN_NamekeeperShop')
    $roof = [Array]::IndexOf($strings, 'SN_NamekeeperRoof')
    if ($shop -lt 0 -or $roof -lt 0 -or $shop -ge $roof) { throw 'Persistent legacy shop must be prepared before the optional roof.' }
    Write-Output 'PASS compiled registration prepares legacy shop before roof'
    Write-Output '4 compiled IL checks passed; Unity runtime not exercised.'
}
finally { $assembly.Dispose() }
