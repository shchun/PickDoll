param([switch]$UiOnly,[int]$Width=540,[int]$Height=960)
$ErrorActionPreference='Stop'
$weekPath=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Week10\Windows'))
$game=Join-Path $weekPath 'PickDoll.exe'
if(-not(Test-Path -LiteralPath $game)){throw 'Build Week 10 Windows first.'}
New-Item -ItemType Directory -Path (Join-Path $weekPath 'Checks') -Force | Out-Null
$flag=if($UiOnly){'--pickdoll-week10-ui'}else{'--pickdoll-week10-test'}
$player=Start-Process -FilePath $game -WorkingDirectory $weekPath -ArgumentList @($flag,'-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-logFile',("`""+(Join-Path $weekPath "Checks\player-$Width-$Height.log")+"`"")) -PassThru
$deadline=[DateTime]::UtcNow.AddMinutes(4)
while(-not $player.WaitForExit(1000)){if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $player.Id;throw 'Week 10 runtime check timed out.'}}
$result=if($UiOnly){"ui-layout-$Width"+'x'+"$Height.txt"}else{'delivery-runtime.txt'}
$report=Join-Path $weekPath ('Checks\'+$result)
Get-Content -LiteralPath $report
if($player.ExitCode -ne 0 -or (Get-Content -LiteralPath $report -TotalCount 1) -ne 'PASS'){throw "Week 10 check failed: $report"}
