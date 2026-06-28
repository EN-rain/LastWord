# Last Word — UI/UX Design System

Design intelligence pulled from **UI/UX Pro Max** skill (`ui-ux-pro-max-skill`).

## Searches used

```bash
python3 src/ui-ux-pro-max/scripts/search.py "horror dark UI" --domain color -n 5
python3 src/ui-ux-pro-max/scripts/search.py "horror dark atmospheric" --domain style -n 3
python3 src/ui-ux-pro-max/scripts/search.py "dark mode" --domain color -n 5
python3 src/ui-ux-pro-max/scripts/search.py "HUD" --domain style -n 3
python3 src/ui-ux-pro-max/scripts/search.py "dark atmospheric" --domain typography -n 3
```

## Design direction

- **Style:** cinematic dark / atmospheric horror HUD.
- **Mood:** tense, low-light, gothic-tech.
- **Palette:** deep void background, warm orange/gold accents for primary actions, red for danger/destructive, muted blue-gray for secondary info.

## Color tokens

| Token | Hex | Godot Color | Usage |
|---|---|---|---|
| Background Deep | `#0A0A0F` | `Color(0.039, 0.039, 0.059, 1)` | Full-screen UI background |
| Card / Panel | `#12121A` | `Color(0.071, 0.071, 0.102, 1)` | Panels, buttons, cards |
| Border | `#2A2A3A` | `Color(0.165, 0.165, 0.227, 1)` | Separators, outlines |
| Foreground | `#E0E0E0` | `Color(0.878, 0.878, 0.878, 1)` | Primary text |
| Muted | `#8A8F98` | `Color(0.541, 0.561, 0.596, 1)` | Secondary / disabled text |
| Accent (gold/orange) | `#D97706` | `Color(0.851, 0.467, 0.024, 1)` | Primary buttons, active phase, progress fill |
| Danger / Destructive | `#EF4444` | `Color(0.937, 0.286, 0.286, 1)` | Warnings, focus outline, token-holder danger |

## Theme resource

`Assets/UI/LastWordTheme.tres`

Applied globally via `project.godot`:

```ini
[gui]
theme/custom="res://Assets/UI/LastWordTheme.tres"
```

Also explicitly attached to:
- `Scenes/MainMenu.tscn`
- `Scenes/PauseMenu.tscn`
- `Scenes/HUD.tscn`
- `Scenes/SettingsMenu.tscn`

## Typography

- **Headings / Title:** large sans-serif, accent gold (`LAST WORD` title).
- **Body / UI labels:** default system sans at 14–16 px.
- **Recommended premium font stack** (from skill): `Space Grotesk` headings + `Inter` body + `JetBrains Mono` data/monospace.
  - These are not bundled; they can be added later as `.ttf`/`.otf` FontFile resources.

## Scene-specific changes

### MainMenu
- Added full-screen dark background `ColorRect`.
- Centered title, increased font size to 56, accent gold color.
- Buttons styled by theme (dark card, orange hover, orange pressed).

### PauseMenu
- Theme applied; panel uses `StyleBoxFlat_panel`.
- Translucent black overlay kept.

### HUD
- Theme applied.
- Progress bars (volume meter) use orange fill.
- Status labels retain functional colors (yellow state, red token holder).

### SettingsMenu
- Theme applied to full-screen settings panel.
- Sidebar and content panels use theme panel style.
- Sliders, line edits, buttons inherit theme styles.

## Accessibility notes

- Avoid pure #000000 backgrounds (theme uses near-black `#0A0A0F`).
- Accent `#D97706` on background passes large-text AA.
- For small body text, ensure 4.5:1 contrast; use `#E0E0E0` on `#12121A`.
