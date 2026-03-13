param(
    [switch]$NoBuild,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$ErrorActionPreference = 'Stop'

$dotnetArgs = @('test', '--nologo', '--verbosity', 'quiet', '--logger', 'console;verbosity=detailed')

if ($NoBuild) {
    $dotnetArgs += '--no-build'
}

if ($ExtraArgs) {
    $dotnetArgs += $ExtraArgs
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$all = & dotnet @dotnetArgs 2>&1
$stopwatch.Stop()
$exitCode = $LASTEXITCODE

$passedCount = 0
$failedCount = 0
$skippedCount = 0
$executionMs = 0
$diagnostics = New-Object System.Collections.Generic.List[string]

function Convert-TestTimeToMs {
    param([string]$raw)

    $text = if ($null -eq $raw) { '' } else { $raw.Trim() }
    if ($text -match '^<\s*1\s*ms$') {
        return 0
    }

    if ($text -match '^([0-9]+(?:[\.,][0-9]+)?)\s*ms$') {
        $value = [double]($Matches[1] -replace ',', '.')
        return [int][Math]::Round($value)
    }

    if ($text -match '^([0-9]+(?:[\.,][0-9]+)?)\s*s$') {
        $value = [double]($Matches[1] -replace ',', '.')
        return [int][Math]::Round($value * 1000)
    }

    return 0
}

foreach ($line in $all) {
    $text = [string]$line

    if ($text -match '^\s+(Passed|Failed|Skipped)\s+(.+?)\s+\[([^\]]+)\]\s*$') {
        $status = $Matches[1]
        $name = $Matches[2].Trim()
        $time = $Matches[3]
        $ms = Convert-TestTimeToMs $time
        $executionMs += $ms

        if ($name -match '^[A-Za-z0-9_.]+$' -and $name -like '*_*') {
            $name = ($name -split '\.')[-1]
            $name = [regex]::Replace($name, '_', ' ')
            $name = [regex]::Replace($name, '(?<=[a-z0-9])(?=[A-Z])', ' ')
        }

        if ([string]::IsNullOrWhiteSpace($name)) {
            $name = '(ismeretlen teszt)'
        }

        if ($status -eq 'Passed') {
            $passedCount++
            Write-Host "" -NoNewline
            Write-Host "PASSED" -ForegroundColor Green -NoNewline
            Write-Host (": {0} ({1}ms)" -f $name, $ms)
        }
        elseif ($status -eq 'Failed') {
            $failedCount++
            Write-Host "" -NoNewline
            Write-Host "FAILED" -ForegroundColor Red -NoNewline
            Write-Host (": {0} ({1}ms)" -f $name, $ms)
        }
        else {
            $skippedCount++
            Write-Host ("SKIPPED: {0} ({1}ms)" -f $name, $ms)
        }

        continue
    }

    if (
        $text -like '*: warning *' -or
        $text -like '*: error *' -or
        $text -like '*error TESTERROR:*'
    ) {
        $diagnostics.Add($text)
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host "Test Run Summary"
Write-Host "========================================"

$totalTests = $passedCount + $failedCount + $skippedCount
$resultText = if ($failedCount -eq 0) { 'SUCCEEDED' } else { 'FAILED' }
$executionSec = [Math]::Round($executionMs / 1000.0, 2).ToString('0.00', [System.Globalization.CultureInfo]::InvariantCulture)
$totalMs = [int][Math]::Round($stopwatch.Elapsed.TotalMilliseconds)
$totalSec = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2).ToString('0.00', [System.Globalization.CultureInfo]::InvariantCulture)

Write-Host ("Total Tests: {0}" -f $totalTests)
Write-Host ("Passed: {0}" -f $passedCount)
Write-Host ("Failed: {0}" -f $failedCount)
Write-Host ("Skipped: {0}" -f $skippedCount)
Write-Host "Result: " -NoNewline
if ($failedCount -eq 0) {
    Write-Host "SUCCEEDED" -ForegroundColor Green
}
else {
    Write-Host "FAILED" -ForegroundColor Red
}
Write-Host ("Test Execution Time: {0}s ({1}ms)" -f $executionSec, $executionMs)
Write-Host ("Total Time (including build): {0}s ({1}ms)" -f $totalSec, $totalMs)
Write-Host "========================================"

if ($failedCount -gt 0 -and $diagnostics.Count -gt 0) {
    Write-Host ""
    Write-Host "Diagnostics"
    foreach ($diag in $diagnostics) {
        Write-Host $diag
    }
}

exit $exitCode
