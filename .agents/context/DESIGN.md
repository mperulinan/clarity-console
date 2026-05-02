---
name: Portfolio
description: A personal crypto portfolio tracker with tax computation.
colors:
  bg-base: "#131722"
  bg-surface: "#1e2236"
  primary: "#FF0054"
  text: "#E8ECF4"
  text-muted: "#8B93A8"
  gain: "#FAFF70"
  loss: "#9B8EFD"
  border: "rgba(255, 255, 255, 0.06)"
typography:
  display:
    fontFamily: "'Manrope', system-ui, sans-serif"
    fontSize: "2.5rem"
    fontWeight: 300
    lineHeight: 1.1
  title:
    fontFamily: "'Manrope', system-ui, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 500
    lineHeight: 1.3
  body:
    fontFamily: "'Manrope', system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
  label:
    fontFamily: "'Work Sans', system-ui, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
rounded:
  md: "8px"
  lg: "16px"
  xl: "20px"
spacing:
  xs: "8px"
  sm: "16px"
  md: "24px"
  lg: "28px"
components:
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.primary}"
    rounded: "{rounded.md}"
    padding: "7px 17px"
  card-bento:
    backgroundColor: "{colors.bg-surface}"
    rounded: "{rounded.xl}"
    padding: "24px 28px"
---

# Design System: Portfolio

## 1. Overview

**Creative North Star: "The Clarity Console"**

This system is built to provide an instant and clear view of the portfolio to aid in decision-making. It operates in a precise, calm, serious, and warm register. It is designed for personal use and interview demonstrations, avoiding generic SaaS templates and AI clichés. The interface relies on a distinctive terminal-native dark ground, with support for warm theme variations. It utilizes a ghosted Hot Crimson for primary actions. Crucially, financial gains are represented in yellow-gold and losses in soft purple, breaking from the traditional green/red to establish a personal, highly readable aesthetic.

**Key Characteristics:**
- **Terminal Warmth**: A deep Midnight Crimson dark background (`#171216`), avoiding pure black or standard grays to create a warmer, bespoke feel.
- **Extreme Restraint**: The primary accent (`#FF0054`) is used sparingly (≤10% of the surface) to highlight only the most critical actions, often as a ghost button to reduce visual noise.
- **Distinctive Semantics**: Gains are gold (`#FAFF70`), losses are soft purple (`#9B8EFD`).
- **Data as Material**: Strong typographic hierarchy with Manrope for structural text and Work Sans (tabular nums) for financial data.
- **Proportional Weight**: Data visualization (like allocation bars) scales opacity gradually within a neutral color family rather than introducing new bright colors for large values.

## 2. Colors

A Restrained strategy rooted in a custom Midnight Crimson dark mode.

