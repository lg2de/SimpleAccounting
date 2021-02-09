param (
  [Parameter(Mandatory=$true)][string]$assetUrl,
  [Parameter(Mandatory=$true)][string]$targetFolder,
  [Parameter(Mandatory=$true)][int]$processId
)

$ErrorActionPreference = "Stop"

try {
  Write-Host Updating SimpleAccounting...

  $tempFile = $env:TEMP + "\package.zip"
  Remove-Item $tempFile -ErrorAction SilentlyContinue

  Write-Host Waiting for current application to finish...
  Wait-Process -Id $processId -ErrorAction SilentlyContinue

  Write-Host Download new release...
  Write-Host $assetUrl
  Invoke-WebRequest -Uri $assetUrl -OutFile $tempFile

  Write-Host Extracting new release...
  Expand-Archive -LiteralPath $tempFile -DestinationPath $targetFolder -Force

  Write-Host Starting...
  & "$targetFolder\SimpleAccounting.exe"
} catch {
  $Shell = New-Object -ComObject "WScript.Shell"
  $Button = $Shell.Popup($_.Exception.Message, 0, "Update failed", 0)
}