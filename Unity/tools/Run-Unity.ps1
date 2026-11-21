param(
    [ValidateSet('Generate','Validate','Windows','Android')][string]$Task = 'Generate',
    [ValidateSet(1,2,3,4)][int]$Week = 1,
    [string]$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not (Test-Path -LiteralPath $Editor)) { throw "Unity not found: $Editor" }
if (Test-Path -LiteralPath (Join-Path $projectPath 'Temp\UnityLockfile')) { throw 'Close this Unity project before running a batch task.' }
$methods = @{ Generate='PickDoll.Editor.WeekOneBuilder.Generate'; Validate='PickDoll.Editor.WeekOneChecks.Run'; Windows='PickDoll.Editor.WeekOneBuilder.BuildWindows'; Android='PickDoll.Editor.WeekOneBuilder.BuildAndroid' }
if ($Week -eq 2) { $methods = @{ Generate='PickDoll.Editor.WeekTwoBuilder.Generate'; Validate='PickDoll.Editor.WeekTwoBuilder.Check'; Windows='PickDoll.Editor.WeekTwoBuilder.BuildWindows'; Android='PickDoll.Editor.WeekTwoBuilder.BuildAndroid' } }
if ($Week -eq 3) { $methods = @{ Generate='PickDoll.Editor.WeekThreeBuilder.Generate'; Validate='PickDoll.Editor.WeekThreeBuilder.Check'; Windows='PickDoll.Editor.WeekThreeBuilder.BuildWindows'; Android='PickDoll.Editor.WeekThreeBuilder.BuildAndroid' } }
if ($Week -eq 4) { $methods = @{ Generate='PickDoll.Editor.WeekFourBuilder.Generate'; Validate='PickDoll.Editor.WeekFourBuilder.Check'; Windows='PickDoll.Editor.WeekFourBuilder.BuildWindows'; Android='PickDoll.Editor.WeekFourBuilder.BuildAndroid' } }
$logDirectory = Join-Path $projectPath 'Builds\Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "Week$Week-$Task.log"
$arguments = @('-batchmode','-projectPath',('"'+$projectPath+'"'),'-executeMethod',$methods[$Task],'-logFile',('"'+$logPath+'"'))
if ($Task -ne 'Validate') { $arguments += '-quit' }
if ($Task -eq 'Android') { $arguments += @('-buildTarget','Android') }
$process = Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Unity task: $Task; PID: $($process.Id); log: $logPath"
$process.WaitForExit()
Get-Content -LiteralPath $logPath -Tail 35
if ($process.ExitCode -ne 0) { throw "Unity failed ($($process.ExitCode)). See $logPath" }
$markers = @{ Generate='PICKDOLL_WEEK01_SCENE_READY'; Validate='PICKDOLL_WEEK01_VALIDATION_PASS'; Windows='PICKDOLL_WINDOWS_BUILD_READY'; Android='PICKDOLL_ANDROID_BUILD_READY' }
if ($Week -eq 2) { $markers = @{ Generate='PICKDOLL_WEEK02_SCENE_READY'; Validate='PICKDOLL_WEEK02_VALIDATION_PASS'; Windows='PICKDOLL_WEEK02_WINDOWS_READY'; Android='PICKDOLL_WEEK02_ANDROID_READY' } }
if ($Week -eq 3) { $markers = @{ Generate='PICKDOLL_WEEK03_SCENE_READY'; Validate='PICKDOLL_WEEK03_VALIDATION_PASS'; Windows='PICKDOLL_WEEK03_WINDOWS_READY'; Android='PICKDOLL_WEEK03_ANDROID_READY' } }
if ($Week -eq 4) { $markers = @{ Generate='PICKDOLL_WEEK04_SCENE_READY'; Validate='PICKDOLL_WEEK04_VALIDATION_PASS'; Windows='PICKDOLL_WEEK04_WINDOWS_READY'; Android='PICKDOLL_WEEK04_ANDROID_READY' } }
if (-not (Select-String -LiteralPath $logPath -SimpleMatch $markers[$Task] -Quiet)) { throw "Expected success marker missing. See $logPath" }
