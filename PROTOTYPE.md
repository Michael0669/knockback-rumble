# Knockback Rumble — Prototype

A **show** of 3 events, played back to back. Everything you do earns or loses **cash**; the richest dummy
after the last event wins the show. Playable **offline vs bots** or **online with friends** through Steam.
Everything is greybox.

1. **The Gauntlet**: the Scrap Sprint obstacle course. Finish payouts $500 / $350 / $250 / $150, then $100.
   Ends when 5 of 8 finish or time runs out.
2. **Cash Grab**: an open arena with 3 money bags. Hold one for $10 a second. Get hit and it pops loose.
3. **Crumble Plaza**: the floor falls apart tile by tile. $5 a second while you're standing. Fall and you're out
   (you watch from the Losers' Lounge). Survivors get a bonus.

In every event: **knock someone off = +$100**, **fall off = -$50**, and every hit you take from a player pays you
a little "pain pay".

After a show you keep your cash in your **wallet** (for cosmetics later) and earn **XP**. Levels unlock more
loadout items.

## Run it in the editor

1. Pull the latest from GitHub (GitHub Desktop → **Fetch origin** → **Pull origin**).
2. **Open Steam and log in** (needed for online play and voice chat; offline works without it).
3. Open the project in Unity and wait for scripts to compile. The Console should show no red errors.
4. **Knockback Rumble → Build Show Scene**. This rebuilds the scene with all three arenas. (If you skip it,
   the game builds the two new arenas when you press Play, so old scenes still work.)
5. Press **Play**. You land on the main menu.

Spoken lines (announcers and bot barks) are switched off for now. To bring them back, set
`VoiceDirector.Enabled = true` in `Scripts/Audio/VoiceDirector.cs`, then run
**Knockback Rumble → Voices → Generate Placeholder Voices**.

**Sound:** every sound effect and both music loops are made from scratch by `Tools/make_sfx.py` (no samples, no
licenses). To swap one for a real recording, save it as a 16-bit mono WAV and rename it to
`Art/Resources/SFX/<name>.bytes`. Music and effects volume are in **VOLUME** (main menu, lobby, pause menu).

Before every event a **loading screen** shows the rules, the controls, your loadout and the cash standings
for 10 seconds (first event) or 8 seconds (later ones), then the 5-second countdown starts.

## The menu

- **PLAY OFFLINE**: you vs 7 bots.
- **LOADOUT**: pick your gear (also in the lobby and the pause menu; changes apply at the next event).
- **HOST A GAME**: makes a friends-only Steam lobby. Invite friends from there.
- **Join a friend**: accept their Steam invite, or right-click them in your Steam friends list → **Join Game**.
- **TESTING TOOLS → HOST DIRECT / JOIN DIRECT**: connect by IP address, for testing without Steam.

Your level, XP and wallet show at the bottom right of the main menu.

## Loadouts

| Slot | Options (unlock level) |
| --- | --- |
| Primary | Pop Pistol (1), Boom Stick shotgun (1), Rocket Launcher (3), Long Shot sniper, 3 shots, scope (5) |
| Gadget | Bouncer Grenade (1), Sticky Mine (2). 2 per event |
| Perks (pick 2) | Steel Toes (1), Grease Monkey (1), Double Jump (2), Deep Pockets (3), Lightweight (4), Scavenger (4) |
| Payout Streaks (pick 2) | Gary Strike: 3 knock-offs in a row (1), Wrecking Ball on the cash leader: 5 in a row (3) |

Streaks count knock-offs since your last fall. Ammo crates refill your primary (and your gadget with Scavenger).
New items go in `Scripts/Loadout/Loadouts.cs` (perks, gadgets, streaks) and `Scripts/Weapons/WeaponDef.cs` (guns).

## Controls

| Action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move / look | WASD / mouse | Left stick / right stick |
| Jump (mash to recover from a launch) | Space | A |
| Dive | Left Ctrl or C | B |
| Fire | Left mouse | Right trigger |
| Aim / scope (zoom) | Hold right mouse | Hold left trigger |
| Shove | E or F | X or RB |
| Throw gadget | G | LB |
| Call in a Payout Streak | Q | Y |
| Pause menu (loadout, leave game, back to lobby) | Esc | Back / Select |
| Full standings (hold) | Tab | |
| Spectating: switch player | Left / right click, A / D | LB / RB |
| Spectating: heckle (tomato, boo, air horn, fart) | 1 / 2 / 3 / 4 | D-pad |
| Mute / unmute your mic | M | |
| Skip to the next event / new show (host or offline, on results) | R | Start |
| Everyone back to the lobby (host, on results) | L | |

**Rocket jump:** pick the Rocket Launcher, look straight down, then jump and fire at your feet at the same time.
The purple "Express Lane" wall in the Gauntlet's crusher hall is only reachable this way (or by riding a crusher up).

## Testing online

### Test 1: by yourself on one PC (direct connect)

This checks that two copies of the game can see each other.

1. **File → Build Profiles** → Windows → **Build** (pick an empty folder such as `Builds/Test`).
   Make sure `Assets/KnockbackRumble/Scenes/ScrapSprint.unity` is in the scene list (the Build Scene menu adds it).
2. Run the built `.exe`. It opens in a window. Click **HOST DIRECT**. If Windows Firewall asks, click **Allow**.
3. In Unity, press **Play** and click **JOIN DIRECT** (IP `127.0.0.1`).
4. You should see both players in the lobby. In the build, press **START THE SHOW**.
5. Play a bit in each window. Check that the other dummy moves smoothly, shots, grenades and streaks show up on
   both, the cash numbers match, bags move on both, and the Crumble Plaza floor falls the same way on both.

Voice chat won't work in this test because both copies use the same Steam account.

### Test 2: with friends over Steam (the real test)

1. Build the game (step 1 above), then zip the whole build folder: the `.exe`, the `_Data` folder,
   `UnityPlayer.dll`, `steam_appid.txt` and everything else in it. Send the zip to your friends
   (Google Drive, Discord, etc.).
2. Everyone: open Steam, unzip, run the `.exe`. Windows may warn about an unknown app ("More info → Run anyway").
3. You: **HOST ONLINE** → **INVITE FRIENDS** (or invite them from your Steam friends list).
4. Friends: accept the invite, or right-click you in Steam → **Join Game**.
5. Start the show. Talk! Your mic is open; others hear you louder when your dummies are close.

Until the game has its own Steam page, Steam shows everyone as playing **"Spacewar"**. That's Valve's free test
app, and it's expected.

### What to tell me after testing

1. Did joining work? If not, what did the screen say?
2. Do other players' dummies move smoothly, or jitter / teleport / lag behind?
3. Do hits feel fair? (Getting hit by a rocket that visibly missed you, or shots that should hit but don't.)
4. Does voice chat work, and does proximity make sense?
5. Did anything get out of sync (cash, bags, crates, finish order, falling tiles, hazards in different places)?
6. Everyone's ping (shown top-right during a show for non-hosts), and roughly where each person is.
7. Any red errors in the Console. Copy the first one.
8. Which event was the most fun, and which loadout items felt too strong or useless?

## How it works (short version)

- **Dent meter:** no health. Every hit adds Dent (0–200%); the higher it is, the farther the next hit launches you.
  Fall off and you respawn (last checkpoint in the Gauntlet, a random spot in the arenas) with Dent reset.
- **Cash:** the host is the bank. It decides every payout and sends it to everyone, so all screens match.
  Balance numbers are at the top of `Scripts/Game/ShowManager.cs`.
- **Crumble Plaza's floor** falls on a schedule made from a shared random seed and the shared clock,
  so it falls the same way on every screen without extra network traffic.
- **Online:** each PC runs its own player's dummy (so controls feel instant) and sends its position 20 times a
  second. The host runs the bots, owns the crates and bags, pays out cash, and decides results. When you hit someone,
  your PC tells their PC, and their PC launches them. Gadgets and streaks are simulated by whoever threw them;
  everyone else sees a look-alike. Everything goes through Steam's relay, so there's no port forwarding.
- **Profile** (level, XP, wallet, loadout) is saved on this PC for now. Steam Cloud / Steam Inventory later.

## Files

```
Packages/com.rlabrecque.steamworks.net/   Steamworks.NET (MIT license) - talks to Steam
Assets/KnockbackRumble/
  Art/Resources/SFX/ sound effects and music (WAV files saved as .bytes, made by Tools/make_sfx.py)
  Art/Resources/UI/  menu art (PNG files saved as .bytes) + fonts (made by Tools/make_ui_art.py; fonts are OFL / Apache licensed)
  Editor/            Knockback Rumble menu, voice generator, build step (writes steam_appid.txt next to builds)
  Scripts/Core/      DummyMotor (movement), DentBody (knockback), DummyVisual, Contestant
  Scripts/Player/    PlayerInputDriver, ThirdPersonCamera (with aim zoom)
  Scripts/Loadout/   Loadouts (perks, gadgets, streaks, unlock levels) and Profile (level, XP, wallet)
  Scripts/Weapons/   WeaponDef (gun balance), WeaponHolder, Weapons, Rocket, WeaponPickup (ammo crates), Fx,
                     Gadgets (+ Grenade, StickyMine), FallingBomb + FlyBy (Payout Streaks)
  Scripts/AI/        BotBrain
  Scripts/Game/      ShowManager (show flow, cash, bags), MoneyBag, RaceManager (runs one event), CourseInfo,
                     HUD, MenuUI + UiSkin (menus, loadout screen), Announcer (all the jokes), Personas (voice packs), PrototypeBootstrap
  Scripts/Audio/     Sfx + SfxRunner (sound effects and music), VoiceLine, VoiceDirector (spoken lines, off for now)
  Scripts/Net/       NetSession (lobby, relay, clock), MatchController (race <-> network), SteamService,
                     SteamTransport, TcpTransport, RemoteDummy (smooths other players), NetVoice + VoicePlayer
  Scripts/Level/     ScrapSprintBuilder (the Gauntlet), ArenaBuilder (Cash Grab, Crumble Plaza), CrumbleFloor, hazards
```

Not in this build yet: cosmetics to spend your wallet on, more guns / perks / streaks, more events, a lineup
the host can pick, Hecklers, real art and audio.
