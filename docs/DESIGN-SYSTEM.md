# FoundationKit Design System — Soft Orbit v1

## Status

Soft Orbit is the first-party visual/UI baseline for FoundationKit v1. It is shared by Core Studio, Workbench reference screens, and Composer/OpenAPI-generated Blazor applications.

This is a FoundationKit identity. It does **not** copy the JAIB logo, JAIB red palette, wallet-specific UI, or any proprietary brand asset. The reference material informed the design-system method: one written system, semantic tokens, real interactive components, platform-aware layouts, and a living visual guide.

## Source-of-truth order

When implementation and documentation disagree, resolve drift in this order:

```text
1. FoundationKit.Blazor semantic tokens / component contracts
2. this design-system specification
3. Core Studio /design living reference
4. generated-app examples / screenshots
```

The living guide renders real reusable components. It must not maintain a visually similar fork of the components.

## Character

Soft Orbit should feel:

- light and calm;
- modern and technical without cyberpunk/neon styling;
- creative through small interactions rather than decoration;
- friendly without becoming childish;
- precise, composable and product-ready;
- Arabic/RTL-ready and equally coherent in English/LTR.

It should not feel like:

- a black/glow-heavy developer dashboard;
- a generic Bootstrap/Material admin template;
- gaming UI;
- luxury black-and-gold banking;
- excessive glassmorphism, gradients or shadows;
- a collection of unrelated colorful cards.

## Color architecture

Brand color communicates importance, not ownership of every surface.

Approximate visual balance:

```text
~78% neutral canvas/surfaces/whitespace
~14% Iris primary emphasis
~5% Aqua active/positive secondary emphasis
~3% warm/semantic contextual accents
```

Canonical light tokens:

```text
Primary          #665CE8
Primary Hover    #584FD4
Primary Pressed  #4A43C0
Primary Soft     #EEECFF
Aqua             #22B8A7
Aqua Soft        #E4F8F5
Warm Accent      #FFAD66
Warm Soft        #FFF3E7

Canvas           #F7F8FC
Surface          #FFFFFF
Surface Muted    #F1F3F8
Text Primary     #252731
Text Secondary   #6F7482
Text Muted       #9A9EAA
Border           #E7E9F0
Border Strong    #D9DCE6
```

Canonical dark surface hierarchy:

```text
Canvas           #11131A
Surface          #181B24
Surface Muted    #20232D
Surface Elevated #252934
Text Primary     #F4F5F8
Text Secondary   #B3B7C4
Text Muted       #7E8392
Border           #2A2E3A
Border Strong    #363B49
```

Status colors are semantic and independent of the brand:

```text
Success  #21A179 (light baseline)
Warning  #D99019
Danger   #D94B55
Info     #4387D8
```

Dark mode uses lighter semantic variants declared in `foundationkit.css`.

## Typography

Preferred stacks:

```text
Arabic: IBM Plex Sans Arabic -> Noto Sans Arabic -> Segoe UI -> system fallbacks
Latin:  Inter -> Segoe UI -> system UI fallbacks
Mono:   IBM Plex Mono -> platform monospace fallbacks
```

FoundationKit does not bundle proprietary font files. A host may provide approved webfont assets; the system remains usable with fallbacks.

Weight should remain restrained. Most functional UI uses 400–600; 700 is reserved for strong headings/key values.

## Spacing and geometry

Base spacing unit: `4px`.

Canonical scale:

```text
4, 8, 12, 16, 20, 24, 32, 40, 48, 64, 80
```

Canonical radius intent:

```text
8px   small compact surfaces
12px  controls/buttons/inputs
16px  cards
20px  large panels
24px  drawers/dialogs
28px  hero surfaces
pill  tags/badges only
```

Do not apply one large radius to every element.

## Elevation

Hierarchy should primarily come from canvas/surface/border differences. Shadows remain low-strength.

- default cards: border + near-zero shadow;
- interactive cards: slight border change + `translateY(-2px)` + small shadow;
- dialogs/drawers: medium shadow;
- no large ambient glow around normal business UI.

## Motion

Canonical duration scale:

```text
Fast      120ms
Standard  180ms
Medium    240ms
Slow      320ms
```

Canonical easing:

```css
cubic-bezier(.2, 0, 0, 1)
```

Rules:

