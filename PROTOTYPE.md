# Knockback Rumble — Offline Prototype (Phase 2)

The goal of this build is one question: **is knocking dummies around actually fun?**
It's one Race round (Scrap Sprint) against 7 bots, no networking yet. Everything is greybox.

## Run it

1. Pull the latest from GitHub (GitHub Desktop → **Fetch origin** → **Pull origin**).
2. Open the project in Unity and wait for scripts to compile. The Console should show no red errors.
3. In the top menu bar: **Knockback Rumble → Build Scrap Sprint Scene**.
   This builds the course, saves it to `Assets/KnockbackRumble/Scenes/ScrapSprint.unity`, and opens it.
4. Press **Play**.

After you change anything in `ScrapSprintBuilder.cs`, run the menu item again to rebuild the course.

## Controls

| Action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move / look | WASD / mouse | Left stick / right stick |
| Jump (mash to recover from a launch) | Space | A |
| Dive | Left Ctrl or C | B |
| Fire weapon | Left mouse | Right trigger |
| Shove | E, F or right mouse | X or RB |
| Free the mouse | Esc (click to grab it again) | |
| Rerun the race | R on the results screen | Start |

**Rocket jump:** grab a rocket crate, look straight down, then jump and fire at your feet at the same time.
The purple "Express Lane" wall at the crusher hall is only reachable this way (or by riding a crusher up).

## How it works

- **Dent meter:** no health. Every hit adds Dent (0–200%); the higher it is, the farther the next hit launches you.
  Fall off and you respawn at the last checkpoint with Dent reset.
- **Weapons:** Shove (always), Pop Pistol (12 shots, light push), Rocket Launcher (3 rockets, big splash).
  Your own explosions launch you but add only half the Dent and never stun you.
- **Course:** start pad → backwards conveyor with bumpers → wrecking-ball bridge → jump gaps →
  crusher hall (with rocket-jump shortcut) → spin bar → ramp → finish. 3 checkpoints.
- **Race:** first 5 of 8 across the line qualify. Rusty Nutz and Trina Tailpipe heckle in subtitles;
  bots yell their voice-pack lines when launched.

## What I need from you after playing

Answer these (a sentence each is fine), plus anything that felt bad:

1. Does running and jumping feel good, or floaty / slippery / sluggish?
2. Do launches feel big and funny? Too strong, too weak?
3. Is the rocket jump doable? Is the shortcut worth the risk?
4. Which part of the course was most fun, and which was annoying?
5. Are the bots too easy, too hard, or dumb in a specific spot?
6. Any red errors in the Console? Copy the first one to me.

## Tuning knobs

Easiest place to experiment: select a dummy in the Hierarchy while playing and change values on
**DummyMotor** (speed, jump, gravity, air control) and **DentBody** (`dentScaling`). Play-mode changes reset
when you stop, so tell me the numbers you liked and I'll bake them in.
Weapon numbers live in `Scripts/Weapons/WeaponDef.cs`.

## Files

```
Assets/KnockbackRumble/
  Editor/KnockbackRumbleMenu.cs      the "Knockback Rumble" menu
  Scripts/Core/      DummyMotor (movement), DentBody (knockback), DummyVisual (greybox dummy), Contestant
  Scripts/Player/    PlayerInputDriver, ThirdPersonCamera
  Scripts/Weapons/   WeaponDef (balance), WeaponHolder, Weapons, Rocket, WeaponPickup, Fx
  Scripts/AI/        BotBrain
  Scripts/Game/      RaceManager, HUD, Announcer (all the jokes), Personas (voice packs), PrototypeBootstrap
  Scripts/Level/     ScrapSprintBuilder (the layout) and hazards: Conveyor, Bumper, WreckingBall, Crusher, SpinBar
```

Not in this build yet: slide, ledge grab, the other 4 weapons, Hecklers, networking, real art and audio.
