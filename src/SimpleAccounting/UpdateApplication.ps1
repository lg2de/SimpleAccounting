param (
  [Parameter(Mandatory=$true)][string]$assetUrl,
  [Parameter(Mandatory=$true)][string]$targetFolder,
  [Parameter(Mandatory=$true)][int]$processId
)

$tempFile = $env:TEMP + "\package.zip"

Write-Host Waiting for current application to finish...
Wait-Process -Id $processId -ErrorAction SilentlyContinue

try {
  Write-Host Download new release...
  Remove-Item $tempFile
  Invoke-WebRequest -Uri $assetUrl -OutFile $tempFile

  Write-Host Extracting new release...
  Expand-Archive -LiteralPath $tempFile -DestinationPath $targetFolder -Force

  Write-Host Starting...
  & "$targetFolder\SimpleAccounting.exe"
} catch {
  $Shell = New-Object -ComObject "WScript.Shell"
  $Button = $Shell.Popup($_.Exception.Message, 0, "Update failed", 0)
}