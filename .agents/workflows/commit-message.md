---
description: Generate a professional commit message from staged git changes
---

# Commit Message Workflow

## Rules

1. **Single focus**: only inspect the staged changes (`git diff --cached`) — never look at unstaged files or the working tree.
2. **Conventional Commits format**: every message MUST follow the structure:
   ```
   <type>(<scope>): <short summary>

   [optional body]

   [optional footer(s)]
   ```
3. **Allowed types** (choose the most specific one):
   | Type | When to use |
   |------|-------------|
   | `feat` | A new feature visible to the user or consumer |
   | `fix` | A bug fix |
   | `refactor` | Code change that is neither a fix nor a feature |
   | `style` | Formatting, whitespace, missing semi-colons — no logic change |
   | `perf` | A change that improves performance |
   | `test` | Adding or correcting tests |
   | `docs` | Documentation only changes |
   | `build` | Changes to build system or external dependencies |
   | `ci` | CI/CD configuration changes |
   | `chore` | Maintenance tasks that don't affect src or tests |
   | `revert` | Reverts a previous commit |

4. **Scope**: derive the scope from the primary module, feature area, or file path changed (e.g. `dashboard`, `auth`, `api`). Omit if the change is truly cross-cutting.
5. **Short summary** (imperative mood):
   - Use the imperative, present tense: *add*, *fix*, *remove* — not *added*, *fixes*, *removed*.
   - Maximum **72 characters** for the entire first line.
   - Do **not** end with a period.
   - **Do NOT capitalize the first letter**. Keep the entire summary lowercase to match Angular ecosystem standards (e.g., `feat(ui): add cool feature`).
6. **Body** (optional but recommended for non-trivial changes):
   - Wrap at **72 characters** per line.
   - Explain *what* and *why*, not *how*.
   - Use a bulleted list for specific changes, following the pattern: `- Verb 'CodeReference' or explanation`. Ensure the first letter of the verb is capitalized. (e.g. `- Replace 'MyComponent' with new grid system`).
   - Separate from the subject with a blank line.
7. **Footer** (optional):
   - Reference issues: `Closes #123`, `Fixes #456`.
   - Mark breaking changes: `BREAKING CHANGE: <description>`.
8. **Apostrophe rule**: use the straight single quote `'` consistently for contractions, possessives, and quoting code components inside the message body. Never use curly quotes.
9. **Output**: always deliver the final commit message inside a fenced code block with no extra commentary unless the user asks a question.

## Steps

// turbo-all
1. Retrieve the project's historical commit style by analyzing the last 10 commits:
   ```powershell
   git log -n 10 --oneline
   ```

2. Retrieve the staged diff:
   ```powershell
   git diff --cached
   ```

3. Analyse the diff and the historical style context:
   - Identify changed files and their feature area → derive **scope**.
   - Determine the nature of the change → pick **type**.
   - Summarise the intent in one imperative sentence → write **subject**. Ensure it is **lowercase**.
   - Note any context worth explaining → draft optional **body** using the `- Verb 'Reference'` format seen in recent project history.
   - Check for issue references or breaking changes → add optional **footer**.

4. Compose the commit message strictly following the rules and present it in a single code block.
