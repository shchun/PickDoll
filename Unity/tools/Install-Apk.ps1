param([Parameter(Mandatory=$true)][string]$Serial, [ValidateSet(1,2,3,4,5)][int]$Week=1)
$ErrorActionPreference = 'Stop'
$apkPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Android\PickDoll-week01.apk'))
if ($Week -eq 2) { $apkPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Week02\Android\PickDoll-week02.apk')) }
if ($Week -eq 3) { $apkPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Week03\Android\PickDoll-week03.apk')) }
if ($Week -eq 4) { $apkPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Week04\Android\PickDoll-week04.apk')) }
if ($Week -eq 5) { $apkPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Builds\Week05\Android\PickDoll-week05.apk')) }
if (-not (Test-Path -LiteralPath $apkPath)) { throw 'Build the Android APK first.' }
$adbCommand = Get-Command adb -ErrorAction Stop
$deviceState = & $adbCommand.Source -s $Serial get-state 2>&1
if ($LASTEXITCODE -ne 0 -or (@($deviceState | ForEach-Object { $_.ToString().Trim() }) -notcontains 'device')) { throw 'The selected device must be connected and USB debugging authorized on the phone.' }
& $adbCommand.Source -s $Serial install -r $apkPath
if ($LASTEXITCODE -ne 0) { throw 'APK install failed.' }
& $adbCommand.Source -s $Serial shell monkey -p com.precipi.pickdoll -c android.intent.category.LAUNCHER 1
if ($LASTEXITCODE -ne 0) { throw 'App launch failed.' }