### Primary
- **Hot Crimson** (#FF0054): The primary interaction color. Used for the active sidebar navigation item, Ghost CTAs (e.g., "New Transaction"), and tiny localized sparks of warmth (like the holdings count badge).

### Neutral
- **Base Background** (#171216): The foundational Midnight Crimson background color.
- **Surface** (#221C20): Slightly lighter panels to create subtle depth without borders.
- **Off-White Text** (#E8ECF4): High-contrast text for critical values and headings.
- **Muted Slate** (#8B93A8): Low-contrast text for secondary labels and table headers.
- **Subtle Border** (rgba(255, 235, 245, 0.05)): Used for hairlines and table row dividers, tinted slightly toward the Midnight Crimson hue.

### Semantic
- **Gold Gain** (#FAFF70): Used exclusively for positive financial values and upward trends.
- **Purple Loss** (#9B8EFD): Used exclusively for negative financial values and downward trends.

**The One Voice Rule.** The primary accent (#FF0054) is used on ≤10% of any given screen. Its rarity is the point.

## 3. Typography

**Display Font:** Manrope (with system-ui)
**Body Font:** Manrope (with system-ui)
**Label/Mono Font:** Work Sans (with system-ui)

**Character:** Precise, technical, and highly legible. Manrope provides a clean, modern structural feel, while Work Sans offers crisp tabular numbers for financial data alignment.

### Hierarchy
- **Display** (300, 2.5rem, 1.1): Used for the massive, quiet numbers in the hero summary (e.g., Portfolio Value).
- **Title** (500, 1.25rem, 1.3): Used for section headers and prominent component titles.
- **Body** (400, 0.875rem, 1.5): Used for the standard data table rows and general text. Cap line length at 65–75ch for prose.
- **Label** (600, 0.75rem, tabular-nums): Used for table column headers, secondary stat labels, and all financial figures. Small, tight, and highly structured.

**The Data Dominance Rule.** Typography, spacing, and color exist to make financial data legible. Aesthetics serve readability, not the reverse.

## 4. Elevation

Surfaces are flat by default. Depth is established through subtle tonal shifts between the base background and surface panels.

### Shadow Vocabulary
- **Lifted Interaction** (`box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25)`): Used exclusively when an interactive element (like a button) is hovered or active.

**The Flat-By-Default Rule.** Surfaces are flat at rest. Shadows appear only as a response to state (hover, elevation, focus).

## 5. Components

Components are refined and restrained, remaining near-invisible until interacted with.

### Buttons
- **Shape:** Gently curved edges (8px radius).
- **Primary Ghost:** Transparent background with 1px Hot Crimson border (`color-mix` tinted) and Hot Crimson text.
- **Hover / Focus:** Fills slightly with a 5% Crimson tint, border solidifies to full primary color, and lifts slightly with the Lifted Interaction shadow.

### Cards / Containers
- **Corner Style:** 20px radius for the bento summary grid, 8px for standard panels.
- **Background:** Surface color (`var(--color-bg-surface)`).
- **Shadow Strategy:** None at rest.
- **Border:** None or a very subtle hairline border (`var(--color-border)`).
- **Internal Padding:** 24px 28px for primary cells, 16px 22px for secondary stat cells.

### Inputs / Fields
- **Style:** Surface background, no visible border at rest, 8px radius.
- **Focus:** A subtle Hot Crimson bottom border or left edge indicator.

### Data Tables
- **Style:** Full width, dense. Minimal horizontal row separators (`var(--color-border)`).
- **Hover:** Row background subtly shifts lighter. No card-like wrappers around individual rows.

### Allocation Bars
- **Style:** Smooth monochromatic neutral scale. Faint neutral (<15%), Solid neutral (15-39%), and Strongest neutral (>40%).
- **Rule:** Uses volume/opacity to encode size, completely avoiding semantic gain/loss colors or primary interaction colors to prevent false emphasis.

## 6. Do's and Don'ts

### Do:
- **Do** use Yellow (#FAFF70) and Purple (#9B8EFD) for all financial gain/loss indicators.
- **Do** rely on typographic size and weight to establish hierarchy instead of borders and boxes.
- **Do** use asymmetry and purposeful negative space to create visual interest.
- **Do** use subtle micro-animations that enhance usability and reward attention.
- **Do** use gradual opacity scales of neutral colors for data visualizations representing volume or weight.

### Don't:
- **Don't** use generic AI UI slop: glowing purple/indigo/cyan neon gradients as default "futuristic energy".
- **Don't** use heavy glassmorphism with strong uniform backdrop blur and frosted translucent cards everywhere.
- **Don't** use overused dashboard clichés like big pulsing glowing metric numbers with dramatic bloom and animated rings.
- **Don't** use rainbow/chrome gradient text on headings.
- **Don't** use identical card grids (same-sized cards with icon + heading + text, repeated endlessly).
- **Don't** use side-stripe borders greater than 1px as a colored accent on cards.
- **Don't** use semantic profit/loss colors or primary action colors for pure volume data visualizations.
