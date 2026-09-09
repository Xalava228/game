$ErrorActionPreference='Stop'
$env:TEMP='D:\Game\Work\Temp'
$env:TMP=$env:TEMP
$env:UPM_CACHE_ROOT='D:\Game\Work\upm-cache'
$env:DOTNET_CLI_HOME='D:\Game\Work\dotnet'
# Explicit process-only proxy prevents older .NET clients parsing a URI-valued
# Windows ProxyServer entry as the hostname "http". No system settings are changed.
$proxySettings=Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -ErrorAction SilentlyContinue
if($proxySettings.ProxyEnable -eq 1 -and $proxySettings.ProxyServer -match '^https?://[^;]+$') {
    $env:HTTP_PROXY=$proxySettings.ProxyServer
    $env:HTTPS_PROXY=$proxySettings.ProxyServer
    $env:NO_PROXY='localhost,127.0.0.1'
}
Start-Process -FilePath 'D:\UnityHub\Unity Hub\Unity Hub.exe' -WindowStyle Hidden
