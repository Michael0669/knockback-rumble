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

After a tournament your cash goes into your **wallet**. There are no levels: you spend wallet cash to unlock
guns, gadgets, perks, kill streaks and characters, in any order you like. Stronger stuff costs more.

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
- **TESTING TOOLS → PROGRESS**: +$10,000 wallet cash, UNLOCK EVERYTHING (owns every gun, gadget, perk, streak and
  character for free), and RESET PROFILE (click twice) to go back to a brand-new player. Take these out before release.

Your wallet and the loadout you're using show on the LOADOUT button.

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
| **Bottomless Clip** | Your own loadout gun with unlimited ammo, on a plus-shaped rooftop | +$100 extra per knock-off. Streaks allowed. |
| **Shotgun Wedding** | Unlimited Boom Sticks on top of a giant wedding cake | One life. +$50 extra per knock-off. |
| **Gone With The Wind** | Unlimited Leaf Blowers on a rooftop; big gusts every 11 s (watch the wind sock) | One life. |
| **Duck Season** | Unlimited Long Shot snipers on nine tall pillars | +$150 extra per knock-off. Hop pillars to dodge. |
| **Moon Base** | Low gravity, bounce pads and unlimited Spray-n-Prays | +$75 extra per knock-off. |
| **Pinball Wizard** | You're the ball: bumpers fling you, flippers swat you, targets at the top pay | $25 per target touch. Drain = -$50. Shoves only. |
| **Color Crazy** | A color is called; get on a tile of that color before the rest drop | One life. Faster every round. Shoves only. |
| **Wheel of Misfortune** | A giant wheel changes the rules every 15 s (rockets, leaf blowers, moon gravity, grease, shoves only, double cash...) | +$50 extra per knock-off. Unlimited ammo. |
| **Sky High** | Race up a staircase of floating platforms into the clouds | Race payouts. Guns allowed. Checkpoints. |
| **Minefield** | Race across a field packed with mines (they re-arm after 2.5 s) | Race payouts. Shoves only. |
| **Bouncy Castle** | You bounce every time you land; unlimited Pop Pistols; the castle deflates | One life. |
| **Swirly** | A flushing toilet bowl drags you round and into the growing drain | One life. Shoves only. |

Games with "one life" pay $5-6 a second while you're in, plus survivor bonuses at the end, like Crumble Plaza.
Knock-offs (+$100) and falls (-$50) count in every game.

**Adding a game:** copy one of the files in `Scripts/MiniGames/`, give it a new `EventKind` (in `CourseInfo.cs`),
add it to the list at the top of `MiniGames` (in `MiniGame.cs`) and to `Events.All`.

## Characters

**CHARACTER** (main menu, lobby, pause menu) picks who you play as: 12 characters from the downloaded pack
(SWAT, adventurer, farmer, construction worker, punk, astronaut, king, beach guy, influencer, dude, lawyer and
Horse Guy). Their main piece of clothing takes your player color, and there's a colored ring under everyone's
feet. Everyone sees your pick online; each bot always plays the same one. The list is in
`Scripts/Cosmetics/Characters.cs` (which parts of the model, which material gets your color).

The old outfit shop (hats, shirts...) is switched off. Its code is still in `Scripts/Cosmetics/Cosmetics.cs`.

## Downloaded art

Models live in `Assets/ThirdParty/Resources/` (Characters, Guns, Environment, Vehicles). The original
.blend / .obj / .gltf files are in `ArtSource/` at the top of the repo, outside `Assets`, so Unity ignores them.

- **Characters:** one rigged file with 12 people and their animations (idle, run, shoot, punch, hit, wave).
  `Scripts/Core/ModelCharacter.cs` shows one person's parts, scales them to dummy size and plays the animations.
- **Guns:** pistol (Pop Pistol), shotgun (Boom Stick), sniper rifle (Long Shot), rifle (Bonk Rifle), P90
  (Spray-n-Pray) and revolver (Hand Cannon). The Leaf Blower and The Big Flush are built-in models. `Scripts/Weapons/GunModels.cs`
  measures each model the first time and fits it into the hand. The rocket launcher has no model in the pack,
  so it keeps the built-in one.
- **Props and vehicles:** barrels, cones, tires, shipping containers, couches, wrecked cars, junk, a water tower
  and street lights replace the matching placeholders (`Props.cs`, `Kit.cs`, `MiniKit.cs`).

Anything missing falls back to the old built-in look. To switch all of it off, set `Art.UseThirdParty = false`
in `Scripts/Util/Art.cs`. The whole game as it was before the downloaded art is saved on the
**backup/placeholder-art** branch on GitHub.

