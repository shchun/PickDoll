param([switch]$Flow,[switch]$Targeting,[switch]$UiOnly,[int]$Width=540,[int]$Height=960)
$ErrorActionPreference='Stop'
$weekPath=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Week11\Windows'))
$game=Join-Path $weekPath 'PickDoll.exe'
if(-not(Test-Path -LiteralPath $game)){throw 'Build Week 11 Windows first.'}
New-Item -ItemType Directory -Path (Join-Path $weekPath 'Checks') -Force | Out-Null
$flag=if($Flow){'--pickdoll-week11-test --pickdoll-week11-flow'}elseif($Targeting){'--pickdoll-week11-test --pickdoll-week11-targeting'}elseif($UiOnly){'--pickdoll-week11-ui'}else{'--pickdoll-week11-test'}
$player=Start-Process -FilePath $game -WorkingDirectory $weekPath -ArgumentList @($flag,'-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-logFile',("`""+(Join-Path $weekPath "Checks\player-$Width-$Height.log")+"`"")) -PassThru
$deadline=[DateTime]::UtcNow.AddMinutes(10)
while(-not $player.WaitForExit(1000)){if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $player.Id;throw 'Week 11 runtime check timed out.'}}
$result=if($Flow){'flow.txt'}elseif($Targeting){'targeting.txt'}elseif($UiOnly){"ui-layout-$Width"+'x'+"$Height.txt"}else{'delivery-runtime.txt'}
$report=Join-Path $weekPath ('Checks\'+$result)
Get-Content -LiteralPath $report
if($player.ExitCode -ne 0 -or (Get-Content -LiteralPath $report -TotalCount 1) -ne 'PASS'){throw "Week 11 check failed: $report"}
