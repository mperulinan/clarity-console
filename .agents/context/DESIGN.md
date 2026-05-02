---
name: Portfolio
description: A personal crypto portfolio tracker with tax computation.
colors:
  primary: "#FF0054"
  bg-base: "#131722"
  bg-surface: "#1e2236"
  text-primary: "#E8ECF4"
  text-muted: "#8B93A8"
  semantic-gain: "#F2C14E"
  semantic-loss: "#9B8EFD"
typography:
  headline:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "2.5rem"
    fontWeight: 300
    lineHeight: 1.1
    letterSpacing: "-0.02em"
  title:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 500
    lineHeight: 1.3
  body:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    letterSpacing: "0.05em"
rounded:
  sm: "4px"
  md: "8px"
spacing:
  sm: "8px"
  md: "16px"
  lg: "24px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "8px 16px"
  button-primary-hover:
    backgroundColor: "#d90047"
---

# Design System: Portfolio

## 1. Overview

**Creative North Star: "The Clarity Console"**

This system is built to provide an instant and clear view of the portfolio to aid in decision-making. It operates in a precise, calm, serious, and warm register. It is designed for personal use and interview demonstrations, avoiding generic SaaS templates and AI clichés. The interface relies on a distinctive TradingView-inspired dark navy ground, utilizing hot crimson exclusively for primary actions. Crucially, financial gains are represented in yellow-gold and losses in soft purple, breaking from the traditional green/red to establish a personal, highly readable aesthetic.

**Key Characteristics:**
- **Terminal Warmth & TradingView Navy**: A specific `#131722` dark navy background, avoiding pure black or standard grays.
- **Extreme Restraint**: The primary accent (`#FF0054`) is used sparingly (≤10% of the surface) to highlight only the most critical actions.
- **Distinctive Semantics**: Gains are gold (`#F2C14E`), losses are soft purple (`#9B8EFD`).
- **Data as Material**: Strong typographic hierarchy with Inter, relying on size and weight instead of borders and cards to group information.

## 2. Colors

A Restrained strategy rooted in a custom navy dark mode.

### Primary
- **Hot Crimson** (#FF0054): Used ONLY for the active sidebar navigation item and the primary Call to Action (e.g., "New Transaction"). Never used for decoration or text.

### Neutral
- **Navy Dark** (#131722): The foundational background color.
- **Navy Surface** (#1e2236): Slightly lighter panels to create subtle depth without borders.
- **Off-White Text** (#E8ECF4): High-contrast text for critical values and headings.
- **Muted Slate** (#8B93A8): Low-contrast text for secondary labels and table headers.

### Semantic
- **Gold Gain** (#F2C14E): Used exclusively for positive financial values and upward trends.
- **Purple Loss** (#9B8EFD): Used exclusively for negative financial values and downward trends.

**The One Voice Rule.** The primary accent (#FF0054) is used on ≤10% of any given screen. Its rarity is the point.

## 3. Typography

**Display Font:** Inter (with system-ui)
**Body Font:** Inter (with system-ui)

**Character:** Precise, technical, and highly legible. Relies on strong scale and weight contrast to create hierarchy without clutter.

### Hierarchy
- **Headline** (300, 2.5rem, 1.1): Used for the massive, quiet numbers in the hero summary (e.g., Portfolio Value).
- **Title** (500, 1.25rem, 1.3): Used for section headers and prominent component titles.
- **Body** (400, 0.875rem, 1.5): Used for the standard data table rows and general text. Cap line length at 65–75ch for prose.
- **Label** (600, 0.75rem, 0.05em, uppercase): Used for table column headers and secondary stat labels. Small, tight, and highly structured.

**The Data Dominance Rule.** Typography, spacing, and color exist to make financial data legible. Aesthetics serve readability, not the reverse.

## 4. Elevation

Surfaces are flat by default. Depth is established through subtle tonal shifts between the Navy Dark background and Navy Surface panels.

### Shadow Vocabulary
- **Lifted Interaction** (`box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2)`): Used exclusively when an interactive element (like a button or a table row) is hovered or active.

**The Flat-By-Default Rule.** Surfaces are flat at rest. Shadows appear only as a response to state (hover, elevation, focus).

## 5. Components

Components are refined and restrained, remaining near-invisible until interacted with.

### Buttons
- **Shape:** Gently curved edges (8px radius).
- **Primary:** Hot Crimson (#FF0054) with 8px 16px padding.
- **Hover / Focus:** Lifts slightly with the Lifted Interaction shadow and darkens slightly in color.

### Cards / Containers
- **Corner Style:** 16px radius for the bento summary cells, 8px for standard panels.
- **Background:** Navy Surface (#1e2236).
- **Shadow Strategy:** None at rest.
- **Border:** None or a very subtle 1px border (`rgba(255, 255, 255, 0.05)`).

### Inputs / Fields
- **Style:** Navy Surface background, no visible border at rest, 8px radius.
- **Focus:** A subtle Hot Crimson bottom border or left edge indicator.

### Data Tables
- **Style:** Full width, dense. Minimal horizontal row separators (`rgba(255, 255, 255, 0.05)`).
- **Hover:** Row background subtly shifts lighter. No card-like wrappers around individual rows.

## 6. Do's and Don'ts

### Do:
- **Do** use Yellow (#FAFF70) and Purple (#9B8EFD) for all financial gain/loss indicators.
- **Do** rely on typographic size and weight to establish hierarchy instead of borders and boxes.
- **Do** use asymmetry and purposeful negative space to create visual interest.
- **Do** use subtle micro-animations that enhance usability and reward attention.

### Don't:
- **Don't** use generic AI UI slop: glowing purple/indigo/cyan neon gradients as default "futuristic energy".
- **Don't** use heavy glassmorphism with strong uniform backdrop blur and frosted translucent cards everywhere.
- **Don't** use overused dashboard clichés like big pulsing glowing metric numbers with dramatic bloom and animated rings.
- **Don't** use rainbow/chrome gradient text on headings.
- **Don't** use identical card grids (same-sized cards with icon + heading + text, repeated endlessly).
- **Don't** use side-stripe borders greater than 1px as a colored accent on cards.
