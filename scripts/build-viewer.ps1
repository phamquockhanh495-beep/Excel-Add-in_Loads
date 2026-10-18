[CmdletBinding()]
param([string]$EtabsApiPath = 'C:\Program Files\Computers and Structures\ETABS 22\CSiAPIv1.dll', [switch]$RunBenchmark)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT' -or -not [Environment]::Is64BitOperatingSystem) { throw 'Build on Windows x64 with .NET SDK 8 and .NET Framework 4.8 targeting support.' }
if (-not (Test-Path -LiteralPath $EtabsApiPath)) { throw "Missing ETABS API: $EtabsApiPath" }
$repo = Split-Path -Parent $PSScriptRoot
Push-Location $repo
try {
    & dotnet run --project tests/Qks.Core.Tests/Qks.Core.Tests.csproj -c Release -p:RestoreLockedMode=true -- $repo
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
    if ($RunBenchmark) {
        $benchmarkPath = Join-Path $repo 'artifacts/test-results/Preview-5/benchmark.json'
        & dotnet run --project tests/Qks.Core.Tests/Qks.Core.Tests.csproj -c Release --no-build -- $repo --benchmark $benchmarkPath
        if ($LASTEXITCODE -ne 0) { throw 'Core/VM benchmark failed.' }
    }
    & dotnet build QksExcelAddIn.csproj -c Release -p:RestoreLockedMode=true "-p:EtabsApiPath=$EtabsApiPath" -p:ExcelDnaCreate32BitAddIn=false -p:ExcelDnaCreate64BitAddIn=true
    if ($LASTEXITCODE -ne 0) { throw 'Excel build failed.' }
    $file = Join-Path $repo 'artifacts/bin/QksExcelAddIn/Release/net48/publish/QksExcelAddIn-AddIn64-packed.xll'
    if (-not (Test-Path -LiteralPath $file)) { throw 'Packed XLL was not produced.' }
    Write-Host "Excel viewer build: $file"
    Get-FileHash -LiteralPath $file -Algorithm SHA256
} finally { Pop-Location }
