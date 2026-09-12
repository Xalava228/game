$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$build=Join-Path $taskRoot 'Builds\WebGL'
foreach($required in @('index.html','sdk-bridge.js','icon.png','Build')) {
    if(-not (Test-Path -LiteralPath (Join-Path $build $required))) { throw "Incomplete WebGL build: $required is missing" }
}
$release='D:\Game\Release'
New-Item -ItemType Directory -Force -Path $release | Out-Null
$zip=Join-Path $release 'TimeThief-Yandex-1.4.1.zip'
Compress-Archive -Path (Join-Path $build '*') -DestinationPath $zip -Force
$hash=Get-FileHash -LiteralPath $zip -Algorithm SHA256
($hash.Hash+'  TimeThief-Yandex-1.4.1.zip') | Set-Content -LiteralPath (Join-Path $release 'SHA256.txt')
Get-Item -LiteralPath $zip | Select-Object FullName,Length
