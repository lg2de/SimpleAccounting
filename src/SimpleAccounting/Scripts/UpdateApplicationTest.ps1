param (
  [Parameter(Mandatory = $true)][string]$assetUrl,
  [Parameter(Mandatory = $true)][string]$targetFolder,
  [Parameter(Mandatory = $true)][int]$processId
)

$ErrorActionPreference = "Stop"

try
{
  Write-Host Testing Updating...

  Write-Host Waiting for current application to finish...
  Wait-Process -Id $processId -ErrorAction SilentlyContinue

  Write-Host Simulating download...
  Start-Sleep -Seconds 3

  Write-Host Starting...
  & "$targetFolder\SimpleAccounting.exe"
}
catch
{
  $shell = New-Object -ComObject "WScript.Shell"
  $shell.Popup($_.Exception.Message, 0, "Update failed", 0) | Out-Null
}
