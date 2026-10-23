---
name: double-stick-project
description: Use when working on the "Double Stick" Unity project at E:\The Source File of Mine Game\Unity\Double Stick. This skill is the project's living progress/context for new sessions: player/camera/stick/enemy systems, Y-sorting, health/white-flash/hitstop, white-screen level restart, current tuning values, and the rule to avoid one-off editor setup scripts. Read it before changing gameplay.
---

# Double Stick Project — Context

## Project

- Unity **2022.3.62f3c1**, 2D, built-in render pipeline.
- Build Settings order: **Launcher (0)**, `SampleScene`, `ED`.
- `Assets/Scenes/Launcher.unity` is the game start scene. Its `LevelManager` GameObject holds the serialized level list (see "Level flow").
- `Assets/Scenes/SampleScene.unity` is the current level/test scene. `ED` is the ending scene.
- Art: `Assets/Assets/Textures/*`, tiles in `Assets/Assets/Tiles/*`.
- No git repo. The configured prefabs/scene are the source of truth.

## Hard rules

- **Do not create one-off `Assets/Editor/*Setup.cs` scripts, menu tools, or scaffolding.** If something only needs configuring once, do it directly in the Editor and save, or document the steps here. Runtime scripts are fine when the feature needs them at runtime.
- Keep the **floor Tilemap sorting order low** (currently 0). Player/Stick/Box/Enemy use `YSort2D` with `baseOrder = 10000`.
- The enemy detection `CircleCollider2D` must stay **Is Trigger = true**. If it is a physical collider it will push the enemy out of the map and can hit the player from far away.
- Enemy stick hits only count on the **non-trigger body collider**. The detection trigger is ignored by `EnemyController.OnStickHit`.
- The old setup tools (`DoubleStickSetup`, `StickSetup`, `GameplaySetup`) were deleted on purpose; do not recreate them.

## Current scene snapshot

- Tilemap bounds: **x -21..7, y -10..6** (289 painted tiles); map was extended to the west.
- Player at **(0.5, 0.5)**.
- Box prefab instances at approximately `(2.92, -0.90)`, `(-0.58, -1.80)`, `(-3.08, -0.04)`.
- Six Enemy prefab instances placed west of the player (x ≈ -15.2 .. -4.9). Walk left to test them.
- Main Camera:
  - `CameraFollow2D`: target Player, offset `(0,0,-10)`, `smoothTime = 0.02`.
  - `PixelPerfectCamera` (user-added): PPU **100**, reference **640×480**, upscale/snapping off. Camera is zoomed; tiles are PPU 32 and player 36, so pixel-perfect consistency is not guaranteed.
- `ScreenFade.prefab` lives in `Assets/Resources/`; `fadeDuration = 0.6`, `holdBeforeLoad = 0.05`.

## Systems

### Player / camera

- `PlayerMovement2D`: WASD/arrow legacy Input Manager; `moveSpeed = 4`, `acceleration = 40`; normalizes input; `Rigidbody2D` dynamic, gravity 0, freeze rotation, interpolation; Animator only plays while input is held, otherwise resets to frame 0 and `speed = 0`.
- The player's `Sprite` child `localScale.x` is set to `-1` when the mouse is left of the player, otherwise `1` (in `StickController.LateUpdate`). y/z are preserved.
- `CameraFollow2D` follows in `LateUpdate` with `Vector3.SmoothDamp`.
- `FrameRateSync` sets `Time.fixedDeltaTime` to the display refresh rate before the scene loads.
- `YSort2D` on Player, Box, Enemy: `sortingPrecision = 100`, `baseOrder = 10000`; lower Y = in front. Floor tilemap stays at a low order.

### Stick

