#!/usr/bin/env pwsh
<#
  compare_alsactl_alas_hints.ps1
  PowerShell 7 script to collect `alsactl info` and the Example.AlsaHints output,
  parse both, and print a simple comparison report.
#>

Set-StrictMode -Version Latest

$WorkingRoot = '/home/pistomp/alsa.net/src'
if (-not (Test-Path $WorkingRoot)) {
  Write-Error "Working root not found: $WorkingRoot"
  exit 2
}

$TmpDir = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ([System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $TmpDir | Out-Null
$AlsactlOut = Join-Path $TmpDir 'alsactl.info.txt'
$HintsOut = Join-Path $TmpDir 'alsahints.txt'

Write-Host "Gathering alsactl info..."
if (-not (Get-Command alsactl -ErrorAction SilentlyContinue)) {
  Write-Error 'alsactl not found in PATH'
  exit 2
}
try {
  & alsactl info 2>&1 | Out-File -FilePath $AlsactlOut -Encoding utf8
} catch {
  Write-Warning "alsactl returned non-zero exit code: $_"
}

Write-Host "Running Example.AlsaHints and capturing canonical JSON from stdout..."
Push-Location $WorkingRoot
try {
  $exampleOutput = & dotnet run --project examples/Example.AlsaHints --verbosity minimal 2>&1
  $exampleJson = ($exampleOutput -join "`n").Trim()
  # save a log of the run for inspection
  $exampleJson | Out-File -FilePath (Join-Path $TmpDir 'example_tool.log') -Encoding utf8
} catch {
  Write-Warning "Example.AlsaHints invocation failed: $_"
  Pop-Location
  exit 2
}
Pop-Location

try {
  $parsed = $exampleJson | ConvertFrom-Json -ErrorAction Stop
} catch {
  Write-Error "Failed to parse JSON emitted by Example.AlsaHints. See $TmpDir/example_tool.log for details: $_"
  exit 2
}

$cards = $parsed.Alsactl
$hints = $parsed.Hints

# (No longer copying raw outputs into the repo; avoid committing generated files.)

Write-Host "`nalsactl cards:`n"
foreach ($c in $cards) { Write-Host "card=$($c.CardIndex) id=$($c.Id) longname=$($c.LongName)" }

Write-Host "`nparsed hints:`n"
foreach ($h in $hints) {
  Write-Host "Name: $($h.Name)"
  Write-Host "  CardId: $($h.CardId)  CardIndex: $($h.CardIndex) DeviceIndex: $($h.DeviceIndex)"
  Write-Host "  LongName: $($h.LongName)"
  Write-Host "  Description:`n$($h.Description)`n"
}

Write-Host "`nComparison:`n"
function Normalize([object]$v) {
  if ($null -eq $v) { return $null }
  $s = [string]$v
  return ($s -replace '\s+', ' ' ).Trim()
}

$diffs = @()
foreach ($c in $cards) {
  $matches = @($hints | Where-Object { -not [string]::IsNullOrEmpty($_.CardId) -and $_.CardId.Equals($c.Id, 'InvariantCultureIgnoreCase') })
  if ($matches.Count -eq 0) {
    Write-Host "Card $($c.Id) (card $($c.CardIndex)) present in alsactl but not in hints"
    $diffs += [PSCustomObject]@{ CardId = $c.Id; CardIndex = $c.CardIndex; Issue = 'MissingInHints'; Details = $null }
    continue
  }
  Write-Host "Card $($c.Id) matches $($matches.Count) hint entries"
    foreach ($m in $matches) {
      Write-Host "  hint name: $($m.Name)"
      # compare LongName
      $alsLong = Normalize($c.LongName)
      $hintLong = Normalize($m.LongName)
      if ((-not [string]::IsNullOrEmpty($alsLong) -or -not [string]::IsNullOrEmpty($hintLong)) -and $alsLong -ne $hintLong) {
        Write-Host "    MISMATCH field=LongName alsactl=\"$alsLong\" hint=\"$hintLong\""
        $diffs += [PSCustomObject]@{ CardId = $c.Id; CardIndex = $c.CardIndex; HintName = $m.Name; Field = 'LongName'; Alsactl = $alsLong; Hint = $hintLong }
      }
      # compare CardIndex
      $alsIdx = $c.CardIndex
      $hintIdx = $m.CardIndex
      if ($alsIdx -ne $hintIdx) {
        Write-Host "    MISMATCH field=CardIndex alsactl=\"$alsIdx\" hint=\"$hintIdx\""
        $diffs += [PSCustomObject]@{ CardId = $c.Id; CardIndex = $c.CardIndex; HintName = $m.Name; Field = 'CardIndex'; Alsactl = $alsIdx; Hint = $hintIdx }
      }
      # compare DeviceIndex (hint.DeviceIndex should be present in Card device indexes)
      if ($null -ne $c.Pcm) {
        $cardDevIdxs = @()
        foreach ($stream in $c.Pcm) {
          foreach ($dev in $stream.Devices) { $cardDevIdxs += [int]$dev.DeviceIndex }
        }
        $cardDevIdxs = $cardDevIdxs | Select-Object -Unique
      }
      else {
        $cardDevIdxs = @($c.DeviceIndexes)
      }
      $hintDev = $m.DeviceIndex
      if ($hintDev -ne $null) {
        if (-not ($cardDevIdxs -contains [int]$hintDev)) {
          Write-Host "    MISMATCH field=DeviceIndex alsactl="$([string]::Join(',', $cardDevIdxs))" hint="$hintDev""
          $diffs += [PSCustomObject]@{ CardId = $c.Id; CardIndex = $c.CardIndex; HintName = $m.Name; Field = 'DeviceIndex'; Alsactl = ($cardDevIdxs -join ','); Hint = $hintDev }
        }
      }
      # compare Description vs device ids/names summary, but only for device-specific hints (those with DEV=)
      if ($m.Name -match 'DEV=') {
        $hintDesc = Normalize($m.Description)
        $found = $false
        if ($null -ne $c.Pcm) {
          $cardDeviceIds = @()
          foreach ($stream in $c.Pcm) {
            foreach ($dev in $stream.Devices) { $cardDeviceIds += $dev.Id }
          }
        }
        else {
          $cardDeviceIds = @($c.DeviceIds)
        }
        foreach ($devId in @($cardDeviceIds)) {
          if (-not [string]::IsNullOrEmpty($devId)) {
            if ($hintDesc -like "*$(Normalize($devId))*") { $found = $true; break }
          }
        }
        if ((-not [string]::IsNullOrEmpty($hintDesc)) -and -not $found) {
          $cardDeviceSummary = Normalize(($cardDeviceIds -join '; '))
          Write-Host "    MISMATCH field=Description alsactl="$cardDeviceSummary" hint="$hintDesc""
          $diffs += [PSCustomObject]@{ CardId = $c.Id; CardIndex = $c.CardIndex; HintName = $m.Name; Field = 'Description'; Alsactl = $cardDeviceSummary; Hint = $hintDesc }
        }
      }
    }
}

# Save machine-readable diffs into the temporary directory (do not modify repo)
$DiffPath = Join-Path $TmpDir 'compare_alsactl_alas_hints.diffs.json'
$diffs | ConvertTo-Json -Depth 5 | Out-File -FilePath $DiffPath -Encoding utf8
Write-Host "`nWrote detailed diffs to $DiffPath`n"

Write-Host "`nDone. Temp files in $TmpDir`n"
Get-ChildItem -Path $TmpDir -File | Format-Table Name,Length

exit 0
