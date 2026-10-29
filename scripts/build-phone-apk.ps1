param(
    [string]$UnityEditor = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe",
    [ValidateRange(1, 64)][int]$JobWorkers = 2
)
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
if (!(Test-Path $UnityEditor)) { throw "Unity Editor not found at $UnityEditor. Pass -UnityEditor with your installed Unity.exe path." }
$BuildDirectory = Join-Path $ProjectRoot "Builds"
New-Item -ItemType Directory -Path $BuildDirectory -Force | Out-Null
$LogFile = Join-Path $BuildDirectory "phone-build.log"
# Close this project in the interactive Editor first. Hub must have an active licence.
$Arguments = @("-batchmode", "-nographics", "-job-worker-count", "$JobWorkers", "-quit", "-projectPath", "`"$ProjectRoot`"", "-buildTarget", "Android", "-executeMethod", "ZeroGCargo.Editor.ProjectSetup.BuildPhoneTest", "-logFile", "`"$LogFile`"")
$BuildStarted = (Get-Date).ToUniversalTime()
$Process = Start-Process -FilePath $UnityEditor -ArgumentList $Arguments -Wait -PassThru
if ($Process.ExitCode -ne 0) { throw "Unity build failed ($($Process.ExitCode)). See $LogFile" }
$Apk = Join-Path $BuildDirectory "ZeroGCargo-phone-test.apk"
if (!(Test-Path $Apk)) { throw "Unity exited without producing an APK. See $LogFile" }
if ((Get-Item $Apk).LastWriteTimeUtc -lt $BuildStarted) { throw "The APK is from an earlier run; no fresh build was produced. See $LogFile" }
Get-FileHash -Algorithm SHA256 $Apk
Write-Host "Installable test APK: $Apk"
