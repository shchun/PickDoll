param(
    [ValidateSet('Generate','Validate','Windows','Android','Preview','All')][string]$Task = 'Generate',
    [ValidateSet(1,2,3,4,5,6,7,8,9,10,11,12,13)][int]$Week = 1,
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
if ($Week -eq 5) { $methods = @{ Generate='PickDoll.Editor.WeekFiveBuilder.Generate'; Validate='PickDoll.Editor.WeekFiveBuilder.Check'; Windows='PickDoll.Editor.WeekFiveBuilder.BuildWindows'; Android='PickDoll.Editor.WeekFiveBuilder.BuildAndroid' } }
if ($Week -eq 6) { $methods = @{ Generate='PickDoll.Editor.WeekSixBuilder.Generate'; Validate='PickDoll.Editor.WeekSixBuilder.Check'; Windows='PickDoll.Editor.WeekSixBuilder.BuildWindows'; Android='PickDoll.Editor.WeekSixBuilder.BuildAndroid' } }
if ($Week -eq 7) { $methods = @{ Generate='PickDoll.Editor.WeekSevenBuilder.Generate'; Validate='PickDoll.Editor.WeekSevenBuilder.Check'; Windows='PickDoll.Editor.WeekSevenBuilder.BuildWindows'; Android='PickDoll.Editor.WeekSevenBuilder.BuildAndroid' } }
if ($Week -eq 8) { $methods = @{ Generate='PickDoll.Editor.WeekEightBuilder.Generate'; Validate='PickDoll.Editor.WeekEightBuilder.Check'; Windows='PickDoll.Editor.WeekEightBuilder.BuildWindows'; Android='PickDoll.Editor.WeekEightBuilder.BuildAndroid' } }
if ($Week -eq 9) { $methods = @{ Generate='PickDoll.Editor.WeekNineBuilder.Generate'; Validate='PickDoll.Editor.WeekNineBuilder.Check'; Windows='PickDoll.Editor.WeekNineBuilder.BuildWindows'; Android='PickDoll.Editor.WeekNineBuilder.BuildAndroid' } }
if ($Week -eq 10) { $methods = @{ Generate='PickDoll.Editor.WeekTenBuilder.Generate'; Validate='PickDoll.Editor.WeekTenBuilder.Check'; Windows='PickDoll.Editor.WeekTenBuilder.BuildWindows'; Android='PickDoll.Editor.WeekTenBuilder.BuildAndroid' } }
if ($Week -eq 11) { $methods = @{ Generate='PickDoll.Editor.WeekElevenBuilder.Generate'; Validate='PickDoll.Editor.WeekElevenBuilder.Check'; Windows='PickDoll.Editor.WeekElevenBuilder.BuildWindows'; Android='PickDoll.Editor.WeekElevenBuilder.BuildAndroid' } }
if ($Week -eq 12) { $methods = @{ Generate='PickDoll.Editor.WeekTwelveBuilder.Generate'; Validate='PickDoll.Editor.WeekTwelveBuilder.Check'; Windows='PickDoll.Editor.WeekTwelveBuilder.BuildWindows'; Android='PickDoll.Editor.WeekTwelveBuilder.BuildAndroid'; Preview='PickDoll.Editor.WeekTwelvePreview.Run'; All='PickDoll.Editor.WeekTwelveBuilder.BuildAll' } }
if ($Week -eq 13) { $methods = @{ Generate='PickDoll.Editor.WeekThirteenBuilder.Generate'; Validate='PickDoll.Editor.WeekThirteenBuilder.Check'; Windows='PickDoll.Editor.WeekThirteenBuilder.BuildWindows'; Android='PickDoll.Editor.WeekThirteenBuilder.BuildAndroid'; All='PickDoll.Editor.WeekThirteenBuilder.BuildAll' } }
$logDirectory = Join-Path $projectPath 'Builds\Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "Week$Week-$Task.log"
$arguments = @('-batchmode','-projectPath',('"'+$projectPath+'"'),'-executeMethod',$methods[$Task],'-logFile',('"'+$logPath+'"'))
if ($Task -ne 'Validate' -and $Task -ne 'Preview') { $arguments += '-quit' }
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
if ($Week -eq 5) { $markers = @{ Generate='PICKDOLL_WEEK05_SCENE_READY'; Validate='PICKDOLL_WEEK05_VALIDATION_PASS'; Windows='PICKDOLL_WEEK05_WINDOWS_READY'; Android='PICKDOLL_WEEK05_ANDROID_READY' } }
if ($Week -eq 6) { $markers = @{ Generate='PICKDOLL_WEEK06_SCENE_READY'; Validate='PICKDOLL_WEEK06_VALIDATION_PASS'; Windows='PICKDOLL_WEEK06_WINDOWS_READY'; Android='PICKDOLL_WEEK06_ANDROID_READY' } }
if ($Week -eq 7) { $markers = @{ Generate='PICKDOLL_WEEK07_SCENE_READY'; Validate='PICKDOLL_WEEK07_VALIDATION_PASS'; Windows='PICKDOLL_WEEK07_WINDOWS_READY'; Android='PICKDOLL_WEEK07_ANDROID_READY' } }
if ($Week -eq 8) { $markers = @{ Generate='PICKDOLL_WEEK08_SCENE_READY'; Validate='PICKDOLL_WEEK08_VALIDATION_PASS'; Windows='PICKDOLL_WEEK08_WINDOWS_READY'; Android='PICKDOLL_WEEK08_ANDROID_READY' } }
if ($Week -eq 9) { $markers = @{ Generate='PICKDOLL_WEEK09_SCENE_READY'; Validate='PICKDOLL_WEEK09_VALIDATION_PASS'; Windows='PICKDOLL_WEEK09_WINDOWS_READY'; Android='PICKDOLL_WEEK09_ANDROID_READY' } }
if ($Week -eq 10) { $markers = @{ Generate='PICKDOLL_WEEK10_SCENE_READY'; Validate='PICKDOLL_WEEK10_VALIDATION_PASS'; Windows='PICKDOLL_WEEK10_WINDOWS_READY'; Android='PICKDOLL_WEEK10_ANDROID_READY' } }
if ($Week -eq 11) { $markers = @{ Generate='PICKDOLL_WEEK11_SCENE_READY'; Validate='PICKDOLL_WEEK11_VALIDATION_PASS'; Windows='PICKDOLL_WEEK11_WINDOWS_READY'; Android='PICKDOLL_WEEK11_ANDROID_READY' } }
if ($Week -eq 12) { $markers = @{ Generate='PICKDOLL_WEEK12_SCENE_READY'; Validate='PICKDOLL_WEEK12_VALIDATION_PASS'; Windows='PICKDOLL_WEEK12_WINDOWS_READY'; Android='PICKDOLL_WEEK12_ANDROID_READY'; Preview='PICKDOLL_WEEK12_PREVIEW_READY'; All='PICKDOLL_WEEK12_ANDROID_READY' } }
if ($Week -eq 13) { $markers = @{ Generate='PICKDOLL_WEEK13_SCENE_READY'; Validate='PICKDOLL_WEEK13_VALIDATION_PASS'; Windows='PICKDOLL_WEEK13_WINDOWS_READY'; Android='PICKDOLL_WEEK13_ANDROID_READY'; All='PICKDOLL_WEEK13_ANDROID_READY' } }
if (-not (Select-String -LiteralPath $logPath -SimpleMatch $markers[$Task] -Quiet)) { throw "Expected success marker missing. See $logPath" }