- Hierarchy: Player → `Stick` (`Stick.prefab`), visual child `Sprite` scale 5; `StickController` is on the Player root; `StickHitbox` + trigger `BoxCollider2D` on the Stick (current size `1.59 × 0.73`, offset `(0.836, 0.015)`, user-tuned).
- Aims at the mouse; `maxAngularSpeed` limits how fast it turns (current **180°/s**, code default 720). First frame snaps; later frames clamp.
- Wall blocking: before committing the next rotation step, `WouldOverlapWall` tests a thin box built from the Stick collider's length/offset (**`wallCheckThickness = 0.1`**) using `Physics2D.OverlapBoxNonAlloc`. Triggers, the Player/stick hierarchy, and non-static bodies are ignored; static non-trigger colliders (tilemap walls + Box) block.
- If blocked, the stick keeps its previous angle (`IsBlocked = true`, `AngularSpeed = 0`, no afterimages). It only resumes when a rotation step (e.g. aiming back the opposite way) no longer overlaps a wall.
- Player movement is constrained by the same probe: `PlayerMovement2D.FixedUpdate` calls `StickController.WouldHitWallAt` on the predicted position. If the full step would push the stick into a wall, it cancels the blocked axis (X, then Y) so the player slides/stops instead of forcing the stick through the wall.
- If the stick tip is within **`wallContactDistance = 0.15`** of a static wall (`StickController.IsStuckAgainstWall`), movement toward the tip and sideways (perpendicular) is blocked. Only movement with a positive pull-away component (`Vector2.Dot(velocity, TipDirection) < 0`) is allowed; rotating the stick away also frees it.
- Afterimages: every `ghostAngleStep = 6°` above `ghostMinAngularSpeed` (current **180**, code default 120); lifetime 0.1 s, start alpha 0.6, pooled, max 6 per frame, order = player order + 1.
- `IsSwinging = activeGhosts > 0`; damage comes from `StickController.Damage` (current **10**, serialized in the Player prefab).
- Stick sorting order = player order + 2. `HitDetected` event remains for future use; enemies use `IStickHittable`.

### Enemy

- `EnemyController` + `Rigidbody2D` (dynamic, gravity 0, freeze rotation, interpolate) + `YSort2D` + Animator.
- Body `BoxCollider2D` is non-trigger; current size ≈ `0.262 × 0.80`, offset `(-0.018, 0.054)`, user-tuned. Sprite child pivot is bottom-left; death rotates the Sprite child 90°.
- Detection: `CircleCollider2D` **trigger**, radius **5**; checked with `Physics2D.OverlapCircleNonAlloc`. The radius is the chase range.
- **No pathfinding**: when the player is inside the detection radius, the enemy sets velocity straight at the player (`moveSpeed = 5`). No grid/A* is built or used, so enemies can be stopped by walls/Boxes by design. `GridPathfinder2D.cs` is currently unused.
- Idle: Animator paused at frame 0. Moving/chasing: Animator speed 1.
- Health: `maxHealth` current **50** (code default 30). Hit damage = stick `Damage` (10), so current = 5 hits. On a valid hit:
  - subtract health;
  - stagger `0.15 s` (stops AI/movement/animation; acts as hit cooldown);
  - white flash overlay for `0.2 s`;
  - `stab.wav` one-shot (`hitSound` on the Enemy prefab, 2D AudioSource created in `Awake`);
  - global hitstop `HitStop.Request(hitStopFrames)`; current `hitStopFrames = 30` (code default 2). 30 frames is very long; tune to 2-4 for a subtle hitstop.
- On 0 HP: disable body/collider/Animator, play `stomp.wav` (`deathSound`), rotate Sprite child 90°, keep the corpse; no more movement or player damage. The killing hit also flashes white and plays both stab + stomp.
- Player body collision → `LevelManager.RestartCurrentLevel()`.

### EnemyShooter / Bullet

- `EnemyShooter.prefab` = EnemyController (`moveSpeed = 0`, `maxHealth = 10`) + `EnemyShooter` + `Bullet` prefab wiring. It never moves; the stationary behavior comes from `moveSpeed = 0`.
- Detection uses the same `EnemyController.IsPlayerInDetectionRange()` (circle radius 5). While the player is inside and the shooter is not dead/frozen, it fires every **`fireInterval = 1.5s`** at the player's current position (no line-of-sight check).
- On each shot it shows the child `White` node for **4 frames**, hides it again, plays optional **`shootSound`** (2D AudioSource auto-created), and spawns `Bullet.prefab` at `firePoint` (the White transform). Before showing, the White's sorting layer/order is set to `max(shooter sprite order, bullet sprite order) + 1`; `YSort2D` pushes the enemy's order well above 10000 and the bullet sits at the muzzle, so either can hide a fixed-order flash.
- While the player is detected, the shooter flips the `Sprite` child's `localScale.x` toward the player and mirrors the muzzle/`firePoint` local X so the flash and bullet origin stay on the facing side.
- **Sprite pitfall:** duplicating the normal Enemy brings its `Enemy` AnimatorController, which sprite-swaps to `enemyNormal-Sheet`. The EnemyShooter prefab's Animator has **no controller** (`m_Controller: 0`) so it keeps `enemyShoot.png`. If you add an animation, build it from the shooter sprites.
- `Bullet.prefab`: kinematic `Rigidbody2D` + **trigger** `BoxCollider2D`, `Bullet.cs` (`speed = 8`, `lifetime = 4s`, sorting order 10002). It reacts to the **player's non-trigger body** → `LevelManager.RestartCurrentLevel()` (same as enemy touch) and destroys itself.
- On hitting a **wall/Box** (any non-trigger collider that is not the player or an enemy) the bullet stops, disables its collider, and plays a **`vanishDuration = 0.5s`** scale 1→0 **quad-in** (`1 - t²`) animation, then destroys itself.
- Bullets ignore the stick, enemies and other triggers; the player's only counter is dodging. The stick's hits/blocking never affect bullets.
- The shooter is a normal `EnemyController` target: the stick can kill it (10 HP = one hit) and it counts toward level completion.