- motion must communicate state/affordance/progress;
- button hover may translate by `-1px`;
- interactive card hover may translate by `-2px`;
- pressed controls may scale to `.985`;
- tabs/navigation indicators should move rather than blink where practical;
- `prefers-reduced-motion` must collapse non-essential animation/transition durations.

## Orbit Nodes

Orbit Nodes are the FoundationKit-specific lightweight visual motif. They represent composition and flow between concepts such as:

```text
Project -> Module -> Resource -> API -> Read Model -> Client -> UI
```

Use them for:

- loading/generation states;
- empty states;
- Composer visualization;
- onboarding/hero illustration;
- lightweight marketing/reference artwork.

Do not use 3D financial objects or wallet-specific art direction as the FoundationKit visual signature.

## Reusable component boundary

`FoundationKit.Blazor` owns first-party product-neutral primitives. The initial v1 baseline includes:

- `FkBrandMark` — temporary replaceable FoundationKit mark;
- `FkButton` — primary/secondary/ghost/danger + size/loading/disabled/link behavior;
- `FkCard` — default/muted/elevated/interactive surfaces;
- `FkBadge` — neutral/primary/aqua/status tones;
- `FkPageHeader` — consistent hierarchy/actions;
- `FkEmptyState` and `FkLoadingState` — Orbit Node system states;
- `FkThemeToggle` — persistent light/dark preference;
- `FkAppShell` and `FkNavItem` — responsive navigation/application frame.

Reusable Core does not depend on MudBlazor. Workbench may use MudBlazor for additional sample controls, but visible Workbench styling must inherit FoundationKit semantic tokens.

## Generated applications

Generated Blazor applications must not ship an independent visual identity. The canonical generator emits:

```text
_content/FoundationKit.Blazor/foundationkit.css
_content/FoundationKit.Blazor/foundationkit.js
FoundationKit.Blazor.Components
FkAppShell / first-party primitives
```

Product-specific CSS may define layout/content details and may override semantic tokens at the host boundary. It should not fork/copy the FoundationKit component stylesheet.

Allowed product-level branding override examples:

```css
:root {
    --fk-color-primary: <product primary>;
    --fk-color-primary-hover: <derived product hover>;
    --fk-color-primary-pressed: <derived product pressed>;
    --fk-color-primary-soft: <product soft tint>;
}
```

A product may also replace the brand mark/logo and application name. Changing product branding must not require editing shared component source.

## RTL / LTR

RTL support is structural, not `text-align:right`.

- use logical CSS (`padding-inline`, `border-inline-*`, etc.);
- navigation/sidebar direction must reverse correctly;
- directional icons/chevrons must follow layout direction;
- IDs, URLs, code, GUIDs and similar technical values may remain isolated LTR inside RTL UI;
- generated apps may choose their initial direction while retaining the same component set.

## Responsive baseline

FoundationKit uses real layout changes:

```text
Desktop: full sidebar + topbar + 12-column content foundation
Tablet:  compact sidebar + reduced content density
Mobile:  bottom navigation shell + compact topbar + single-column critical flows
```

Desktop is not an enlarged mobile screen and mobile is not a squeezed desktop table.

## Functional UI rules

- labels are explicit; placeholders are not the only label;
- validation explains the problem and does not rely on color alone;
- minimum interactive target is 44px where practical;
- focus state is visible;
- tables use soft sticky headers, subtle separators and row hover;
- filters should prefer a compact search/filter/saved-view toolbar over many always-visible fields;
- complex edits should prefer pages/drawers over nested dialogs;
- empty states explain the next useful action;
- loading states avoid layout jumps where skeletons are appropriate.

## Security / architecture boundary

The design system is presentation only.

- UI visibility is not authorization;
- server authorization remains mandatory;
- browser filters do not replace server query policy;
- multi-table/report screens consume API read models / SQL views rather than reproducing joins in the browser;
- secrets are never design tokens/static assets;
- arbitrary manifest HTML/JavaScript is not rendered as trusted UI.

## Maintenance rule

When the visual system changes:

```text
1. update reusable tokens/components
2. update this specification
3. update Core Studio /design reference
4. update generator proof/assertions if the public UI contract changed
5. regenerate/verify generated-app evidence
```

This keeps one visual DNA across Core Studio, examples and future generated projects.
