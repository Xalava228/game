param([string]$UnityRoot='D:\UnityHub\6000.3.23f1\Editor')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$output='D:\Game\Work\Checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$env:TEMP='D:\Game\Work\Temp'
$env:TMP=$env:TEMP
$env:DOTNET_CLI_HOME='D:\Game\Work\dotnet'
$runtime=Get-ChildItem -LiteralPath (Join-Path $UnityRoot 'Data\NetCoreRuntime\shared\Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$dotnet=Join-Path $UnityRoot 'Data\NetCoreRuntime\dotnet.exe'
$compiler=Join-Path $UnityRoot 'Data\DotNetSdkRoslyn\csc.dll'
$references=@(Get-ChildItem -LiteralPath $runtime.FullName -Filter '*.dll' | ForEach-Object {
    try {
        [System.Reflection.AssemblyName]::GetAssemblyName($_.FullName) | Out-Null
        '-r:"'+$_.FullName+'"'
    } catch [System.BadImageFormatException] { } # Native runtime support libraries are not C# references.
})
$sources=@('GameConfig','PlayerStats','EnemyData','BossData','EnemyGenerator','ShopManager','RewardManager','EnemyController') | ForEach-Object { '"'+(Join-Path $repo ('game\Assets\TimeThief\Scripts\'+$_+'.cs'))+'"' }
foreach($entry in @('ModelChecks','BalanceChecks')) {
    $assembly=Join-Path $output ($entry+'.dll')
    $response=Join-Path $output ($entry+'.rsp')
    $arguments=@('-nologo','-target:exe','-nostdlib+',('-main:'+$entry),('-out:"'+$assembly+'"'))+$references+$sources+@('"'+(Join-Path $PSScriptRoot 'ModelChecks.cs')+'"')
    if($entry -eq 'BalanceChecks') { $arguments+=('"'+(Join-Path $PSScriptRoot 'BalanceChecks.cs')+'"') }
    $arguments | Set-Content -LiteralPath $response -Encoding utf8
    & $dotnet $compiler ('@'+$response)
    if($LASTEXITCODE -ne 0) { throw "Compilation failed: $entry" }
    @{runtimeOptions=@{tfm='net6.0';framework=@{name='Microsoft.NETCore.App';version=$runtime.Name}}} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output ($entry+'.runtimeconfig.json'))
    $report=if($entry -eq 'BalanceChecks'){'balance-checks.csv'}else{'portable-checks.txt'}
    & $dotnet $assembly | Tee-Object -FilePath (Join-Path $repo ('QA\'+$report))
    if($LASTEXITCODE -ne 0) { throw "Regression failed: $entry" }
}