### White flash (shader)

- `Assets/Resources/SpriteWhiteFlash.shader` (`Sprites/WhiteFlash`).
- On hit, `EnemyController` creates a runtime child `HitFlash` SpriteRenderer with this shader, same sprite, order = enemy sprite order + 1. It draws a white silhouette using the sprite's alpha and the overlay color's alpha; the base SpriteRenderer/material is untouched. Alpha fades 1 → 0 over `flashDuration`.
- If the flash is missing, verify the shader imported and the Enemy `Sprite` child exists.

### White-screen restart

- `LevelManager.RestartCurrentLevel()` loads `Resources/ScreenFade.prefab` if no fade exists, sets it `DontDestroyOnLoad`, shows a full-screen white Image (Canvas sorting 32767), reloads the active scene, then fades alpha to 0 over `fadeDuration`.
- The fade prefab is shared across levels; add any new level scenes to Build Settings.
- The guard prevents duplicate fades while one is active.

### Hitstop

- `HitStop` + `HitStopRunner`: sets `Time.timeScale = 0` and waits `frames` **rendered** frames via `WaitForEndOfFrame`, then restores the previous timeScale. Global: player, enemies, stick, and afterimages freeze.
- Requested from `EnemyController.OnStickHit` after a valid body hit.

### Level flow (Launcher)

- `LevelManager` is now a `MonoBehaviour` singleton on the `Launcher` scene's LevelManager GameObject; it is `DontDestroyOnLoad` and persists across levels.
- Launcher `Start` loads `levels[0]` directly (no fade). Each `LevelDefinition` has `sceneName` + `openingDialog` (`List<DialogLine>`).
- On each level `sceneLoaded`, it freezes player movement **and pauses the player Animator at frame 0**, disables the stick, and freezes all enemies; it subscribes to each `EnemyController.Died`.
- After the opening dialog completes (or immediately if there are no lines) it unfreezes actors and gameplay starts. `PlayerMovement2D.SetFrozen(bool)` owns the freeze/pause logic.
- Level music: `StartGameplay` starts `Double Stick.ogg` (looping, 2D `AudioSource` on the persistent LevelManager; `levelMusic` assigned in Launcher). `CompleteLevel` stops it before the black transition. Death-restart resumes without restarting the track (`isPlaying` guard).
- When the last enemy dies (`aliveEnemies == 0`) it freezes actors, stops the music, waits **`completionDelay = 2s`** (realtime), then plays the black transition to the next level; after the **final** level it transitions to `endingSceneName` (`ED`).
- The first level launches without fade. Reaching the next level/ED uses `ScreenFadeBlack` with `fadeIn 1s -> hold 3s -> load -> fadeOut 1s`.
- `EnemyController.SetFrozen(true/false)` suppresses AI/animator without disabling the component; `Died` is raised at the end of `Die()`.
- Player death still calls static `LevelManager.RestartCurrentLevel()` (white `ScreenFade`); it reloads the current level without replaying the opening dialog.
- The configured level list currently has one entry: `SampleScene` with a single placeholder dialog line.

### Dialog

- `DialogController` is attached to `Assets/Prefabs/UI/Dialog.prefab` (inactive `StandL`/`StandR` portrait Images, dialogue `Text (Legacy)`, "点击继续" hint).
- `Play(List<DialogLine>, onComplete)` shows each line with its `text`, `leftPortrait`, `rightPortrait`, and optional `typeSound`.
- Typewriter: `charactersPerSecond = 30` (realtime). Left click while typing instantly completes the line; left click after typing advances.
- Portraits are swapped/activated per line; `typeSound` plays a randomized-pitch one-shot **per non-whitespace character** and is silent when unassigned.
- When the last line finishes it invokes `onComplete` and destroys the dialog instance.
- `DialogController` auto-creates an `AudioSource` on the prefab root if none is serialized.

### DialogArea (area-triggered dialog)

