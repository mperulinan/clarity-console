---
description: Automatically analyze, group, stage, and commit all Git changes.
---

# Smart Commit Workflow

## Rules

1. **Convention Authority**: Follow `docs/COMMIT_CONVENTION.md` as the only source of truth for commit message format, types, scopes, subject, body, and footers.
2. **Analyze All Changes**: Inspect both staged and unstaged changes (`git status`, `git diff --cached`, `git diff`).
3. **Logical Grouping**: Determine if the changes should be split into multiple logical commits based on feature areas, types of changes, or specific files.
4. **Execution over Output**: Do not just present the commit messages. You MUST automatically execute the `git add` and `git commit` commands for each logical group sequentially. Do NOT wait for user approval before running the commands (the IDE will handle the terminal execution approval).
5. **PowerShell Multiline Commits**: Use a separate `-m` flag for the subject and each individual bullet point or paragraph in the body. Do not use newline characters (`\n` or literal newlines) inside a single `-m` string. Escape any literal `$` as `` `$ `` inside double-quoted strings.

## Steps

// turbo-all
1. Retrieve historical context, current status, and all changes (staged and unstaged) in one go:
   ```powershell
   git log -n 15 --format="%s%n%b%n---"; git status; git diff --cached; git diff
   ```

2. Analyse the diffs and historical style context:
   - Determine if the changes represent a single logical unit of work or if they should be split into multiple commits.
   - For each logical commit group:
     - Identify the files that belong to this group.
     - Derive type, scope, subject, body, and footers strictly according to `docs/COMMIT_CONVENTION.md`, using recent history only for tone.

3. Execute the commits sequentially. For each logical group, run the `git add` command for the specific files, followed by the `git commit` command:
   ```powershell
   git add <file1> <file2> ...
   git commit -m "<type>(<scope>): <subject>" -m "- <bullet 1>" -m "- <bullet 2>"
   ```
