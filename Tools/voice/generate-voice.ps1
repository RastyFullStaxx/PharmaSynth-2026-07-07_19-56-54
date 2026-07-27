# PharmaSynth NPC voice generation (user-approved ElevenLabs TTS, 2026-07-11).
#
# Reads Assets/PharmaSynth/Audio/Voice/voice-manifest.json (from the Unity menu
# Tools > PharmaSynth > Voice > Export Voice Manifest) and generates one MP3 per
# line into Assets/PharmaSynth/Audio/Voice/<Speaker>/<id>.mp3. Incremental: a
# file that already exists is skipped, so re-runs only fetch new/changed lines
# (a changed line gets a new id).
#
# Usage (PowerShell, from the repo root):
#   $env:ELEVENLABS_API_KEY = "sk_..."          # your key — NEVER commit it
#   .\Tools\voice\generate-voice.ps1 -SampleOnly    # 2 lines per speaker → listen first!
#   .\Tools\voice\generate-voice.ps1                # full pass after voice approval
#
# Voices: pick from https://elevenlabs.io/voice-library and paste the ids below
# (defaults are ElevenLabs premade voices: bright/energetic for the robot guide,
# deep/authoritative for the examiner — audition and swap freely).
param(
    [string]$PharmeeVoiceId = "pFZP5JQG7iQjIQuC4Bku",   # "Lily" — bright, crisp
    [string]$JimenezVoiceId = "onwK4e9ZLuTAKqWW03F9",   # "Daniel" — stern, older male
    [string]$ModelId = "eleven_flash_v2_5",              # 0.5 credits/char
    [switch]$SampleOnly,
    # Generate ONE speaker only. The manifest is already written Jimenez-first, so
    # a plain run spends the budget on him before it reaches Pharmee; -Speaker is
    # the belt-and-braces version (user 2026-07-27: "prioritize dr jimenez").
    [ValidateSet("All", "Jimenez", "Pharmee")]
    [string]$Speaker = "All",
    # Generate ONE scene's worth of lines. The manifest tags every row with the
    # pool it came from, so you can buy the game a scene at a time and replace
    # Pharmee's placeholder chirps incrementally (user 2026-07-27). Names:
    #   Gate       - the whole front door flow: welcome, mode choice, experiment
    #                pick, PPE prompt, threshold, congrats, supply warning
    #   Greeting Praise Celebrate Encourage Idle Error Tour Review Exam
    #   Objectives Unlock Cutscene
    [string]$Group = "",
    # Generate only lines whose TEXT contains this substring — for auditioning a
    # single specific line before committing to a whole group.
    [string]$TextMatch = "",
    # Print what WOULD be generated, with the character cost, and exit. Free.
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
# Fall back to the PERSISTED user-scope value: a process started before the
# variable was set inherits a stale environment block, which looks exactly like
# "the key isn't set" (2026-07-27).
if (-not $env:ELEVENLABS_API_KEY) {
    $env:ELEVENLABS_API_KEY = [Environment]::GetEnvironmentVariable("ELEVENLABS_API_KEY", "User")
}
# -WhatIf costs nothing and touches no API, so it must not demand a key.
if (-not $env:ELEVENLABS_API_KEY -and -not $WhatIf) {
    throw "Set ELEVENLABS_API_KEY first (your ElevenLabs API key)."
}

$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$manifestPath = Join-Path $root "Assets/PharmaSynth/Audio/Voice/voice-manifest.json"
if (-not (Test-Path $manifestPath)) { throw "Manifest not found — run Tools > PharmaSynth > Voice > Export Voice Manifest in Unity first." }

# -Encoding UTF8 is REQUIRED (2026-07-27): Unity writes the manifest as UTF-8
# with NO BOM, and PowerShell 5.1 then falls back to the ANSI codepage — turning
# every em-dash into the three chars "a-euro-quote", which were duly sent to the
# API to be PRONOUNCED. The request succeeds, so this fails silently in the audio.
$manifest = Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$log = @()
$done = 0; $skipped = 0; $failed = 0
$sampleCount = @{ Pharmee = 0; Jimenez = 0 }

if ($WhatIf) {
    $sel = $manifest.lines | Where-Object {
        ($Speaker -eq "All" -or $_.speaker -eq $Speaker) -and
        ($Group -eq "" -or $_.group -eq $Group) -and
        ($TextMatch -eq "" -or $_.text -like "*$TextMatch*")
    }
    $chars = ($sel | Measure-Object -Property chars -Sum).Sum
    Write-Host ("WhatIf: {0} line(s), {1} characters, ~{2} ElevenLabs credits. Nothing generated." -f `
        $sel.Count, $chars, [int]($chars / 2)) -ForegroundColor Cyan
    foreach ($l in $sel) { Write-Host ("  [{0}/{1}] {2}" -f $l.speaker, $l.group, $l.text) }
    return
}

$quotaOut = $false
foreach ($line in $manifest.lines) {
    if ($quotaOut) { break }
    if ($Speaker -ne "All" -and $line.speaker -ne $Speaker) { continue }
    if ($Group -ne "" -and $line.group -ne $Group) { continue }
    if ($TextMatch -ne "" -and $line.text -notlike "*$TextMatch*") { continue }
    if ($SampleOnly) {
        if ($sampleCount[$line.speaker] -ge 2) { continue }
        $sampleCount[$line.speaker]++
    }

    $dir = Join-Path $root ("Assets/PharmaSynth/Audio/Voice/" + $line.speaker)
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
    $file = Join-Path $dir ($line.id + ".mp3")
    if (Test-Path $file) { $skipped++; continue }

    $voiceId = if ($line.speaker -eq "Jimenez") { $JimenezVoiceId } else { $PharmeeVoiceId }
    $body = @{ text = $line.text; model_id = $ModelId } | ConvertTo-Json -Depth 3
    $uri = "https://api.elevenlabs.io/v1/text-to-speech/$voiceId`?output_format=mp3_44100_128"

    try {
        # UTF-8 BYTES, not a string (2026-07-27): Windows PowerShell 5.1 encodes a
        # string body with the ContentType's charset, defaulting to Latin-1 — which
        # mangles every em-dash into invalid UTF-8 and earns a flat 400 from the API.
        # A quarter of this corpus contains an em-dash, so it silently ate 25% of
        # the run. Sending bytes with an explicit charset is the fix.
        $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        Invoke-RestMethod -Method Post -Uri $uri -Body $bodyBytes `
            -ContentType "application/json; charset=utf-8" `
            -Headers @{ "xi-api-key" = $env:ELEVENLABS_API_KEY } -OutFile $file
        $done++
        $log += "$($line.speaker),$($line.id),$($line.chars),ok"
        Write-Host ("[{0}] {1}  {2}" -f $line.speaker, $line.id, $line.text.Substring(0, [Math]::Min(60, $line.text.Length)))
        Start-Sleep -Milliseconds 350   # stay well under the API rate limit
    }
    catch {
        $failed++
        $msg = $_.Exception.Message
        $log += "$($line.speaker),$($line.id),$($line.chars),FAILED: $msg"
        Write-Warning ("FAILED {0}: {1}" -f $line.id, $msg)
        # A partial/zero file from a failed call would be "already done" on the next
        # run and never regenerate — delete it.
        if (Test-Path $file) { Remove-Item $file -Force }
        # Out of credits: stop cleanly rather than hammering the API for every
        # remaining line (user 2026-07-27: "stop only when we ran out of tokens").
        if ($msg -match "quota|401|402|429|credit") {
            $quotaOut = $true
            Write-Host "Credits exhausted (or rate-limited) — stopping here. Re-run later to resume; finished lines are skipped." -ForegroundColor Yellow
        }
    }
}

$log | Out-File (Join-Path $PSScriptRoot "generation-log.csv") -Encoding utf8
Write-Host ""
Write-Host ("Generated {0}, skipped {1} existing, {2} failed." -f $done, $skipped, $failed)
Write-Host "Next: in Unity run Tools > PharmaSynth > Voice > Import & Wire Voice Clips."
