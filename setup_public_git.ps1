param(
    [string]$RemoteUrl = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path .git)) {
    git init -b main
}

git lfs install --local
git add .gitattributes .gitignore README.md Assets Packages ProjectSettings
git commit -m "Initial public release"

if ($RemoteUrl -ne "") {
    git remote add origin $RemoteUrl
    git push -u origin main
}

