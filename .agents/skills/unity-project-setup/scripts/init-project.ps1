<#
.SYNOPSIS
    Initializes a Unity 6 workspace with recommended folder structure, packages, .gitignore, and asmdef.
#>
[CmdletBinding()]
param(
    [string]$TargetDir = (Get-Location).Path,
    [string]$UnityVersion = "6000.6.5f1"
)

Write-Host "Scaffolding Unity 6 Friendslop project at $TargetDir..." -ForegroundColor Cyan

# 1. Create standard directories
$folders = @(
    "Assets/_Game/Scripts/Core",
    "Assets/_Game/Scripts/Player",
    "Assets/_Game/Scripts/Interactions",
    "Assets/_Game/Scripts/Networking",
    "Assets/_Game/Scripts/Audio",
    "Assets/_Game/Prefabs",
    "Assets/_Game/Scenes",
    "Assets/_Game/Materials",
    "Assets/_Game/Audio",
    "Assets/_Game/Settings",
    "Packages",
    "ProjectSettings"
)

foreach ($folder in $folders) {
    $fullPath = Join-Path $TargetDir $folder
    if (-not (Test-Path $fullPath)) {
        New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
    }
}

# 2. Write ProjectSettings/ProjectVersion.txt
$versionFile = Join-Path $TargetDir "ProjectSettings/ProjectVersion.txt"
if (-not (Test-Path $versionFile)) {
    @"
m_EditorVersion: $UnityVersion
m_EditorVersionWithRevision: $UnityVersion
"@ | Set-Content -Path $versionFile -Encoding UTF8
    Write-Host "Created $versionFile" -ForegroundColor Green
}

# 3. Write Packages/manifest.json with Netcode, URP, and Input System
$manifestFile = Join-Path $TargetDir "Packages/manifest.json"
if (-not (Test-Path $manifestFile)) {
    @"
{
  "dependencies": {
    "com.unity.cinemachine": "3.1.2",
    "com.unity.inputsystem": "1.13.0",
    "com.unity.multiplayer.tools": "2.2.1",
    "com.unity.netcode.gameobjects": "2.2.0",
    "com.unity.render-pipelines.universal": "17.0.3",
    "com.unity.textmeshpro": "3.2.0-pre.7",
    "com.unity.transport": "2.4.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.ui": "1.0.0"
  }
}
"@ | Set-Content -Path $manifestFile -Encoding UTF8
    Write-Host "Created $manifestFile" -ForegroundColor Green
}

# 4. Write standard Unity .gitignore
$gitignoreFile = Join-Path $TargetDir ".gitignore"
if (-not (Test-Path $gitignoreFile)) {
    @"
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/

# Visual Studio / VS Code / Rider cache
.vs/
.idea/
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd

# OS generated
.DS_Store
Thumbs.db
desktop.ini
"@ | Set-Content -Path $gitignoreFile -Encoding UTF8
    Write-Host "Created $gitignoreFile" -ForegroundColor Green
}

# 5. Write Assembly Definition for fast compilation
$asmdefFile = Join-Path $TargetDir "Assets/_Game/Scripts/Game.asmdef"
if (-not (Test-Path $asmdefFile)) {
    @"
{
    "name": "Game",
    "rootNamespace": "Game",
    "references": [
        "Unity.Netcode.Runtime",
        "Unity.InputSystem",
        "Unity.TextMeshPro",
        "Unity.RenderPipelines.Universal.Runtime",
        "Unity.Cinemachine"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
"@ | Set-Content -Path $asmdefFile -Encoding UTF8
    Write-Host "Created $asmdefFile" -ForegroundColor Green
}

Write-Host "Unity 6 Friendslop project scaffolding completed!" -ForegroundColor Green
