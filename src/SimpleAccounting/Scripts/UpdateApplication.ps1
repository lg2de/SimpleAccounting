param (
  [Parameter(Mandatory=$true)][string]$assetUrl,
  [Parameter(Mandatory=$true)][string]$targetFolder,
  [Parameter(Mandatory=$true)][int]$processId,
  [Parameter(Mandatory=$false)][int]$dryRun
)

$ErrorActionPreference = "Stop"

function Test-WriteAccess {
  param (
    [Parameter(Mandatory)][string]$Path
  )

  try {
    if (-not (Test-Path $Path)) {
      $null = New-Item -ItemType Directory -Path $Path -Force
    }

    $testFile = Join-Path $Path ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType File -Path $testFile -Force | Out-Null
    Remove-Item $testFile -Force
    return $true
  }
  catch {
    return $false
  }
}

try {
  Write-Host Updating SimpleAccounting...

  $tempFile = Join-Path $env:TEMP "update-SimpleAccounting.zip"
  Remove-Item $tempFile -ErrorAction SilentlyContinue

  Write-Host Waiting for current application to finish...
  Wait-Process -Id $processId -ErrorAction SilentlyContinue

  if ($dryRun -eq 1) {
    Write-Host Simulating download...
    $cmd = 'Start-Sleep -Seconds 3'
  } else {
    Write-Host Download new release...
    Write-Host $assetUrl
    Invoke-WebRequest -Uri $assetUrl -OutFile $tempFile

    $cmd = 'Expand-Archive -LiteralPath "$tempFile" -DestinationPath "$targetFolder" -Force'
  }
  if (Test-WriteAccess($targetFolder)) {
    Write-Host Extracting new release...
    Start-Process powershell.exe -Wait -NoNewWindow -ArgumentList "-NoProfile -Command $cmd"
  } else {
    Write-Host Extracting new release as administrator...
    Start-Process powershell.exe -Verb RunAs -Wait -WindowStyle Hidden -ArgumentList "-NoProfile -Command $cmd"
  }
  
  Write-Host Re-Starting application...
  & "$targetFolder\SimpleAccounting.exe"
} catch {
  $shell = New-Object -ComObject "WScript.Shell"
  $shell.Popup($_.Exception.Message, 0, "Update failed", 0) | Out-Null
}
