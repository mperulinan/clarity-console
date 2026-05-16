---
description: Automatically analyze, group, stage, and commit all Git changes.
---

# Smart Commit Workflow

## Rules

1. **Analyze All Changes**: Inspect both staged and unstaged changes (`git status`, `git diff --cached`, `git diff`).
2. **Logical Grouping**: Determine if the changes should be split into multiple logical commits based on feature areas, types of changes, or specific files.
3. **Conventional Commits format**: every message MUST follow the structure:
   ```
   <type>(<scope>): <short summary>

   [optional body]

   [optional footer(s)]
   ```
4. **Allowed types** (choose the most specific one):
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

5. **Scope**: derive the scope from the primary module, feature area, or file path changed (e.g. `dashboard`, `auth`, `api`). Omit if the change is truly cross-cutting.
6. **Short summary** (imperative mood):
   - Use the imperative, present tense: *add*, *fix*, *remove* — not *added*, *fixes*, *removed*.
   - Maximum **72 characters** for the entire first line.
   - Do **not** end with a period.
   - **Do NOT capitalize the first letter**. Keep the entire summary lowercase to match Angular ecosystem standards (e.g., `feat(ui): add cool feature`).
7. **Body** (optional but recommended for non-trivial changes):
   - Wrap at **72 characters** per line.
   - Explain *what* and *why*, not *how*.
   - Use a bulleted list for specific changes, following the pattern: `- Verb 'CodeReference' or explanation`. Ensure the first letter of the verb is capitalized. (e.g. `- Replace 'MyComponent' with new grid system`).
   - Separate from the subject with a blank line.
8. **Apostrophe rule**: use the straight single quote `'` consistently for contractions, possessives, and quoting code components inside the message body. Never use curly quotes.
9. **Execution over Output**: Do not just present the commit messages. You MUST automatically execute the `git add` and `git commit` commands for each logical group sequentially. Do NOT wait for user approval before running the commands (the IDE will handle the terminal execution approval).

## Steps

// turbo-all
1. Retrieve historical context, current status, and all changes (staged and unstaged) in one go:
   ```powershell
   git log -n 20 --oneline; git status; git diff --cached; git diff
   ```

2. Analyse the diffs and historical style context:
   - Determine if the changes represent a single logical unit of work or if they should be split into multiple commits.
   - For each logical commit group:
     - Identify the files that belong to this group.
     - Derive the **scope**.
     - Pick the **type**.
     - Write the **subject** (lowercase, imperative).
     - Draft the optional **body**.

3. Execute the commits sequentially. For each logical group, run the `git add` command for the specific files, followed by the `git commit` command. To handle multiline commit messages in PowerShell and avoid string parsing errors, you MUST use a separate `-m` flag for the subject and **each individual paragraph or bullet point** in the body. Do not use newline characters (`\n` or literal newlines) inside a single `-m` string.
   ```powershell
   git add <file1> <file2> ...
   git commit -m "<type>(<scope>): <subject>" -m "- <bullet 1>" -m "- <bullet 2>"
   ```
