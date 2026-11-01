---
description: Generate a professional commit message from staged git changes
---

# Commit Message Workflow

## Rules

1. **Convention Authority**: Follow `docs/COMMIT_CONVENTION.md` as the only source of truth for commit message format, types, scopes, subject, body, and footers.
2. **Single focus**: only inspect the staged changes (`git diff --cached`) — never look at unstaged files or the working tree.
3. **Output**: always deliver the final commit message inside a fenced code block with no extra commentary unless the user asks a question.

## Steps

// turbo-all
1. Retrieve historical context for tone and vocabulary:
   ```powershell
   git log -n 15 --format="%s%n%b%n---"
   ```

2. Retrieve the staged diff:
   ```powershell
   git diff --cached
   ```

3. Analyse the diff and historical context:
   - Derive type, scope, subject, body, and footers strictly according to `docs/COMMIT_CONVENTION.md`.
   - Use recent commit history solely to calibrate tone and project vocabulary.

4. Compose the commit message strictly following `docs/COMMIT_CONVENTION.md` and present it in a single fenced code block.
