# Frontend - Overview

## Stack

| Layer                | Technology                                 |
| -------------------- | ------------------------------------------ |
| Framework            | Vue 3 (Composition API + `<script setup>`) |
| State management     | Pinia                                      |
| Routing              | Vue Router 4                               |
| Styling              | Tailwind CSS v4 (Vite plugin)              |
| Icons                | `@lucide/vue`                              |
| Build tool           | Vite                                       |
| Package manager      | pnpm                                       |
| Serving (production) | nginx (inside Docker)                      |

---

## Project structure

```
placey-fe/
├── public/
├── src/
│   ├── assets/
│   │   └── main.css                  Tailwind CSS entry + Placey theme tokens
│   ├── components/                   Reusable UI components (15 files)
│   ├── composables/                  Shared Composition API utilities (3 files)
│   ├── router/
│   │   └── index.ts                  Route definitions + auth guard
│   ├── stores/                       Pinia stores (6 stores + barrel export)
│   ├── types/
│   │   └── index.ts                  TypeScript interfaces matching API response shapes
│   ├── views/                        Route-level page components (13 views)
│   ├── App.vue                       Root component — SSE lifecycle, NavBar, Footer
│   └── main.ts                       App entry point
├── nginx.conf                        Production nginx config (SPA routing + API proxy)
├── vite.config.ts                    Vite build config + dev proxy
├── vitest.config.ts                  Test runner config
├── tsconfig.app.json                 TypeScript config for app source
├── eslint.config.ts                  ESLint + Oxlint rules
├── .prettierrc.json                  Prettier formatting config
└── package.json                      Dependencies and scripts
```

---

## Design theme

Tailwind v4 uses `@theme` for brand tokens defined in `src/assets/main.css`:

| Token             | Value     | Use                    |
| ----------------- | --------- | ---------------------- |
| `--color-cream`   | `#f7f2ec` | Page background        |
| `--color-ink`     | `#1a1a1a` | Primary text           |
| `--color-muted`   | `#717171` | Secondary text, labels |
| `--color-divider` | `#dddddd` | Borders, separators    |
| `--color-dark`    | `#222222` | Headings, accents      |

**Fonts**: Cormorant Garamond (serif headings), Inter (body), Georgia, Luxurious Script.

---

## Further reading

| Topic                | Document                                       |
| -------------------- | ---------------------------------------------- |
| Routing & auth guard | [routing.md](./routing.md)                     |
| Pinia stores         | [state-management.md](./state-management.md)   |
| Components reference | [components.md](./components.md)               |
| Composables          | [composables.md](./composables.md)             |
| Views / pages        | [views.md](./views.md)                         |
| API communication    | [api-communication.md](./api-communication.md) |
| Testing              | [testing.md](./testing.md)                     |
| Tooling & config     | [tooling.md](./tooling.md)                     |
| Messaging feature    | [../messaging.md](../messaging.md)             |
