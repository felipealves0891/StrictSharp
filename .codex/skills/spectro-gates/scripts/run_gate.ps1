param(
    [Parameter(Mandatory = $true)][string]$Command,
    [int]$TimeoutSeconds = 900,
    [string]$EvidencePath = ""
)

$started = Get-Date
$output = ""
$status = "passed"
$exitCode = 0

try {
    $job = Start-Job -ScriptBlock {
        param($Text)
        $captured = & ([scriptblock]::Create($Text)) 2>&1 | Out-String
        [pscustomobject]@{
            output = $captured
            exit_code = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
        }
    } -ArgumentList $Command

    if (-not (Wait-Job -Job $job -Timeout $TimeoutSeconds)) {
        Stop-Job -Job $job
        $status = "blocked"
        $exitCode = 124
        $output = "Command timed out after $TimeoutSeconds seconds."
    } else {
        $result = Receive-Job -Job $job -ErrorAction SilentlyContinue
        $output = [string]$result.output
        $exitCode = [int]$result.exit_code
        if ($exitCode -ne 0) { $status = "failed" }
        if ($job.State -eq "Failed") {
            $status = "error"
            $exitCode = 1
        }
    }
} catch {
    $status = "error"
    $exitCode = 1
    $output = $_.Exception.Message
} finally {
    if ($job) { Remove-Job -Job $job -Force -ErrorAction SilentlyContinue }
}

if ($EvidencePath) {
    $parent = Split-Path -Parent $EvidencePath
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    Set-Content -Path $EvidencePath -Value $output -Encoding utf8
}

$durationMs = [int]((New-TimeSpan -Start $started -End (Get-Date)).TotalMilliseconds)
[pscustomobject]@{
    status = $status
    exit_code = $exitCode
    duration_ms = $durationMs
    output = $output
} | ConvertTo-Json -Depth 4

if ($exitCode -ne 0) { exit $exitCode }
