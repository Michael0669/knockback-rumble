# Knockback Rumble — Prototype

One Race round (Scrap Sprint), playable **offline vs bots** or **online with friends** through Steam.
Everything is greybox. Online is Phase 3: the goal is to find out whether 8 people can play smoothly.

## Run it in the editor

1. Pull the latest from GitHub (GitHub Desktop → **Fetch origin** → **Pull origin**).
2. **Open Steam and log in** (needed for online play and voice chat; offline works without it).
3. Open the project in Unity and wait for scripts to compile. The Console should show no red errors.
   - The first time, Unity imports the Steamworks.NET package (in `Packages/`), writes `steam_appid.txt`
     into the project folder, and adds a `STEAMWORKS_NET` setting. GitHub Desktop will show those
     changes. Commit them.
4. **Knockback Rumble → Build Scrap Sprint Scene** (only needed if you don't have the scene yet, or the course changed).
5. **Knockback Rumble → Voices → Generate Placeholder Voices** (one time, about a minute).
6. Press **Play**. You land on the main menu.

## The menu

- **PLAY OFFLINE**: you vs 7 bots, same as before.
- **HOST ONLINE (Steam friends)**: makes a friends-only lobby. Invite friends from there.
- **Join a friend**: accept their Steam invite, or right-click them in your Steam friends list → **Join Game**.
  The game must already be running when you do this.
- **HOST DIRECT / JOIN DIRECT**: connect by IP address, for testing without Steam (see below).

In the lobby the host can toggle **Fill empty spots with bots** (on by default, so 2 players still get an
8-dummy race) and press **START RACE**.

## Controls

| Action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move / look | WASD / mouse | Left stick / right stick |
| Jump (mash to recover from a launch) | Space | A |
| Dive | Left Ctrl or C | B |
| Fire weapon | Left mouse | Right trigger |
| Shove | E, F or right mouse | X or RB |
| Pause menu (leave game, back to lobby) | Esc | Back / Select |
| Mute / unmute your mic | M | |
| Next race (host / offline, on results) | R | Start |
| Everyone back to the lobby (host, on results) | L | |

**Rocket jump:** grab a rocket crate, look straight down, then jump and fire at your feet at the same time.
The purple "Express Lane" wall at the crusher hall is only reachable this way (or by riding a crusher up).

## Testing online

### Test 1: by yourself on one PC (direct connect)

This checks that two copies of the game can see each other.

1. **File → Build Profiles** → Windows → **Build** (pick an empty folder such as `Builds/Test`).
   Make sure `Assets/KnockbackRumble/Scenes/ScrapSprint.unity` is in the scene list (the Build Scene menu adds it).
2. Run the built `.exe`. It opens in a window. Click **HOST DIRECT**. If Windows Firewall asks, click **Allow**.
3. In Unity, press **Play** and click **JOIN DIRECT** (IP `127.0.0.1`).
4. You should see both players in the lobby. In the build, press **START RACE**.
5. Play a bit in each window. Check that the other dummy moves smoothly, shots and launches show up on both,
   crates disappear on both, and the results match.

Voice chat won't work in this test because both copies use the same Steam account.

### Test 2: with friends over Steam (the real test)

1. Build the game (step 1 above), then zip the whole build folder: the `.exe`, the `_Data` folder,
   `UnityPlayer.dll`, `steam_appid.txt` and everything else in it. Send the zip to your friends
   (Google Drive, Discord, etc.).
2. Everyone: open Steam, unzip, run the `.exe`. Windows may warn about an unknown app ("More info → Run anyway").
3. You: **HOST ONLINE** → **INVITE FRIENDS** (or invite them from your Steam friends list).
4. Friends: accept the invite, or right-click you in Steam → **Join Game**.
5. Start a race. Talk! Your mic is open; others hear you louder when your dummies are close.

Until the game has its own Steam page, Steam shows everyone as playing **"Spacewar"**. That's Valve's free test
app, and it's expected.

### What to tell me after testing

1. Did joining work? If not, what did the screen say?
2. Do other players' dummies move smoothly, or jitter / teleport / lag behind?
3. Do hits feel fair? (Getting hit by a rocket that visibly missed you, or shots that should hit but don't.)
4. Does voice chat work, and does proximity make sense?
5. Did anything get out of sync (crates, finish order, results, hazards in different places)?
6. Everyone's ping (shown top-right during a race for non-hosts), and roughly where each person is.
7. Any red errors in the Console. Copy the first one.

## How it works (short version)

- **Dent meter:** no health. Every hit adds Dent (0–200%); the higher it is, the farther the next hit launches you.
  Fall off and you respawn at the last checkpoint with Dent reset.
- **Weapons:** Shove (always), Pop Pistol (8 shots, light push), Rocket Launcher (3 rockets, big splash).
  Your own explosions launch you but add only half the Dent and never stun you.
- **Course:** start pad → backwards conveyor with bumpers → wrecking-ball bridge → jump gaps →
  crusher hall (with rocket-jump shortcut) → spin bar → ramp → finish. 3 checkpoints.
- **Race:** the first 5 of 8 across the line qualify. Rusty Nutz and Trina Tailpipe heckle out loud.
- **Weapon crates:** 4 on the course, respawning every 20 s.
- **Online:** each PC runs its own player's dummy (so controls feel instant) and sends its position 20 times a
  second. The host runs the bots, owns the crates, and decides finish order and results. When you hit someone, your
  PC tells their PC, and their PC launches them. Hazards run off a shared clock, so they line up on every screen.
  Everything goes through Steam's relay, so there's no port forwarding.

## Files

```
Packages/com.rlabrecque.steamworks.net/   Steamworks.NET (MIT license) - talks to Steam
Assets/KnockbackRumble/
  Art/Resources/UI/  menu art (PNG files saved as .bytes) + fonts (made by Tools/make_ui_art.py; fonts are OFL / Apache licensed)
  Editor/            Knockback Rumble menu, voice generator, build step (writes steam_appid.txt next to builds)
  Scripts/Core/      DummyMotor (movement), DentBody (knockback), DummyVisual, Contestant
  Scripts/Player/    PlayerInputDriver, ThirdPersonCamera
  Scripts/Weapons/   WeaponDef (balance), WeaponHolder, Weapons, Rocket, WeaponPickup, Fx
  Scripts/AI/        BotBrain
  Scripts/Game/      RaceManager, HUD, MenuUI + UiSkin (menus and their look), Announcer (all the jokes), Personas (voice packs), PrototypeBootstrap
  Scripts/Audio/     VoiceLine, VoiceDirector
  Scripts/Net/       NetSession (lobby, relay, clock), MatchController (race <-> network), SteamService,
                     SteamTransport, TcpTransport, RemoteDummy (smooths other players), NetVoice + VoicePlayer
  Scripts/Level/     ScrapSprintBuilder (the layout) and hazards
```

Not in this build yet: the other round types (Survival, Team, Final), Hecklers, more weapons, real art and audio.
