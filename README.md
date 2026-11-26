# Tether

A time-travel cinematic platformer for COMP 4478 Project 2 (Group 3).

*You create a better world without you.*

Wren, a scavenger in the ruined city of Halden (2190), finds the Tether, a broken
pocket device that Shifts the world between two moments of the same place. Wren
travels back through 2041 to 1889 to stop the disaster called the Bloom, and ends
up stranded in the past for good.

## Setup (about 5 minutes)

1. Install **Unity 6 (6000.0 LTS or newer)** from Unity Hub. Unity 2022.3 LTS also works.
2. In Unity Hub, create a **new project** with the **Universal 2D** template
   (or **2D (Built-In Render Pipeline)**).
3. Copy the `Assets/Tether` and `Assets/Resources` folders from this package into
   the new project's `Assets` folder.
4. Go back to Unity and wait for it to import. It creates `Assets/Tether/Tether.unity`,
   adds it to Build Settings and opens it. If it does not open, use the menu
   **Tether > Open Game Scene**.
5. Press **Play**.

On Unity 2022, install **Input System** from the Package Manager for full controller
support. Without it, the game falls back to the old Input Manager.

To build: **File > Build Settings** (or **Build Profiles** in Unity 6). The scene is
already in the list. Pick Windows or Mac and press **Build**.

## Controls

| Action | Keyboard | Controller |
| --- | --- | --- |
| Move | A / D or arrow keys | Left stick or D-pad |
| Jump (hold for higher) | Space | A |
| Shift time | Left or right Shift, or K | X, LB or RB |
| Interact | E | Y |
| Climb ladders | W / S | Stick or D-pad up/down |
| Duck, crawl, slide | S (while running = slide) | Down |
| Pause and settings | Esc or P | Start |
| Menus | Arrows, Enter/Space, Esc, mouse | D-pad, A, B |

On-screen prompts switch between keyboard and controller names based on the
last device used.

## Course requirements

| Requirement | Where it is |
| --- | --- |
| Controller support | `GInput.cs`: every action works on a gamepad, and menus are fully navigable with one |
| Background music and action sounds | `Synth.cs` and `AudioDir.cs`: a music track per era (crossfades on every Shift), chase music, and about 20 action sounds |
| Change display, resolution and input device at any time | Settings on the title screen and in the pause menu (Esc or Start, usable even during cutscenes): monitor, resolution, window mode, input device (Keyboard + Controller, Keyboard only, Controller only), music and SFX volume. Saved between sessions. |
| Original work | Story, levels, code, music and sound are original to this project. Character sprites are Kenney's CC0 Platformer Characters, credited in-game. |
| Cinematic focus | Letterbox cutscenes, camera pans, chapter cards, colour grading per era, story told through on-screen text and posters that change between eras |

## Chapters

1. **Ashfall** (2190): movement tutorial, Ivy's camp, the street of Hollows, climbing the Vale tower to find the Tether.
2. **Two Cities** (2190 and 2041): learn to Shift. A bridge, a flood wall, a balcony, a mid-air scaffold climb, and a street with Hollows in one era and guards in the other.
3. **The Lantern** (2041 and 1889): stealth past guards, climb the 1889 mine shaft, read Ines's notes, cross the core hall as it pulses between eras.
4. **Deep Time** (1889 and 2041): meet Otto, Shifts now cost Tether charges, gas tunnels, falling beams, a low crawlspace, and the crystal vein.
5. **The Last Shift** (1889): the Tether breaks, kick out the support, and outrun the cave-in.
6. **Epilogue**: Halden in 2190, saved, followed by credits.

Every chapter can be started from **Chapters** on the title screen.

## Code map (`Assets/Tether/Scripts`)

| File | What it does |
| --- | --- |
| `Game.cs` | Starts everything from code, title menu, chapter flow, pause, settings menu |
| `Levels.cs` | The level layouts, story text and cutscene scripts. Edit this to change levels. |
| `LevelPlan.cs` | Small API for describing a level: `Ground`, `Block`, `Ladder`, `Hollow`, `Guard`, `Text`, `Cue` and more |
| `LevelRuntime.cs` | Builds a level, runs the Shift, checkpoints, respawn, cues, core pulses and the chase |
| `Player.cs` | Wren's movement: run, jump, climb, duck, slide |
| `Actors.cs` | Hollows, guards, spikes, crumbling floors, gas, lamps, beams, NPCs, the cave-in |
| `Presentation.cs` | Camera (follow, pans, shake) and the parallax backdrop with ambient particles |
| `UI.cs` | HUD, captions, dialogue boxes, cards, letterbox, fades, menus |
| `Art.cs` | Loads the Kenney sprites and draws tiles, skylines and props in code |
| `Synth.cs`, `AudioDir.cs` | Generates the music and sound effects, and plays them |
| `GInput.cs`, `Settings.cs` | Keyboard and controller input, saved options |

Levels use a grid where 1 unit is 1 tile, x goes right and y goes up. `EM.A` and
`EM.B` put something in only one of the level's two eras; `EM.Both` in both.

## Credits

- Characters: [Kenney Platformer Characters](https://kenney.nl) (CC0). See `Assets/Resources/Characters/Kenney_License.txt`.
- Everything else: COMP 4478 Group 3.
