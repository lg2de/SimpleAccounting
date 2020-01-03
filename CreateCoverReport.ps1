$ErrorActionPreference = "Stop"

Write-Host --- running tests with coverage ---
$openCoverDir = (Get-ChildItem -Path $PSScriptRoot\packages\ -Directory -Filter OpenCover* | select -last 1).Name
$openCoverExe = $PSScriptRoot + "\packages\$openCoverDir\tools\OpenCover.Console.exe"
$xunitDir = (Get-ChildItem -Path $PSScriptRoot\packages\ -Directory -Filter xunit.runner.console* | select -last 1).Name
$xunitExeDir = (Get-ChildItem -Path $PSScriptRoot\packages\$xunitDir\tools\ -Directory -Filter net4* | select -last 1).Name
$xunitExe = $PSScriptRoot + "\packages\$xunitDir\tools\$xunitExeDir\xunit.console.x86.exe"
$testAssemblies = $PSScriptRoot + "\tests\SimpleAccounting.UnitTests\bin\Debug\SimpleAccounting.UnitTests.dll"
$testArguments = "$testAssemblies -noshadow -verbose"
if (Test-Path $PSScriptRoot\CoverOutput) {
  Remove-Item $PSScriptRoot\CoverOutput -Confirm:$false -Force -Recurse
}
$dummy = md $PSScriptRoot\CoverOutput
$coverageFilter = "+[SimpleAccounting*]* -[SimpleAccounting.UnitTests*]*"
$coverageAttributeExclude = "*ExcludeFromCodeCoverage*"
$coverageFileExclude = "*.designer.cs;*.g.cs"
$outputFile = "$PSScriptRoot\CoverOutput\coverage.xml"
& $openCoverExe -register -target:$xunitExe -targetargs:$testArguments -output:$outputFile -filter:$coverageFilter -excludebyattribute:$coverageAttributeExclude -excludebyfile:$coverageFileExclude

Write-Host --- creating coverage report ---
$generatorDir = (Get-ChildItem -Path $PSScriptRoot\packages\ -Directory -Filter ReportGenerator* | select -last 1).Name
$generatorExeDir = (Get-ChildItem -Path $PSScriptRoot\packages\$generatorDir\tools\ -Directory -Filter net4* | select -last 1).Name
$generatorExe = $PSScriptRoot + "\packages\$generatorDir\tools\$generatorExeDir\ReportGenerator.exe"
$reportDir = "$PSScriptRoot\CoverReport"
if (Test-Path $reportDir) {
  Remove-Item $reportDir -Confirm:$false -Force -Recurse
}
& $generatorExe -reports:$outputFile -targetdir:$reportDir
