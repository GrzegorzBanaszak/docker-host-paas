---
name: Blueprint Precision
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#45464d'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#76777d'
  outline-variant: '#c6c6cd'
  surface-tint: '#565e74'
  primary: '#000000'
  on-primary: '#ffffff'
  primary-container: '#131b2e'
  on-primary-container: '#7c839b'
  inverse-primary: '#bec6e0'
  secondary: '#006591'
  on-secondary: '#ffffff'
  secondary-container: '#39b8fd'
  on-secondary-container: '#004666'
  tertiary: '#000000'
  on-tertiary: '#ffffff'
  tertiary-container: '#002113'
  on-tertiary-container: '#009668'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dae2fd'
  primary-fixed-dim: '#bec6e0'
  on-primary-fixed: '#131b2e'
  on-primary-fixed-variant: '#3f465c'
  secondary-fixed: '#c9e6ff'
  secondary-fixed-dim: '#89ceff'
  on-secondary-fixed: '#001e2f'
  on-secondary-fixed-variant: '#004c6e'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.01em
  body-main:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-caps:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.05em
  code-block:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 4px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 32px
  grid-margin: 24px
  grid-gutter: 16px
---

## Brand & Style

The design system is engineered for high-density technical environments where clarity and utility are paramount. It targets DevOps engineers and system architects who require immediate, glanceable insights into complex containerized ecosystems. 

The aesthetic is a hybrid of **Minimalism** and **Corporate/Modern**, infused with subtle **Tactile** "blueprint" elements. It avoids the soft, rounded playfulness of modern B2B SaaS in favor of a "Control Room" ethos—precise, sharp, and authoritative. The UI should evoke a sense of organized power, making the user feel like they are operating a high-performance machine rather than browsing a marketing site.

## Colors

This design system utilizes a structured, high-contrast palette designed for functional status signaling. 

- **Ink/Navy (#0F172A):** Used for primary headings, sidebars, and high-emphasis UI elements to ground the interface.
- **Sky (#0EA5E9):** The primary action and focus color, representing "active" or "selected" states.
- **Mint (#10B981):** Reserved strictly for healthy states, successful builds, and "running" containers.
- **Rose/Coral (#E11D48 / #FB7185):** A dual-tone system for errors and warnings. Use Rose for critical failures and Coral for non-blocking alerts.
- **Slate (#64748B):** Used for secondary text, borders, and inactive interface elements.

The background must remain a clean, light Slate-tinted white (#F8FAFC) to ensure maximum contrast for technical data.

## Typography

The system uses **Inter** for all UI elements to ensure legibility at small sizes within dense dashboards. A strict hierarchy is maintained through weight and letter spacing rather than excessive size shifts.

- **Headings:** Use Semibold or Bold weights with slight negative letter-spacing for a compact, engineered look.
- **Labels:** Small caps with increased letter spacing are used for table headers and section metadata.
- **Monospace:** **JetBrains Mono** is mandatory for all code snippets, container IDs, terminal logs, and environment variables. It must be rendered with subpixel antialiasing to ensure maximum clarity on light backgrounds.

## Layout & Spacing

The design system employs a **12-column fluid grid** system optimized for large-format displays. The spacing rhythm is based on a **4px baseline**, ensuring all components align to a mathematical grid.

- **Blueprint Texture:** The main background features a subtle 16px grid pattern in #E2E8F0 at 50% opacity, reinforcing the "technical drawing" aesthetic.
- **Density:** The layout favors "Compact" density. Gutters are kept tight (16px) to maximize the amount of information visible without scrolling.
- **Margins:** Large views utilize a 24px outer margin, while internal panel padding defaults to 16px.

## Elevation & Depth

Depth is achieved through **Tonal Layering** and **Low-Contrast Outlines** rather than traditional drop shadows.

- **Flat Surfaces:** Most panels sit directly on the grid. Depth is suggested by a 1px solid border in Slate-200 (#E2E8F0).
- **Active Elevation:** Only floating menus or active modals use a shadow. The shadow should be highly diffused: `0px 4px 12px rgba(15, 23, 42, 0.08)`.
- **Gradients:** Subtle linear gradients (e.g., from #FFFFFF to #F1F5F9) may be used on panel headers to give them a "machined" feel.
- **Blueprint Overlay:** Use a very faint radial gradient on the main dashboard background to draw the eye toward the center of the viewport.

## Shapes

The design system uses a **Soft (0.25rem)** roundedness profile to maintain a professional, tool-like appearance. 

- **Standard Elements:** Buttons, input fields, and status badges use a 4px (0.25rem) radius.
- **Large Containers:** Dashboard cards and side-panels may use up to 8px (0.5rem) to provide a structural container feel.
- **Interactive States:** Avoid "Pill" shapes entirely. All interactive elements must feel structural and rectangular to maintain the engineering aesthetic.

## Components

### Buttons & Inputs
- **Primary Action:** Solid Navy (#0F172A) with white text. Sharp 4px corners.
- **Secondary Action:** Ghost style with a 1px Slate-300 border.
- **Inputs:** High-contrast 1px borders. Focused state uses a 1px Sky-500 ring with no glow.

### Status Badges
Badges are critical for this system. They should use a "Signal Light" style: a subtle background tint with a high-contrast 1px border and a small circular "LED" indicator icon.
- **Running:** Mint-100 background, Mint-600 text, Mint-500 indicator.
- **Failed:** Rose-100 background, Rose-700 text, Rose-600 indicator.

### Data Visualization
- **Line Charts:** Use "Stepped" lines to represent discrete system changes rather than smooth curves.
- **Bar Charts:** No rounded tops; use clean, sharp rectangles.
- **Usage Gauges:** Simple linear progress bars; avoid circular "donut" charts to save horizontal space.

### Log Viewer
A dedicated component with a dark "console" option (the only dark exception). It features JetBrains Mono text, line numbering, and syntax highlighting for JSON/YAML. It should include a "pin to bottom" toggle and a search/filter bar integrated into the header.