<#
.SYNOPSIS
    Reads the tail of the local Unity Editor.log to diagnose editor/runtime errors.
#>
[CmdletBinding()]
param(
    [int]$Lines = 100
)

$logPath = [System.IO.Path]::Combine($env:LOCALAPPDATA, "Unity", "Editor", "Editor.log")

if (-not (Test-Path $logPath)) {
    Write-Warning "Editor.log not found at $logPath"
    exit 0
}

Write-Host "Reading last $Lines lines from $logPath..." -ForegroundColor Cyan
Get-Content $logPath -Tail $Lines
