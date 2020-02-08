# check whether build on tag
$version = "DEVEL"
if ($env:GITHUB_REF -match "/tags/") {
  # use tag as version name
  $version = $env:GITHUB_REF -replace "(\w+/)*",""
} else {
  # use git sha as version name
  $version = $env:GITHUB_SHA
}

Write-Host version = $version

$file = gi .\src\SimpleAccounting\SimpleAccounting.csproj
$xml = [xml](gc $file)
$xml.Project.PropertyGroup.InformationalVersion = $version

$utf8 = New-Object System.Text.UTF8Encoding($true)
$sw = New-Object System.IO.StreamWriter($file.Fullname, $false, $utf8)
$xml.Save( $sw )
$sw.Close()