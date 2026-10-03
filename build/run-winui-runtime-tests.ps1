# Builds and runs the native WinUI runtime tests (tests/PanAndZoom.WinUI.RuntimeTests) on Windows.
#
# Usage: build/run-winui-runtime-tests.ps1 [-Configuration Release] [-Filter "ClassA | ClassB"]
#
# The tests use real injected mouse and touch input, so the test window must stay in front and the
# mouse must not be used while they run. Results are written as NUnit XML to
# artifacts/test-results/winui-runtime-tests.xml; the script exits with a non-zero code on failure.
param(
    [string]$Configuration = "Release",
    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "tests/PanAndZoom.WinUI.RuntimeTests/PanAndZoom.WinUI.RuntimeTests.csproj"
$resultsDir = Join-Path $root "artifacts/test-results"
$results = Join-Path $resultsDir "winui-runtime-tests.xml"
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$rid = "win-$arch"

New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
Remove-Item -Force -ErrorAction SilentlyContinue $results, "$results.log"

dotnet build $project -c $Configuration -r $rid
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $root "tests/PanAndZoom.WinUI.RuntimeTests/bin/$Configuration/net10.0-windows10.0.19041.0/$rid/PanAndZoom.WinUI.RuntimeTests.exe"

$env:PANANDZOOM_WINUI_TESTS = if ([string]::IsNullOrWhiteSpace($Filter)) { "true" } else { $Filter }
$env:PANANDZOOM_WINUI_TESTS_OUTPUT = $results

$process = Start-Process -FilePath $exe -PassThru
if (-not $process.WaitForExit(30 * 60 * 1000)) {
    $process.Kill()
    Write-Error "WinUI runtime tests timed out."
    exit 1
}

if (Test-Path "$results.log") { Get-Content "$results.log" | Write-Host }

if (-not (Test-Path $results)) {
    Write-Error "WinUI runtime tests did not produce a result file at $results (exit code $($process.ExitCode))."
    exit 1
}

[xml]$xml = Get-Content $results
$run = $xml.'test-run'
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
    Write-Host "FAILED: $($case.fullname)"
    $message = $case.SelectSingleNode("failure/message")
    if ($message) { Write-Host "  $($message.InnerText.Trim())" }
}

Write-Host "WinUI runtime tests: total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped)"
if ([int]$run.failed -gt 0 -or [int]$run.total -eq 0) { exit 1 }
exit 0