- `Assets/Prefabs/UI/DialogArea.prefab` root has a **CircleCollider2D (Is Trigger)** + `DialogArea`. Its `dialog` list uses the same `DialogLine` fields as LevelManager (`text`, `leftPortrait`, `rightPortrait`, `typeSound`).
- `DialogArea` plays the configured lines the first time the player's non-trigger body enters the area in that scene instance (`hasPlayed`), then never again until the scene reloads (death restart re-arms it).
- It uses its own `dialogPrefab` (preassigned to `Dialog.prefab`), falling back to `LevelManager.DialogPrefab`.
- While it plays, `LevelManager.SetActorsFrozen(true)` freezes player movement/animation, the stick, and all enemies; it unfreezes when the last line is dismissed. Unlike the opening dialog it does **not** switch scenes or count enemies.
- `OnTriggerStay2D` retries while the player is inside, so an area overlapping the spawn still fires after the opening dialog ends.
- It skips if another `DialogController` is already active. The collider must stay a trigger (a non-trigger collider blocks the player instead).

### Screen vignette (global)

- Built-in render pipeline, so there is no URP Volume/post-processing. The vignette is a **runtime screen-space UI overlay**: `ScreenVignette` on the Launcher LevelManager GameObject generates a radial alpha `Texture2D`, puts it on a stretched `RawImage` in a `Canvas` with `sortingOrder = -1`, and `DontDestroyOnLoad`s it.
- Order matters: `-1` is above all world sprites but below the Dialog Canvas (`0`), so the vignette darkens the game and not the dialogue; ScreenFade canvases (32767) are above it.
- Tunables on the Launcher LevelManager: `color` (black), `intensity 0.55`, `innerRadius 0.45`, `outerRadius 1.25`, `sortingOrder -1`, `textureSize 256`.
- Persists across Launcher → levels → ED. If a level scene is played directly (without Launcher), add `ScreenVignette` to that scene to see it.

### Screen scanline (global)

- `Assets/Resources/ScreenScanline.shader` (`Hidden/DoubleStick/ScreenScanline`) draws screen-space horizontal dark lines using `_ScreenParams.y` and `_Time`; included in builds via Resources.
- `ScreenScanline` (also on the Launcher LevelManager GameObject) creates a full-screen `RawImage` Canvas with that material, `sortingOrder = -2` (above world, below the vignette at -1, dialog at 0, fades at 32767), and `DontDestroyOnLoad`s it.
- Tunables: `intensity 0.18`, `lineSpacing 3` px, `lineThickness 0.35` (fraction of spacing), `scrollSpeed 0` (px/s), `sortingOrder -2`.

### Screen RGB split (global)

- Built-in pipeline camera effect: `RGBSplitEffect` (`OnRenderImage` + `Assets/Resources/ScreenRGBSplit.shader`) samples R/B with a radial UV offset; green/alpha stay centered. UI (dialog, overlays) is rendered after the camera and is not affected.
- `ScreenRGBSplit` on the Launcher LevelManager holds the settings, subscribes to `SceneManager.sceneLoaded`, and adds/configures `RGBSplitEffect` on `Camera.main` for the Launcher, every level, and ED. Disabling or `amount = 0` removes the effect from the current camera.
- Tunable `amount` (UV offset, current **0.0025**, range 0–0.02); direct scene play without Launcher needs `ScreenRGBSplit` added to that scene.

## Key files

