# Design System Strategy: The Architectural Editorial

## 1. Overview & Creative North Star
The Creative North Star for this design system is **"The Architectural Editorial."** 

This system moves beyond the "app-like" grid, treating the interface as a high-end publication where information is curated, not just displayed. It blends the structural authority of a technical dashboard with the sophisticated whitespace of a premium editorial. We break the "template" look by utilizing heavy tonal blocks (deep navy), intentional asymmetry in card nesting, and a hierarchy that favors bold, expressive typography over structural lines.

## 2. Colors
Our palette is rooted in deep structural tones, using high-contrast accents to guide the eye without cluttering the visual field.

### Primary & Accent Palette
- **Primary (`#031632`):** The foundation. Use for high-impact surfaces and primary backgrounds to create immediate depth.
- **Primary Container (`#1a2b48`):** Use for internal brand elements and secondary hero sections.
- **Secondary (`#1b6d24`):** Specifically for "Active" or "Success" states. It should feel organic and vibrant against the navy foundation.
- **Tertiary/Yellow (`#f8bd2a`):** A spotlight color. Use sparingly for high-priority labels or alerts.

### The "No-Line" Rule
**Borders are strictly prohibited for sectioning.** 1px solid lines create visual noise and signify a lack of confidence in the layout. Instead, define boundaries through:
- **Tonal Shifts:** Transitioning from `surface` (`#f8f9fa`) to `surface-container-low` (`#f3f4f5`).
- **Negative Space:** Using the Spacing Scale (specifically `spacing-8` or `spacing-12`) to allow sections to breathe independently.

### Surface Hierarchy & Nesting
Treat the UI as a series of stacked architectural layers. 
- Place a `surface-container-lowest` (`#ffffff`) card on a `surface-container` (`#edeeef`) background to create a soft, natural lift.
- **Glassmorphism:** For floating modals or navigation, use `surface` at 80% opacity with a `24px` backdrop-blur. This ensures the UI feels integrated into the environment.

## 3. Typography
We use a dual-font strategy to balance technical precision with editorial elegance.

*   **Display & Headlines (Manrope):** Chosen for its geometric modernism. High-tracking (letter-spacing) on smaller headlines adds an authoritative, premium feel.
*   **Body & Labels (Inter):** A workhorse for readability. Use `body-md` for standard descriptions to maintain a clean, "uncluttered" look.

### Hierarchy
- **Display-LG (3.5rem):** Used for "Hero" technical headers.
- **Headline-SM (1.5rem):** The standard for card titles.
- **Label-MD (0.75rem / Uppercase):** Use for category tags (e.g., "API" or "DATABASE") with `0.05em` letter-spacing to mimic architectural labeling.

## 4. Elevation & Depth
Depth is achieved through **Tonal Layering**, not shadows.

- **The Layering Principle:** A card should never sit on a background of the same color. If the background is `surface-container-low`, the card must be `surface-container-lowest`. 
- **Ambient Shadows:** When a card must float (e.g., a hover state), use a shadow tinted with the `on-surface` color (`#191c1d`) at 6% opacity with a 32px blur. Avoid pure black shadows; they look "dirty" on our clean off-white background.
- **The "Ghost Border" Fallback:** If a boundary is visually required for accessibility, use `outline-variant` at 15% opacity. It should be felt, not seen.

## 5. Components

### Buttons
- **Primary:** `primary` background with `on-primary` (white) text. Roundedness: `xl` (1.5rem).
- **Secondary/Status:** Use `secondary_container` for a softer look, or `primary_container` for technical actions. 
- **Interaction:** On hover, shift the background color by one tier (e.g., `primary` to `primary_container`).

### Cards & Lists
- **The "No Divider" Rule:** Never use a horizontal line to separate list items. Use `spacing-4` vertical padding and subtle background shifts (alternating between `surface` and `surface-container-low`).
- **Card Corners:** Apply `rounded-xl` (1.5rem) to main container cards and `rounded-lg` (1rem) to nested internal cards to create a "nested radius" effect.

### Chips & Tags
- **Status Chips:** High-saturation backgrounds (`secondary` or `tertiary_fixed_dim`) with `0.5rem` (sm) rounding. These should feel like physical "pills" dropped onto the layout.
- **Action Chips:** Use `primary_container` with a semi-transparent opacity (40%) to create a "ghost" effect that doesn't compete with primary CTAs.

### Input Fields
- **Styling:** Use `surface-container-lowest` for the fill. No border. Instead, use a 2px `primary` bottom-border that only appears on `:focus` to signify architectural intent.

## 6. Do's and Don'ts

### Do:
- **Use Intentional Asymmetry:** Let one side of a layout be a solid navy block (`primary`) and the other a clean off-white (`surface`) to create a signature "split-screen" editorial look.
- **Embrace Large Spacing:** Use `spacing-16` or `20` between major sections. High-end design requires "breathing room."
- **Nesting:** Always nest a lighter surface inside a darker one (or vice versa) to define hierarchy.

### Don't:
- **Don't use 1px Borders:** This is the quickest way to make a premium system look like a generic template.
- **Don't use pure Black (#000):** Use `primary` or `on-surface` for all "dark" elements to keep the palette sophisticated and tonal.
- **Don't crowd the cards:** If information feels cramped, increase the card size rather than decreasing the font size. Information must feel "curated," not "stuffed."