# check whether build on tag
$version = "DEVEL"

Write-Host ref = $env:GITHUB_REF

if ($env:GITHUB_REF -match "/tags/") {
  # use tag as version name
  Write-Host building tag...
  $version = $env:GITHUB_REF -replace "(\w+/)*",""
} else {
  if ($env:GITHUB_REF -match "/pull/") {
    # use branch as version name base
    Write-Host building PR...
    $version = $env:GITHUB_PR_REF
  }

  # use git sha as version name
  $version = $version + "-" + ($env:GITHUB_SHA).Substring(0, 8)
}

Write-Host version = $version

$file = gi .\src\SimpleAccounting\SimpleAccounting.csproj
$xml = [xml](gc $file)
$xml.Project.PropertyGroup.InformationalVersion = $version

$utf8 = New-Object System.Text.UTF8Encoding($true)
$sw = New-Object System.IO.StreamWriter($file.Fullname, $false, $utf8)
$xml.Save( $sw )
$sw.Close()
