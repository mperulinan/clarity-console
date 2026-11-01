# Commit message convention

Format: Conventional Commits

```text
<type>(<scope>): <short summary>

[optional body]

[optional footer(s)]
```

## Allowed types

| Type | When to use |
|------|-------------|
| `feat` | A new feature visible to the user or consumer |
| `fix` | A bug fix |
| `refactor` | Code change that is neither a fix nor a feature |
| `style` | Formatting, whitespace, missing semi-colons — no logic change |
| `perf` | A change that improves performance |
| `test` | Adding or correcting tests |
| `docs` | Documentation only changes |
| `build` | Changes to build system or project configuration |
| `ci` | CI/CD configuration changes (e.g. `.github/workflows`) |
| `chore` | Maintenance tasks that do not affect production code or tests |
| `revert` | Reverts a previous commit |

## Scopes

Scopes must be strictly lowercase. Prefer deriving the scope from the primary architectural layer or path:

| Primary Path | Scope |
|--------------|-------|
| `src/Portfolio.Domain` & `tests/Portfolio.Domain.Tests` | `domain` |
| `src/Portfolio.Application` & `tests/Portfolio.Application.Tests` | `application` |
| `src/Portfolio.API` & `tests/Portfolio.API.Tests` | `api` |
| `src/Portfolio.Infrastructure` & `tests/Portfolio.Infrastructure.Tests` | `infrastructure` |
| `src/Portfolio.Client` (including client unit tests) | `client` |
| Dependency / lockfile changes only | `deps` |
| `.github/workflows` | `ci` |

Omit the scope only when the change is truly cross-cutting or applies to repository-level tooling/docs where no specific layer applies (e.g. `docs: ...` or `chore: ...`).

## Subject rules

- Imperative, present tense: `add`, `fix`, `remove` — not `added`, `fixes`, `removed`.
- Entire first line ≤ 72 characters.
- Do not capitalize the first letter of the summary (Angular-style lowercase, e.g. `feat(api): add import endpoint`).
- Do not end with a period.

## Body rules

- Optional, but preferred for non-trivial changes.
- Wrap at 72 characters per line.
- Explain *what* and *why*, not *how*.
- Separate from the subject with a blank line.
- Bulleted list pattern: `- Verb 'CodeReference' or explanation` (capitalized verb, straight single quotes only).
- Straight quote rule: use the straight single quote `'` consistently for contractions, possessives, and quoting code components inside the message body. Never use typographic or curly quotes (`’`, `‘`).

## Footers (optional)

- Reference issues: `Closes #123`, `Fixes #456`.
- Mark breaking changes: `BREAKING CHANGE: <description>` (or append `!` to the type/scope, e.g. `feat(api)!: drop deprecated endpoints`).

## Examples

```text
test(application): add unit tests for import command handlers
- Add test suites for Preview and Commit import CQRS handlers

feat(infrastructure): implement degiro transaction csv parser
- Add CsvHelper package dependency to Infrastructure
- Implement ITransactionImportParser and DegiroTransactionParser for DEGIRO CSV files
- Add unit tests for DegiroTransactionParser

ci(client): enable angular unit tests in ci pipeline
- Run ng test with ChromeHeadless in GitHub Actions workflow

docs: add commit message convention
- Add docs/COMMIT_CONVENTION.md as canonical format for types, scopes, and structure
```