- `Assets/Scripts/PlayerMovement2D.cs` — WASD movement, idle/move animation.
- `Assets/Scripts/CameraFollow2D.cs` — smooth camera follow.
- `Assets/Scripts/FrameRateSync.cs` — fixed timestep = display refresh.
- `Assets/Scripts/YSort2D.cs` — Y-based sorting order.
- `Assets/Scripts/StickController.cs` — mouse aim, speed cap, afterimages, damage, player sprite flip.
- `Assets/Scripts/StickAfterimage.cs` — pooled afterimage fade.
- `Assets/Scripts/StickHitbox.cs` — forwards stick trigger overlaps to `NotifyHit`.
- `Assets/Scripts/IStickHittable.cs` — enemy hit interface.
- `Assets/Scripts/EnemyController.cs` — direct chase, health, stagger, flash, death, level restart.
- `Assets/Scripts/ScreenVignette.cs` — persistent runtime screen-space vignette overlay.
- `Assets/Scripts/ScreenScanline.cs` — persistent screen-space scanline overlay; shader at `Assets/Resources/ScreenScanline.shader`.
- `Assets/Scripts/ScreenRGBSplit.cs`, `RGBSplitEffect.cs` — global camera RGB split; shader at `Assets/Resources/ScreenRGBSplit.shader`.
- `Assets/Scripts/GridPathfinder2D.cs` — unused (A* removed; kept for reference).
- `Assets/Scripts/HitStop.cs`, `HitStopRunner.cs` — global frame freeze.
- `Assets/Scripts/LevelManager.cs` — level list, launches levels, dialog gating, last-enemy detection, black transitions, static white restart.
- `Assets/Scripts/DialogController.cs` — typewriter dialog, portraits, per-line typing sound.
- `Assets/Scripts/DialogArea.cs` — first-entry trigger dialog for a level area.
- `Assets/Scripts/EnemyShooter.cs` — stationary shooter: detection-gated interval fire, White muzzle flash (4 frames), configurable sound.
- `Assets/Scripts/Bullet.cs` — kinematic trigger projectile; only the player body takes effect.
- `Assets/Scripts/ScreenFade.cs` — white reload fade + black `PlayTransition(scene, 1, 3, 1)`.
- `Assets/Prefabs/Player.prefab`, `Stick.prefab`, `Enemy.prefab`, `Box.prefab`.
- `Assets/Prefabs/EnemyShooter.prefab` — stationary ranged enemy; `Assets/Prefabs/Bullet.prefab` — projectile.
- `Assets/Prefabs/UI/Dialog.prefab` — opening-dialog UI prefab.
- `Assets/Prefabs/UI/DialogArea.prefab` — trigger-volume prefab for area dialogue.
- `Assets/Resources/ScreenFade.prefab`, `ScreenFadeBlack.prefab`, `SpriteWhiteFlash.shader`.

## Tuning snapshot (prefab overrides vs code defaults)

| Setting | Current | Code default |
| --- | --- | --- |
| Player move speed / accel | 4 / 40 | 4 / 40 |
| Camera smoothTime | 0.02 | 0.12 |
| Stick maxAngularSpeed | 180 | 720 |
| Stick ghostMinAngularSpeed | 180 | 120 |
| Stick ghost lifetime / alpha / step | 0.1 / 0.6 / 6° | same |
| Stick damage | 10 | 10 |
| Stick wall-check thickness | 0.1 | 0.1 |
| Stick wall-contact distance | 0.15 | 0.15 |
| Enemy detection radius | 5 | 6 (if auto-created) |
| Enemy move speed | 5 | 2.5 |
| Enemy maxHealth | 50 | 30 |
| Enemy stagger / flash | 0.15 / 0.2 | same |
| Enemy hitStopFrames | 30 | 2 |
| ScreenFade duration / hold | 0.6 / 0.05 | same |
| Level complete pre-fade delay | 2 | 2 |
| Black transition in / hold / out | 1 / 3 / 1 | same |
| Level music volume | 1 | 1 |
| Shooter fire interval | 1.5 | 1.5 |
| Bullet speed / lifetime | 8 / 4 | 8 / 4 |
| Bullet vanish duration | 0.5 | 0.5 |
| Shooter White flash frames | 4 | 4 |
| Vignette intensity / inner / outer | 0.55 / 0.45 / 1.25 | same |
| Scanline intensity / spacing / thickness | 0.18 / 3 / 0.35 | same |
| RGB split amount | 0.0025 | 0.0025 |
| Dialog typewriter speed | 30 chars/s | 30 |
| YSort baseOrder / precision | 10000 / 100 | same |

## Known issues / next steps

- The six enemies are west of the player; the map is extended. Make sure the player can actually walk to them and they are inside the tilemap.
- For a 3-hit kill (30 HP / 10 damage) set Enemy `maxHealth = 30`. Current 50 = 5 hits.
- `hitStopFrames = 30` is a long freeze; use 2-4 if it feels like a hitch.
- If hits feel unreliable, check the enemy body collider size/offset versus the sprite; the stick trigger is generous (≈1.95 × 0.19).
- Enemies move straight at the player within the detection radius and have no obstacle avoidance; they can be stopped by walls/Boxes. This is intentional since A* was removed for performance.
- Pixel Perfect Camera is PPU 100 / 640×480 while tiles are PPU 32 and player 36; adjust if the pixel scale looks wrong.
- Every level scene added to `LevelManager.levels` must also be added to Build Settings (Launcher is index 0, ED last).
- The configured `SampleScene` dialog line is a placeholder (`这是一段对话`) with no portraits/sound; replace it in the Launcher LevelManager inspector.
- `Dialog.prefab` uses `fusion-pixel-12px-monospaced-latin.ttf`, which has no CJK glyphs; swap the font if Chinese text renders blank/boxes.
- Do not recreate the deleted setup tools.
