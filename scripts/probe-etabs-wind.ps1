[CmdletBinding()]
param(
    [string]$ApiPath = 'C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll',
    [string]$OutputPath = (Join-Path (Get-Location) 'QKS-etabs-wind-host-report.json')
)
# Read only: attaches to an already running ETABS; never starts analysis, changes units or writes loads.
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run on Windows with ETABS already open.' }
Add-Type -Path (Resolve-Path -LiteralPath $ApiPath).Path
$helper = [CSiAPIv1.cHelper](New-Object CSiAPIv1.Helper)
$application = $helper.GetObject('CSI.ETABS.API.ETABSObject')
if (-not $application) { throw 'No running ETABS instance was returned by the helper.' }
$sap = $application.SapModel
function Assert-CsiCode([int]$Code, [string]$Operation) { if ($Code -ne 0) { throw "$Operation returned $Code" } }
$modelPath = $sap.GetModelFilename($true)
[string]$version = ''; [double]$versionNumber = 0
Assert-CsiCode ($sap.GetVersion([ref]$version, [ref]$versionNumber)) 'GetVersion'
$units = $sap.GetPresentUnits()
[int]$count = 0; [string[]]$names = @()
Assert-CsiCode ($sap.LoadPatterns.GetNameList([ref]$count, [ref]$names)) 'LoadPatterns.GetNameList'
$patterns = @()
foreach ($name in $names) {
    $loadType = [CSiAPIv1.eLoadPatternType]::Wind
    Assert-CsiCode ($sap.LoadPatterns.GetLoadType($name, [ref]$loadType)) "GetLoadType $name"
    if ($loadType -ne [CSiAPIv1.eLoadPatternType]::Wind) { continue }
    [string]$code = ''
    $codeResult = $sap.LoadPatterns.GetAutoWindCode($name, [ref]$code)
    $record = [ordered]@{ pattern = $name; autoWindCode = $code; codeResult = $codeResult; units = [string]$units }
    if ($codeResult -eq 0 -and $code.Trim() -eq 'User Loads') {
        [int]$n = 0; [string[]]$keys = @()
        [double[]]$fx = @(); [double[]]$fy = @(); [double[]]$mz = @(); [double[]]$x = @(); [double[]]$y = @()
        $result = $sap.LoadPatterns.AutoWind.GetUserLoad($name, [ref]$n, [ref]$keys, [ref]$fx, [ref]$fy, [ref]$mz, [ref]$x, [ref]$y)
        $record.readResult = $result; $record.count = $n
        $record.rawArrays = [ordered]@{ diaphragmKeys = $keys; fx = $fx; fy = $fy; mz = $mz; x = $x; y = $y }
    }
    $patterns += $record
}
[int]$diaphragmCount = 0; [string[]]$diaphragms = @()
$diaphragmResult = $sap.Diaphragm.GetNameList([ref]$diaphragmCount, [ref]$diaphragms)
[int]$tableCount = 0; [string[]]$tableKeys = @(); [string[]]$tableNames = @(); [int[]]$importTypes = @()
$tableResult = $sap.DatabaseTables.GetAvailableTables([ref]$tableCount, [ref]$tableKeys, [ref]$tableNames, [ref]$importTypes)
$interestingTables = @()
if ($tableResult -eq 0) { $interestingTables = @($tableKeys | Where-Object { $_ -match 'Diaphragm|Wind|Center|Modal' }) }
if ($sap.GetModelFilename($true) -ne $modelPath -or $sap.GetPresentUnits() -ne $units) { throw 'Model or units changed while reading. Repeat the probe.' }
$report = [ordered]@{
    capturedUtc = [DateTime]::UtcNow.ToString('o'); product = 'ETABS'; version = $version
    modelPath = $modelPath; units = [string]$units; patterns = $patterns
    diaphragmResult = $diaphragmResult; diaphragmNames = $diaphragms
    tableResult = $tableResult; availableRelevantTableKeys = $interestingTables
    scope = 'Read only. Raw current units. No period/rigidity-center inference or write compatibility claim. A diaphragm definition name need not identify a unique story assignment.'
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host "Saved: $OutputPath"
Write-Host 'Report includes model paths and load values; review before sharing.'
