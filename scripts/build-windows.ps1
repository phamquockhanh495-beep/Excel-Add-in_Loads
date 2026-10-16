[CmdletBinding()]
param(
    [ValidateRange(2018, 2026)][int]$AcadVersion = 2024,
    [string]$AcadSdkDir = "",
    [string]$EtabsApiPath = "C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll"
)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run this script on Windows x64.' }
if (-not [Environment]::Is64BitOperatingSystem) { throw 'Windows x64 is required.' }
$repo = Split-Path -Parent $PSScriptRoot
if (-not $AcadSdkDir) { $AcadSdkDir = "C:\Program Files\Autodesk\AutoCAD $AcadVersion" }
foreach ($file in @('acmgd.dll','acdbmgd.dll','accoremgd.dll')) {
    if (-not (Test-Path (Join-Path $AcadSdkDir $file))) { throw "Missing AutoCAD $AcadVersion SDK file: $file" }
}
if (-not (Test-Path $EtabsApiPath)) { throw "Missing ETABS API: $EtabsApiPath" }
function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
Push-Location $repo
try {
    Invoke-Dotnet -Arguments @('run','--project','tests/Qks.Core.Tests/Qks.Core.Tests.csproj','-c','Release','-p:RestoreLockedMode=true','--',$repo)
    Invoke-Dotnet -Arguments @('build','QksExcelAddIn.csproj','-c','Release','-p:RestoreLockedMode=true',"-p:EtabsApiPath=$EtabsApiPath",'-p:ExcelDnaCreate32BitAddIn=false','-p:ExcelDnaCreate64BitAddIn=true')
    Invoke-Dotnet -Arguments @('build','QKS_Loads_AcadPlugin.csproj','-c','Release','-p:RestoreLockedMode=true',"-p:AcadVersion=$AcadVersion","-p:AcadSdkDir=$AcadSdkDir")
    $framework = if ($AcadVersion -eq 2018) { 'net46' } elseif ($AcadVersion -le 2020) { 'net47' } elseif ($AcadVersion -le 2024) { 'net48' } else { 'net8.0-windows' }
    $excelOutput = Join-Path $repo 'artifacts/bin/QksExcelAddIn/Release/net48/publish/QksExcelAddIn-AddIn64-packed.xll'
    $cadOutput = Join-Path $repo "artifacts/bin/QKS_Loads_AcadPlugin/$AcadVersion/Release/$framework"
    if (-not (Test-Path $excelOutput)) { throw 'Packed Excel XLL was not produced.' }
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $package = Join-Path $repo "artifacts/packages/QKS-$AcadVersion-$stamp"
    New-Item -ItemType Directory -Path "$package/Excel", "$package/AutoCAD/$AcadVersion" -Force | Out-Null
    Copy-Item $excelOutput "$package/Excel/"
    foreach ($name in @('QKS_Loads_AcadPlugin.dll','Newtonsoft.Json.dll')) {
        Copy-Item (Join-Path $cadOutput $name) "$package/AutoCAD/$AcadVersion/" -ErrorAction Stop
    }
    $deps = Join-Path $cadOutput 'QKS_Loads_AcadPlugin.deps.json'
    if (Test-Path $deps) { Copy-Item $deps "$package/AutoCAD/$AcadVersion/" }
    Copy-Item (Join-Path $repo 'licenses/Newtonsoft.Json-LICENSE.md') "$package/"
    Copy-Item (Join-Path $repo 'docs/BUILD-AND-TEST.md') "$package/BUILD-AND-TEST.md"
    $manifest = [ordered]@{
        sourceCommit = (& git rev-parse HEAD)
        workingTreeChanged = [bool](& git status --porcelain)
        builtAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        autoCAD = $AcadVersion; framework = $framework; platform = 'Windows x64 / Excel 64-bit'
        validation = 'Compiled and core tests passed; Windows host integration must be tested separately.'
        files = @(Get-ChildItem $package -File -Recurse | ForEach-Object {
            @{ path = $_.FullName.Substring($package.Length + 1); sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
        })
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content "$package/manifest.json" -Encoding UTF8
    Write-Host "Package ready for local testing: $package"
    Write-Host 'No files were deployed into a running Excel/AutoCAD session.'
} finally { Pop-Location }
