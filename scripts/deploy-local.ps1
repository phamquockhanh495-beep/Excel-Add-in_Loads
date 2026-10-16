[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$PackagePath, [string]$Destination = 'C:\QKS_JSON_Data\Loads', [int]$AcadVersion = 0)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run on Windows.' }
$PackagePath = (Resolve-Path $PackagePath).Path
$manifest = Get-Content (Join-Path $PackagePath 'manifest.json') -Raw | ConvertFrom-Json
$supported = [int[]]@($manifest.autoCAD)
if ($AcadVersion -eq 0) {
    if ($supported.Count -ne 1) { throw 'This package contains several adapters. Specify -AcadVersion (2018-2026).' }
    $AcadVersion = $supported[0]
}
if ($AcadVersion -lt 2018 -or $AcadVersion -gt 2026 -or $supported -notcontains $AcadVersion) { throw 'Unsupported AutoCAD version for this package.' }
# Never attempt to overwrite a loaded assembly; no hidden best-effort copy.
if (Get-Process EXCEL, acad -ErrorAction SilentlyContinue) { throw 'Close Excel and AutoCAD before deployment.' }
foreach ($file in $manifest.files) {
    $path = [IO.Path]::GetFullPath((Join-Path $PackagePath $file.path))
    if (-not $path.StartsWith($PackagePath.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid manifest path.' }
    if ((Get-FileHash $path -Algorithm SHA256).Hash -ne $file.sha256) { throw "Hash mismatch: $($file.path)" }
}
# A custom destination must be selected by QKS_HOME in the Excel process as documented.
$cadTarget = Join-Path $Destination "AutoCAD/$AcadVersion"
$excelTarget = Join-Path $Destination 'Excel'
foreach ($target in @($cadTarget, $excelTarget)) {
    if (Test-Path $target) {
        $backup = "$target.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')-$([Guid]::NewGuid().ToString('N'))"
        Copy-Item $target $backup -Recurse -ErrorAction Stop
    }
    New-Item -ItemType Directory -Path $target -Force | Out-Null
}
Copy-Item "$PackagePath/AutoCAD/$AcadVersion/*" $cadTarget -Force -ErrorAction Stop
Copy-Item "$PackagePath/Excel/*" $excelTarget -Force -ErrorAction Stop
Copy-Item "$PackagePath/manifest.json" "$Destination/manifest-$AcadVersion.json" -Force
Write-Host "Deployed to $Destination. Restart AutoCAD/Excel and load Excel/QksExcelAddIn-AddIn64-packed.xll."
