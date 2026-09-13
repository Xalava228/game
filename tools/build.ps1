param([switch]$PrepareOnly, [string]$ProjectPath='', [string]$LogFile='D:\Game\Work\Logs\unity-build.log')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
if (-not $ProjectPath) { $ProjectPath=Join-Path $taskRoot 'game' }
$ProjectPath=[System.IO.Path]::GetFullPath($ProjectPath)
$env:TEMP='D:\Game\Work\Temp'
$env:TMP=$env:TEMP
$env:UPM_CACHE_ROOT='D:\Game\Work\upm-cache'
$env:UPM_NPM_CACHE_PATH='D:\Game\Work\upm-cache\npm'
$env:UPM_CACHE_PATH='D:\Game\Work\upm-cache\packages'
$env:UNITY_GI_CACHE_PATH='D:\Game\Work\gi-cache'
$env:EM_CACHE='D:\UnityHub\6000.3.23f1\Editor\Data\PlaybackEngines\WebGLSupport\BuildTools\Emscripten\emscripten\cache'
$env:DOTNET_CLI_HOME='D:\Game\Work\dotnet'
$env:NUGET_PACKAGES='D:\Game\Work\nuget'
$method=if($PrepareOnly){'TimeThief.Editor.ProjectBuilder.Prepare'}else{'TimeThief.Editor.ProjectBuilder.Build'}
$buildProcess=Start-Process -FilePath 'D:\UnityHub\6000.3.23f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-nographics','-quit','-projectPath',('"'+$ProjectPath+'"'),'-buildTarget','WebGL','-executeMethod',$method,'-logFile',('"'+$LogFile+'"'),'-upmLogFile','D:\Game\Work\Logs\upm.log') -WindowStyle Hidden -PassThru
$buildProcess.WaitForExit()
exit $buildProcess.ExitCode
