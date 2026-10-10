<#
.SYNOPSIS
    Runs Unity in batchmode to compile scripts and report errors/warnings.
.DESCRIPTION
    Launches Unity headless with -batchmode -quit, pipes the output, and parses
    compilation errors (CSxxxx) and critical exceptions.
#>
[CmdletBinding()]
param(
    [string]$ProjectPath = (Get-Location).Path,
    [string]$UnityExe = ""
)

# 1. Locate Unity executable if not provided
if (-not $UnityExe -or -not (Test-Path $UnityExe)) {
    $candidates = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe" -ErrorAction SilentlyContinue |
        Sort-Object { $_.FullName -like "*6000*" } -Descending
    if ($candidates) {
        $UnityExe = $candidates[0].FullName
    } else {
        Write-Error "Unity.exe could not be found in C:\Program Files\Unity\Hub\Editor\"
        exit 1
    }
}

Write-Host "Using Unity Editor: $UnityExe" -ForegroundColor Cyan
Write-Host "Project Path: $ProjectPath" -ForegroundColor Cyan

$tempLog = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "unity_compile_check.log")
if (Test-Path $tempLog) { Remove-Item $tempLog -Force }

Write-Host "Compiling scripts in batchmode..." -ForegroundColor Yellow
$process = Start-Process -FilePath $UnityExe `
    -ArgumentList "-batchmode", "-quit", "-projectPath", "`"$ProjectPath`"", "-logFile", "`"$tempLog`"" `
    -Wait -PassThru -NoNewWindow

$exitCode = $process.ExitCode

if (Test-Path $tempLog) {
    $logContent = Get-Content $tempLog
    $errors = $logContent | Where-Object { $_ -match "error CS\d+" -or $_ -match "Compilation failed" -or $_ -match "Scripts have compiler errors" }
    $warnings = $logContent | Where-Object { $_ -match "warning CS\d+" }

    if ($errors) {
        Write-Host "`n--- COMPILATION ERRORS DETECTED ---" -ForegroundColor Red
        foreach ($err in $errors) {
            Write-Host $err -ForegroundColor Red
        }
        exit 1
    } else {
        Write-Host "`nCompilation Successful! No compile errors detected." -ForegroundColor Green
        if ($warnings) {
            Write-Host "Warnings ($($warnings.Count)):" -ForegroundColor DarkYellow
            $warnings | Select-Object -First 10 | ForEach-Object { Write-Host $_ -ForegroundColor DarkYellow }
        }
        exit 0
    }
} else {
    Write-Host "No log file produced. Unity exited with code $exitCode." -ForegroundColor Yellow
    exit $exitCode
}
