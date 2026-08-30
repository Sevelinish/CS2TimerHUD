# CS2 Run Timer HUD Plugin

![CS2](https://img.shields.io/badge/Game-Counter--Strike_2-orange?style=flat-square&logo=counter-strike)
![Status](https://img.shields.io/badge/Status-Active-success?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)

> A lightweight and convenient Counter-Strike 2 plugin that puts a run timer under the crosshair and hands its control to a single key. Perfect for surf and bhop practice, recording demos, or comparing your own attempts.

---

## Description
This plugin creates a visual timer on the screen, controlled by one button of the player's choice. Every press moves to the next action, so a run is started, paused and reset without ever opening the chat. It features multi-language support and saves the bound button per Steam ID.

The timer does not read the keyboard. It reads the button input the server already receives, so it stays correct no matter how the player has bound their keys. Time is counted in server ticks, so the value always matches the tick rate the server actually runs at.

## What It Shows

```
   00:12.45
  TMP 00:09.87
```

| Element | Meaning |
| :--- | :--- |
| Main timer | The current run |
| Grey | Idle, waiting for the first press |
| Green | Running |
| Yellow | Paused |
| `TMP` line | The value kept from the last overwrite, replaced by the next one |

Every press of the bound button moves one step along the cycle:

| Press | Action |
| :--- | :--- |
| 1st | **Start** - the timer runs |
| 2nd | **Pause** - the timer stops at the current value |
| 3rd | **Overwrite** - the value moves to the `TMP` line, the main timer returns to zero |

If a player clicks through the cycle by accident, the value already on the `TMP` line survives - a run shorter than `minimum_record_seconds` is never recorded over it.

The timer is parented to the player's view model, so it stays under the crosshair at any angle.

---

## Positioning

The timer places itself according to whether the key display of [MovementHUD](../../../MovementHUD) is on screen for that player:

| MovementHUD | Timer position |
| :--- | :--- |
| Off | Takes its place below the crosshair, at full size |
| On | Moves above the key display and shrinks, so it takes less room |

The layout switches live. The moment a player toggles the key display, the timer moves within a second, and nothing is recreated - the existing text is repositioned. The geometry of the block above is read from the MovementHUD config and, per player, from its preferences file.

---

## Usage

All commands should be typed into the in-game text chat. Every command also works from the console with the `css_` prefix, for example `css_timerhud_bind reload`, and from a key bind, for example `bind "x" "say !timerhud"`.

### Main Commands
| Command | Description |
| :--- | :--- |
| `!timerhud` | Toggles the plugin's HUD on or off (True/False) |
| `!timerhud_bind <button>` | Binds the button that starts, pauses and overwrites the timer. Example: `!timerhud_bind reload` |
| `!timerhud_bind` | Shows the current button and the full list of supported names |
| `!timerhud_bind none` | Removes the bind |

Every setting is saved per Steam ID and survives reconnects and map changes.

### Supported Buttons
| Button | Aliases |
| :--- | :--- |
| `use` | `e` |
| `reload` | `r` |
| `inspect` | `f`, `lookatweapon` |
| `scoreboard` | `tab`, `score` |
| `attack` `attack2` `attack3` | `mouse1`, `mouse2`, `mouse3` |
| `jump` `duck` `speed` | `space`, `ctrl`, `shift` |
| `forward` `back` `moveleft` `moveright` | `w`, `s`, `a`, `d` |

Also available: `zoom`, `walk`, `turnleft`, `turnright`, `grenade1`, `grenade2`, `weapon1`, `weapon2`, `alt1`, `alt2`, `run`, `bullrush`, `cancel`.

### Language Settings
Changes the plugin's display language to the selected one. Available options:

| Command | Language |
| :--- | :--- |
| `!timerhud_engl` | 🇬🇧 English (**Default**) |
| `!timerhud_rus` | 🇷🇺 Russian |
| `!timerhud_ukr` | 🇺🇦 Ukrainian |
| `!timerhud_spanish` | 🇪🇸 Spanish |
| `!timerhud_deutsch` | 🇩🇪 German |

The digits and the `TMP` label stay in Latin script in every language - the world text font renders them the same everywhere.

### Admin Commands
| Command | Permission | Description |
| :--- | :--- | :--- |
| `css_timerhud_debug` | `@css/root` | Prints plugin state: active sessions, bound buttons, timer states, the detected MovementHUD layout and the paths it reads |

---

## Configuration

The config file is created on first launch at
`addons/counterstrikesharp/configs/plugins/TimerHUD/TimerHUD.json`.

| Key | Default | Description |
| :--- | :--- | :--- |
| `enabled_by_default` | `true` | Show the timer to players who have not changed the setting |
| `default_language` | `engl` | `engl` / `rus` / `deutsch` / `ukr` / `spanish` |
| `default_button` | `""` | Button given to players who have not bound one yet |
| `chat_triggers` | `!/` | Characters that start a chat command |
| `default_render_mode` | `worldtext` | `worldtext` or `centerhtml` |
| `update_interval` | `2` | Ticks between HUD updates, `2` is about 32 refreshes per second |
| `time_precision` | `2` | Digits after the seconds, `0` to `3` |
| `minimum_record_seconds` | `0.15` | Shorter runs are never written to the `TMP` line |
| `reset_on_map_change` | `true` | Clear the timer and the temporary value when the map changes |
| `announce_actions` | `false` | Also print every start, pause and overwrite to chat |
| `theme` | - | Colors, font, sizes and the `TMP` label |
| `layout` | - | HUD geometry: distance, offsets, spacing and the solo and stacked scales |
| `movement_hud` | - | Detection mode, paths and the assumed MovementHUD geometry |

A reference config with every default value is included in [docs/TimerHUD.example.json](docs/TimerHUD.example.json).

---

## Installation

1. Download the latest release from the [Releases](../../releases) tab.
2. Extract the archive and move the files into the `game/csgo/addons/...` directory on your server.
3. Restart the server or reload the plugin.

**Requirements:** Metamod:Source and CounterStrikeSharp installed on the server.

## Screenshots
<img width="839" height="165" alt="image" src="https://github.com/user-attachments/assets/ee20b255-b77f-441d-a96b-b86f3105ae78" />

---
**Developed for the CS2PRAK Launcher.**
