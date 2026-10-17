[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ApiPath,
    [ValidateSet('SAFE', 'ETABS')][string]$Product = 'SAFE',
    [string]$OutputPath = (Join-Path (Get-Location) 'QKS-csi-api-report.json')
)
# Metadata only: does not attach to a model, start CSI or write to any CSI model.
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run on Windows with the API from the installed CSI product.' }
$api = (Resolve-Path -LiteralPath $ApiPath).Path
if ([IO.Path]::GetExtension($api) -ne '.dll') { throw 'ApiPath must identify the CSI API DLL.' }
$assembly = [Reflection.Assembly]::LoadFrom($api)
$methods = @()
$wanted = @('GetVersion', 'GetPresentUnits', 'SetPresentUnits', 'GetPresentUnits_2', 'GetModelFilename', 'GetModelIsLocked', 'Save', 'GetNameList', 'GetCoordCartesian', 'GetPoints', 'GetSection', 'GetProperty', 'GetOpening', 'GetDesignOrientation', 'GetLoadDistributed', 'SetLoadDistributed', 'DeleteLoadDistributed', 'GetLoadUniform', 'SetLoadUniform', 'DeleteLoadUniform', 'GetElevation', 'GetUserLoad', 'SetUserLoad', 'GetAutoWindCode', 'GetLoadType', 'GetDiaphragm', 'ModalPeriod', 'GetAvailableTables', 'GetTableForDisplayArray')
foreach ($typeName in @('cSapModel', 'cFile', 'cPointObj', 'cFrameObj', 'cAreaObj', 'cStory', 'cGridSys', 'cLoadPatterns', 'cAutoWind', 'cDiaphragm', 'cAnalysisResults', 'cDatabaseTables')) {
    $type = $assembly.GetTypes() | Where-Object { $_.Name -eq $typeName } | Select-Object -First 1
    if (-not $type) { $methods += [ordered]@{ type = $typeName; status = 'not-found' }; continue }
    foreach ($method in $type.GetMethods() | Where-Object { $_.Name -in $wanted }) {
        $methods += [ordered]@{
            type = $type.FullName; method = $method.Name
            returns = $method.ReturnType.FullName
            parameters = @($method.GetParameters() | ForEach-Object { [ordered]@{ name = $_.Name; type = $_.ParameterType.FullName; optional = $_.IsOptional; defaultValue = if ($_.IsOptional) { [string]$_.DefaultValue } else { $null } } })
        }
    }
}
$file = Get-Item -LiteralPath $api
$report = [ordered]@{
    product = $Product
    assemblyName = $assembly.GetName().Name
    assemblyVersion = $assembly.GetName().Version.ToString()
    fileVersion = $file.VersionInfo.FileVersion
    productVersion = $file.VersionInfo.ProductVersion
    sha256 = (Get-FileHash -LiteralPath $api -Algorithm SHA256).Hash
    capturedUtc = [DateTime]::UtcNow.ToString('o')
    scope = 'Assembly metadata only. Does not prove COM attachment, model reading, writing or version compatibility.'
    methods = $methods
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host "Report saved: $OutputPath"
Write-Host 'SAFE 18-22 remain unverified until tested with the corresponding installed hosts.'
