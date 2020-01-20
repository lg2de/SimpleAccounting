$ErrorActionPreference = "Stop"

Write-Host --- running tests with coverage ---
$packagesFolder = $PSScriptRoot + "\packages"
$openCoverDir = (Get-ChildItem -Path "$packagesFolder\OpenCover" -Directory | select -last 1).Name
$openCoverExe = $packagesFolder + "\OpenCover\$openCoverDir\tools\OpenCover.Console.exe"
$dotNetExe = $env:ProgramFiles + "\dotnet\dotnet.exe"
$testArguments = "test --no-restore --no-build"
if (Test-Path $PSScriptRoot\CoverOutput) {
  Remove-Item $PSScriptRoot\CoverOutput -Confirm:$false -Force -Recurse
}
$dummy = md $PSScriptRoot\CoverOutput
$coverageFilter = "+[SimpleAccounting*]* -[SimpleAccounting.UnitTests*]*"
$coverageAttributeExclude = "*ExcludeFromCodeCoverage*"
$coverageFileExclude = "*.designer.cs;*.g.cs"
$outputFile = "$PSScriptRoot\CoverOutput\coverage.xml"
& $openCoverExe -register -target:$dotNetExe -targetargs:$testArguments -output:$outputFile -filter:$coverageFilter -excludebyattribute:$coverageAttributeExclude -excludebyfile:$coverageFileExclude

Write-Host --- creating coverage report ---
$generatorDir = (Get-ChildItem -Path "$packagesFolder\ReportGenerator" -Directory | select -last 1).Name
$generatorExeDir = (Get-ChildItem -Path "$packagesFolder\ReportGenerator\$generatorDir\tools\" -Directory -Filter netcoreapp* | select -last 1).Name
$generatorExe = "$packagesFolder\ReportGenerator\$generatorDir\tools\$generatorExeDir\ReportGenerator.exe"
$reportDir = "$PSScriptRoot\CoverReport"
if (Test-Path $reportDir) {
  Remove-Item $reportDir -Confirm:$false -Force -Recurse
}
& $generatorExe -reports:$outputFile -targetdir:$reportDir
