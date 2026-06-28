# Frontend - Tooling & Configuration

---

## Package manager

**pnpm** (`^10.11.0`). Node version requirement: `^20.19.0 || >=22.12.0`.

---

## npm scripts

| Script          | Command                                         | Description                                          |
| --------------- | ----------------------------------------------- | ---------------------------------------------------- |
| `dev`           | `vite`                                          | Start dev server with HMR at `http://localhost:5173` |
| `build`         | `vue-tsc --build && vite build`                 | Type-check then bundle for production                |
| `build-only`    | `vite build`                                    | Bundle without type-check                            |
| `preview`       | `vite preview`                                  | Preview the production build locally                 |
| `type-check`    | `vue-tsc --build --force`                       | TypeScript validation only                           |
| `lint`          | `pnpm lint:oxlint && pnpm lint:eslint`          | Run all linters with auto-fix                        |
| `lint:oxlint`   | `oxlint --fix`                                  | Fast Rust-based linter (Oxlint)                      |
| `lint:eslint`   | `eslint --fix --cache`                          | ESLint with Vue + TS plugins                         |
| `format`        | `prettier --write src/`                         | Format all source files                              |
| `test`          | `vitest run`                                    | Run tests once                                       |
| `test:watch`    | `vitest`                                        | Run tests in watch mode                              |
| `test:coverage` | `vitest run --coverage`                         | Run with V8 coverage report                          |
| `ci:lint`       | `pnpm lint:oxlint && pnpm lint:eslint --no-fix` | Lint without auto-fix (CI)                           |
| `ci:format`     | `prettier --check src/`                         | Format check without writing (CI)                    |
| `ci:type-check` | `vue-tsc --build --force --noEmit`              | Type-check without emitting (CI)                     |

---

## Vite (`vite.config.ts`)

```ts
plugins: [
  vue(),                        // Vue 3 SFC support
  tailwindcss(),                // Tailwind CSS v4 via @tailwindcss/vite
  vueDevTools(),                // Vue DevTools integration
]

resolve.alias: { '@': './src' } // Path alias used throughout source

server.proxy: {
  '/api': 'http://localhost:5001'  // Forward all /api/* to the backend
}

scrollBehavior: () => ({ top: 0 }) // Reset scroll on every navigation
```

---

## TypeScript

Three tsconfig files:

| File                 | Purpose                                                                                                                     |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `tsconfig.json`      | Root — references the two below                                                                                             |
| `tsconfig.app.json`  | App source (`src/**`). Extends `@vue/tsconfig/tsconfig.dom.json`. Sets `@/*` path alias. Enables `noUncheckedIndexedAccess` |
| `tsconfig.node.json` | Build tooling files (`vite.config.ts`, etc.)                                                                                |

TypeScript version: **6.0.0**.

---

## ESLint (`eslint.config.ts`)

Plugins active:

| Plugin                  | Purpose                                        |
| ----------------------- | ---------------------------------------------- |
| `eslint-plugin-vue`     | Vue 3 specific rules                           |
| `@typescript-eslint`    | TypeScript-aware rules                         |
| `eslint-plugin-oxlint`  | Disables ESLint rules that overlap with Oxlint |
| `eslint-plugin-unicorn` | Code quality rules                             |

Prettier integration: `eslint-config-prettier` disables formatting rules — Prettier handles formatting exclusively.

---

## Oxlint (`.oxlintrc.json`)

Fast Rust-based linter that runs first. Configured with plugins: `eslint`, `typescript`, `unicorn`, `oxc`, `vue`. Correctness violations are treated as errors.

Oxlint runs alongside ESLint in the `lint` script. The `eslint-plugin-oxlint` plugin disables any ESLint rules that Oxlint already covers to avoid duplicate reports.

---

## Prettier (`.prettierrc.json`)

```json
{
  "semi": false,
  "singleQuote": true,
  "printWidth": 100
}
```

---

## Tailwind CSS v4

Tailwind is integrated via `@tailwindcss/vite` (no `tailwind.config.js` needed). Theme tokens are defined in `src/assets/main.css` using the `@theme` directive:

```css
@theme {
  --color-cream: #f7f2ec;
  --color-ink: #1a1a1a;
  --color-muted: #717171;
  --color-divider: #dddddd;
  --color-dark: #222222;

  --font-family-sans: 'Inter', sans-serif;
  --font-family-serif: 'Cormorant Garamond', serif;
}
```

Custom utility: `scrollbar-hide` (hides scrollbar while preserving scroll functionality).

---

## CI checks

The frontend CI workflow (`.github/workflows/fe-ci.yml`) runs in order:

1. `pnpm ci:format` — Prettier format check
2. `pnpm ci:lint` — Oxlint + ESLint (no auto-fix)
3. `pnpm ci:type-check` — vue-tsc type check
4. *(Tests are run in the backend CI; frontend CI focuses on static analysis)*

All checks must pass before a PR can be merged.

---

## Production build & nginx

```bash
cd placey-fe
pnpm build   # outputs to dist/
```

In Docker, the frontend is served by nginx using `nginx.conf`:

- Serves static assets from `dist/`.
- Routes all non-asset requests to `index.html` (SPA fallback) so client-side routes work on direct access and browser refresh.
- Proxies `/api/` to the backend container.
