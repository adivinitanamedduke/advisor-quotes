# UserPromptSubmit hook: appends every prompt to docs/ai/PROMPTS.md in the MAIN checkout,
# so prompts from all sessions (including git worktrees) land in one file.
# Never blocks the prompt: any failure exits 0 silently.
try {
    [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json

    $cwd = if ($payload.cwd) { $payload.cwd } else { (Get-Location).Path }

    # The common git dir is the main checkout's .git, even from inside a worktree
    $commonDir = (& git -C $cwd rev-parse --path-format=absolute --git-common-dir 2>$null)
    if (-not $commonDir) { exit 0 }
    $mainRoot = Split-Path -Parent $commonDir.Trim()
    $branch = (& git -C $cwd branch --show-current 2>$null)
    if (-not $branch) { $branch = 'detached' }

    $prompt = [string]$payload.prompt

    # Optional local, gitignored list of terms to mask (one per line)
    $redactions = Join-Path $mainRoot '.claude/hooks/redactions.local.txt'
    if (Test-Path $redactions) {
        foreach ($term in Get-Content $redactions -Encoding UTF8) {
            $t = $term.Trim()
            if ($t) { $prompt = [regex]::Replace($prompt, [regex]::Escape($t), '[internal reference]', 'IgnoreCase') }
        }
    }

    $log = Join-Path $mainRoot 'docs/ai/PROMPTS.md'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $log) | Out-Null

    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm')
    $session = ([string]$payload.session_id)
    if ($session.Length -gt 8) { $session = $session.Substring(0, 8) }

    $quoted = ($prompt -split "`r?`n" | ForEach-Object { "> $_" }) -join "`n"
    $entry = "`n### $stamp | branch ``$branch`` | session ``$session```n`n$quoted`n"

    [System.IO.File]::AppendAllText($log, $entry, (New-Object System.Text.UTF8Encoding $false))
} catch {
    # Logging must never stop the user's prompt
}
exit 0