**After pulling:** run **Knockback Rumble → Build Show Scene** once, so the Gauntlet, Cash Grab and Crumble
Plaza pick up the new props (the mini-games and characters don't need it).

## Loadouts and unlocks

**LOADOUT** has 5 saved loadouts (tabs along the top). The one you click is the one you play with ("IN USE").
Rename it in the NAME box, then set its 6 slots. Anything with a price tag is locked: click it once ("CLICK TO
BUY"), then again to buy it with wallet cash and equip it. Gold price = you can afford it, red = you can't yet.
Buying something unlocks it for all 5 loadouts. The end-of-tournament screen shows the next thing you can buy.

| Slot | Options (price, free = starter) |
| --- | --- |
| Primary | Pop Pistol (free), Bonk Rifle $1,500, Leaf Blower $2,000, Spray-n-Pray $2,500, Boom Stick $3,000, Hand Cannon $3,500, Long Shot $5,000, Rocket Launcher $6,000, The Big Flush $8,000 |
| Gadget | Bouncer Grenade (free), Sticky Mine $1,000, Banana Peel $1,200, Stink Bomb $1,500, Bounce Pad $2,000, Air Horn $2,500 |
| Perks (pick 2) | Steel Toes, Grease Monkey (free), Scavenger $1,000, Springs $1,000, Bandolier $1,200, Double Jump $1,500, Lightweight $1,500, Hazard Pay $1,500, Deep Pockets $2,000, Quick Hands $2,000, Bounty Hunter $2,500, Heavy Hitter $3,000 |
| Kill Streaks (pick 2) | Moon Boots 2 KOs $1,000, Carpet Bomb 2 KOs $1,500, Gary Strike 3 KOs (free), Anvil Drop 4 KOs $3,000, Wrecking Ball 5 KOs $2,500, Tactical Toilet 7 KOs $6,000 |

**New guns:** Bonk Rifle (steady mid-range pushes), Spray-n-Pray (30-round bullet hose, wobbly aim), Hand Cannon
(6 huge slugs), Leaf Blower (hold to push people; barely hurts, barely stuns), The Big Flush (fires a whole toilet:
slow, enormous splash).
**New gadgets:** Banana Peel (step on it and you slip straight up), Stink Bomb (big soft blast that piles on %),
Bounce Pad (a trampoline anyone can use), Air Horn (blasts everyone near you away, never you).
**New perks:** Bandolier (+50% ammo), Quick Hands (shoot 20% faster), Heavy Hitter (your hits knock 15% harder),
Bounty Hunter (+$50 per knock-off), Hazard Pay (double pain pay), Springs (jump 20% higher).
**New streaks:** Moon Boots (low gravity for 12 s), Carpet Bomb (a line of bombs ahead of you), Anvil Drop (anvils
on the two closest rivals), Tactical Toilet (a giant toilet on every rival).

Streaks count knock-offs since your last fall. Ammo crates refill your primary (and your gadget with Scavenger).
Prices live next to each item: `Scripts/Loadout/Loadouts.cs` (perks, gadgets, streaks), `Scripts/Weapons/WeaponDef.cs`
(guns) and `Scripts/Cosmetics/Characters.cs` (characters).

**Characters** also cost cash now (two are free). Click a locked one to preview it, then **BUY**.

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
| Call in a Kill Streak | Q | Y |
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

- **Damage %:** no health. Every hit adds to the big % number bottom-left (0–200%). Like Smash Bros, hits from
  other players barely move you at low %, and launch you farther and farther as it climbs (about 0.4x at 0%,
  1.6x at 100%, 3.4x at 200%; the curve is in `DentBody.Hit`). Hazards push the same at any %.
  Fall off and you respawn (last checkpoint in the Gauntlet, a random spot in the arenas) back at 0%.
- **Cash:** the host is the bank. It decides every payout and sends it to everyone, so all screens match.
  Balance numbers are at the top of `Scripts/Game/ShowManager.cs`.
- **Crumble Plaza's floor** falls on a schedule made from a shared random seed and the shared clock,
  so it falls the same way on every screen without extra network traffic.
- **Online:** each PC runs its own player's dummy (so controls feel instant) and sends its position 20 times a
  second. The host runs the bots, owns the crates and bags, pays out cash, and decides results. When you hit someone,
  your PC tells their PC, and their PC launches them. Gadgets and streaks are simulated by whoever threw them;
  everyone else sees a look-alike. Everything goes through Steam's relay, so there's no port forwarding.
- **Profile** (wallet, everything you've bought, 5 loadouts) is saved on this PC for now. Steam Cloud / Steam Inventory later.

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
  Scripts/Loadout/   Loadouts (perks, gadgets, streaks, prices), Profile (wallet, unlocks, 5 loadouts), Shop
  Scripts/Weapons/   WeaponModels (gun, rocket, bomb, spike ball models), Puffs (low-poly smoke/fire/sparks/confetti),
                     WeaponDef (gun balance), WeaponHolder, Weapons, Rocket, WeaponPickup (ammo crates), Fx,
                     Gadgets (+ Grenade, StickyMine), FallingBomb + FlyBy (Kill Streaks)
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
