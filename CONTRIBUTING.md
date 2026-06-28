# Contributing to Placey

This document covers the conventions expected before submitting a pull request: branching, commits, code style, and CI checks.

---

## Branches

Branch off `main` for all work. Use the following prefixes:

| Prefix      | When to use                                                   |
| ----------- | ------------------------------------------------------------- |
| `feat/`     | New feature or user-facing behaviour                          |
| `fix/`      | Bug fix                                                       |
| `chore/`    | Tooling, config, dependencies, CI — no production code change |
| `docs/`     | Documentation only                                            |
| `refactor/` | Code restructure with no behaviour change                     |
| `test/`     | Adding or fixing tests                                        |

**Format:** `<prefix>/<short-kebab-description>`

```
feat/listing-image-upload
fix/booking-date-overlap
chore/update-dependencies
docs/api-auth-flow
```

Rules:
- Branch from an up-to-date `main`: `git fetch && git checkout -b feat/my-thing origin/main`
- One concern per branch
- Delete the branch after it is merged

---

## Commit messages

Follow [Conventional Commits](https://www.conventionalcommits.org/).

```
<type>(<optional scope>): <short description>

<optional body>

<optional footer — e.g. "Closes #42">
```

**Types** (same set as branch prefixes): `feat`, `fix`, `chore`, `docs`, `refactor`, `test`

**Scopes** (optional, use when it helps narrow the change): `fe`, `be`, `auth`, `bookings`, `listings`, `messages`, `reviews`

```
feat(listings): add image upload to listing form
fix(be): return 404 when listing is soft-deleted
chore(fe): update pnpm to 10.11.0
docs: document SSE messaging design
refactor(auth): extract token refresh logic into service
test(bookings): cover date-conflict edge cases
```

Rules:
- Description is lowercase, imperative mood ("add", not "added" or "adds"), no trailing period
- Keep the subject line under 72 characters
- Use the body to explain *why*, not *what* (the diff shows what)
- Reference issues in the footer: `Closes #12` or `Refs #34`

---

## Pull requests

### Before opening a PR

1. Make sure CI passes locally (see [CI checks](#ci-checks) below)
2. Rebase onto `main` if your branch has fallen behind
3. Keep the PR focused

### Title

Same format as a commit message subject line:

```
feat(listings): add image upload to listing form
fix(be): return 404 when listing is soft-deleted
```

### Description

If not trivial, include at a minimum:
- **What** changed (and why)
- Related issue numbers (`Closes #12`)

### Review

- At least one approval is officialy required before merging
- Address all comments before merging 
  
---

## Branch protection (`main`)

`main` is protected. GitHub enforces the following on every PR targeting it:

| Rule                                     | Setting                                                  |
| ---------------------------------------- | -------------------------------------------------------- |
| Pull request required                    | Direct pushes to `main` are blocked                      |
| Required approvals                       | 1 (with admin bypass)                                    |
| Dismiss stale reviews on new commits     | Yes                                                      |
| Status checks must pass                  | Both CI jobs must be green (see [CI checks](#ci-checks)) |
| Branch must be up to date before merging | Yes                                                      |
| Force pushes                             | Blocked                                                  |
| Branch deletion                          | Blocked                                                  |

**Merge strategy:** squash or rebase only — merge commits are disabled to keep history linear.  

---

## CI checks

Two GitHub Actions workflows run on every PR. Both must be green before merging.

### Frontend (`fe-ci.yml`)


| Step       | Command              | What it checks                 |
| ---------- | -------------------- | ------------------------------ |
| Format     | `pnpm ci:format`     | Prettier (formatting)          |
| Lint       | `pnpm ci:lint`       | oxlint + ESLint (linting)      |
| Type-check | `pnpm ci:type-check` | vue-tsc (no TypeScript errors) |
| Test       | `pnpm ci:test`       | Vitest (unit tests pass)       |

**Fix locally before pushing:**

```bash
cd placey-fe
pnpm format      # auto-fix formatting
pnpm lint        # auto-fix lint (where possible)
pnpm ci:type-check
pnpm ci:test
```

### Backend (`be-ci.yml`)

| Step   | Command                    | What it checks         |
| ------ | -------------------------- | ---------------------- |
| Format | `dotnet csharpier check .` | CSharpier (formatting) |
| Build  | `dotnet build`             | No compilation errors  |
| Test   | `dotnet test`              | Unit tests pass        |

**Fix locally before pushing:**

```bash
# from repo root
cd placey-be && dotnet tool run csharpier format . && cd ..   # auto-fix formatting
dotnet build placey.sln
dotnet test placey.sln
```

### Tooling overview

| Tool                                               | Layer    | Purpose                                    |
| -------------------------------------------------- | -------- | ------------------------------------------ |
| [Prettier](https://prettier.io)                    | Frontend | Opinionated code formatter (JS/TS/Vue/CSS) |
| [oxlint](https://oxc.rs/docs/guide/usage/linter)   | Frontend | Fast linter for correctness rules          |
| [ESLint](https://eslint.org)                       | Frontend | Vue-specific and TypeScript rules          |
| [vue-tsc](https://github.com/vuejs/language-tools) | Frontend | TypeScript type-checking for `.vue` files  |
| [CSharpier](https://csharpier.com)                 | Backend  | Opinionated C# code formatter              |

---

## Code style

### Frontend

Configured in [placey-fe/.prettierrc.json](placey-fe/.prettierrc.json) and [placey-fe/.editorconfig](placey-fe/.editorconfig). Key settings:

- Single quotes, no semicolons, 100-character line width
- 2-space indent, LF line endings
- Component names must be multi-word (ESLint `vue/multi-word-component-names`)

### Backend

Configured in [placey-be/.editorconfig](placey-be/.editorconfig). CSharpier handles all formatting automatically, just run `dotnet csharpier format .` before committing.
