# Knockback Rumble — Prototype

A **tournament** of mini-games, played back to back. The host picks the games (or hits **RANDOMIZE**), everyone
plays them in a row, and everything you do earns or loses **cash**. The richest dummy after the last game wins the
tournament. Playable **offline vs bots** or **online with friends** through Steam.

There are **18 games**: the three classic events below, plus 15 mini-games (see "The mini-games").

1. **The Gauntlet**: the Scrap Sprint obstacle course: conveyor with bumpers, spiked wrecking balls, hover
   platforms, crusher hall (container shortcut), spin bar, Punch Alley (boxing gloves), Piston Pit, Wind Tunnel
   (fans + swinging plungers), finish stage. 5 checkpoints. Finish payouts $500 / $350 / $250 / $150, then $100.
   Ends when 5 of 8 finish or after 3:20.
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

- **PLAY OFFLINE**: you vs 7 bots. Opens **PICK THE GAMES** first (see "Tournaments").
- **LOADOUT**: pick your gear (also in the lobby and the pause menu; changes apply at the next event).
- **HOST A GAME**: makes a friends-only Steam lobby. Invite friends from there.
- **Join a friend**: accept their Steam invite, or right-click them in your Steam friends list → **Join Game**.
- **TESTING TOOLS → HOST DIRECT / JOIN DIRECT**: connect by IP address, for testing without Steam.
- **TESTING TOOLS → PROGRESS**: +$10,000 wallet cash, +1 level, MAX LEVEL (unlocks every weapon, perk and streak),
  and RESET PROFILE (click twice) to go back to a brand-new player. Take these out before release.

Your level, XP and wallet show at the bottom right of the main menu.

## Tournaments

**PICK THE GAMES** opens when you press PLAY OFFLINE, or START TOURNAMENT in the lobby (host only).

- **PICK GAMES**: click games to add them. They're played in the order you click them (the number on the card).
  Click one again to take it out. **ALL** picks every game, **CLEAR** empties the list.
- **RANDOMIZE**: choose how many games (1 to 20). The game picks them for you, with no repeats until every game
  has been played once.
- **START!** begins. Your choice is saved for next time.

After the last game, **R** starts a new tournament with the same settings (re-randomized if RANDOMIZE is on).
Online, **L** sends everyone back to the lobby, where the host can change the games.

## The mini-games

Mini-game arenas are built the first time they're played (during the loading screen), so there's nothing to
rebuild in the editor. Each one lives in `Scripts/MiniGames/`, one file per game.

| Game | What you do | Rules |
|---|---|---|
| **Hot Potato** | Pass a ticking bomb by touching or shoving someone | Holding it when it blows = out. +$40 per blast survived. Shoves only. |
| **Throne Room** | King of the hill around a golden toilet | $20/s alone on the throne, $6/s if shared. It moves every 20 s. |
| **Make It Rain** | Grab coins, cash stacks and gold bars falling from the sky | $15 / $35 / $75. Bombs fall too (red shadows). |
| **Greased Sumo** | Shove people off a greasy, shrinking ring | One life. Shoves only. Low grip. |
| **Spin Cycle** | Jump two sweeper bars inside a giant washing machine | One life. Bars speed up. Shoves only. |
| **The Floor Is Lava** | Climb two spiral staircases around a tower as lava rises | One life (lava = out). Guns allowed. |
| **Turf War** | Paint floor tiles your color by running over them | +$1 per tile painted, $8 per tile you own at the end. |
| **Rocket Rodeo** | Everyone gets unlimited rocket launchers on floating islands | +$100 extra per knock-off. |
| **Toilet Roll Hill** | Race up a hill dodging giant toilet rolls | Race payouts like the Gauntlet. Shoves only. |
| **Hole in the Wall** | Get through the holes in walls sliding across the stage | One life. +$40 per wall survived. Shoves only. |
| **Porta-Potty Panic** | Musical chairs: when the music stops, run into a potty | One too few each round; no potty = out. +$60 per round survived. |
| **Bird Brained** | Dodge seagull "presents" (red circles show where) | One life. Shoves only. |
| **Red Light, Green Light** | Run while Lord Dookington's back is turned | Move on red and you get zapped back. Race payouts. |
| **Balloon Pop** | Pop balloons in a bounce house; trampolines reach the high ones | $15 per balloon, $60 gold. |
| **Treadmill of Doom** | Keep running on a speeding treadmill, jump/dodge the junk | One life (the grinder at the back). |

