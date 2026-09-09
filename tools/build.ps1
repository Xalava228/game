param([switch]$PrepareOnly)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$env:TEMP='D:\Game\Work\Temp'
$env:TMP=$env:TEMP
$env:UPM_CACHE_ROOT='D:\Game\Work\upm-cache'
$env:UPM_NPM_CACHE_PATH='D:\Game\Work\upm-cache\npm'
$env:UPM_CACHE_PATH='D:\Game\Work\upm-cache\packages'
$env:UNITY_GI_CACHE_PATH='D:\Game\Work\gi-cache'
$env:EM_CACHE='D:\Game\Work\emscripten-cache'
$env:DOTNET_CLI_HOME='D:\Game\Work\dotnet'
$env:NUGET_PACKAGES='D:\Game\Work\nuget'
$method=if($PrepareOnly){'TimeThief.Editor.ProjectBuilder.Prepare'}else{'TimeThief.Editor.ProjectBuilder.Build'}
$buildProcess=Start-Process -FilePath 'D:\UnityHub\6000.3.23f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-nographics','-quit','-projectPath',('"'+$taskRoot+'\game"'),'-buildTarget','WebGL','-executeMethod',$method,'-logFile','D:\Game\Work\Logs\unity-build.log','-upmLogFile','D:\Game\Work\Logs\upm.log') -WindowStyle Hidden -PassThru -Wait
exit $buildProcess.ExitCode
