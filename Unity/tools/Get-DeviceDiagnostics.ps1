param([Parameter(Mandatory=$true)][string]$Serial)
$ErrorActionPreference='Stop'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$destination=Join-Path $PSScriptRoot "..\Builds\Week13\Device\diagnostics-$stamp"
New-Item -ItemType Directory -Path $destination -Force | Out-Null
& adb -s $Serial pull '/sdcard/Android/data/com.precipi.pickdoll/files/diagnostics' $destination
if($LASTEXITCODE -ne 0){throw 'Could not retrieve diagnostics. Check the device and whether Week 13 has been run.'}
Write-Output ([IO.Path]::GetFullPath($destination))