Games with "one life" pay $5-6 a second while you're in, plus survivor bonuses at the end, like Crumble Plaza.
Knock-offs (+$100) and falls (-$50) count in every game.

**Adding a game:** copy one of the files in `Scripts/MiniGames/`, give it a new `EventKind` (in `CourseInfo.cs`),
add it to the list at the top of `MiniGames` (in `MiniGame.cs`) and to `Events.All`.

## Characters

**CHARACTER** (main menu, lobby, pause menu) picks who you play as: Sarge, Rookie, Mohawk, Beardo, Doc or Gramps.
Your vest is always your player color. Everyone sees your pick online, and each bot always plays the same one.
The list lives in `Scripts/Cosmetics/Characters.cs`. For now they're variations of the built-in low-poly soldier;
real character models can replace them later.

The old outfit shop (hats, shirts...) is switched off. Its code is still in `Scripts/Cosmetics/Cosmetics.cs`.

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
4. You should see both players in the lobby. In the build, press **START TOURNAMENT**, pick the games, then **START!**.
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
  Scripts/Core/      DummyMotor (movement), DentBody (knockback), DummyVisual (the soldier model + outfits), Contestant
  Scripts/Cosmetics/ Cosmetics (outfit items), CustomizePreview (the spinning dummy on the CUSTOMIZE screen)
  Scripts/MiniGames/ MiniGame (base class + the MiniGames list), MiniKit (arena building blocks), CollectGame (shared by
                     Make It Rain and Balloon Pop), and one file per mini-game
  Scripts/Player/    PlayerInputDriver, ThirdPersonCamera (with aim zoom)
  Scripts/Loadout/   Loadouts (perks, gadgets, streaks, unlock levels) and Profile (level, XP, wallet)
  Scripts/Weapons/   WeaponModels (gun, rocket, bomb, spike ball models), Puffs (low-poly smoke/fire/sparks/confetti),
                     WeaponDef (gun balance), WeaponHolder, Weapons, Rocket, WeaponPickup (ammo crates), Fx,
                     Gadgets (+ Grenade, StickyMine), FallingBomb + FlyBy (Payout Streaks)
  Scripts/AI/        BotBrain
  Scripts/Game/      WorldLook (sun, sky, fog, bloom/color grading), Tournament (the host's game picks), ShowManager (tournament flow, cash, bags), MoneyBag, RaceManager (runs one event), CourseInfo,
                     HUD, MenuUI + UiSkin (menus, loadout screen), Announcer (all the jokes), Personas (voice packs), PrototypeBootstrap
  Scripts/Audio/     Sfx + SfxRunner (sound effects and music), VoiceLine, VoiceDirector (spoken lines, off for now)
  Scripts/Net/       NetSession (lobby, relay, clock), MatchController (race <-> network), SteamService,
                     SteamTransport, TcpTransport, RemoteDummy (smooths other players), NetVoice + VoicePlayer
  Scripts/Level/     ScrapSprintBuilder (the Gauntlet), ArenaBuilder (Cash Grab, Crumble Plaza), Kit (decks, I-beams,
                     gantries, railings, containers, arches), CrumbleFloor, hazards (incl. PunchGlove, PistonPillar, WindBlower),
                     Props (junk, grandstands + crowd, light towers, jumbotron, blimp, mountains, clouds)
  Scripts/Util/      Mats (materials), LowPoly (faceted procedural meshes, saved to Meshes/ in edit mode)
```

Not in this build yet: cosmetics to spend your wallet on, more guns / perks / streaks, more events, a lineup
the host can pick, Hecklers, real art and audio.
