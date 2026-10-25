param(
    [Parameter(Mandatory = $true)][string]$ReportPath
)

$ErrorActionPreference = "Stop"
$report = Get-Content -Raw -Encoding utf8 -LiteralPath $ReportPath | ConvertFrom-Json
$errors = @()
foreach ($field in @("schema_version", "feature_id", "execution_id", "code_revision", "config_hash", "started_at", "duration_ms", "gates", "result")) {
    if ($null -eq $report.$field) { $errors += "Missing field: $field" }
}
if ($report.schema_version -ne "1.0") { $errors += "Unsupported schema_version: $($report.schema_version)" }
if ($report.result -notin @("passed", "failed", "blocked", "error")) { $errors += "Invalid aggregate result: $($report.result)" }
$allowed = @("passed", "failed", "blocked", "error", "skipped")
foreach ($gate in @($report.gates)) {
    foreach ($field in @("id", "name", "required", "status", "command", "duration_ms", "diagnostics", "evidence")) {
        if ($null -eq $gate.$field) { $errors += "$($gate.name): missing field $field" }
    }
    if ($gate.status -notin $allowed) { $errors += "$($gate.name): invalid status $($gate.status)" }
}
if ($errors.Count) { $errors | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Output "Gate report is valid: $ReportPath"
