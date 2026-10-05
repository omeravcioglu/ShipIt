# Indie Studio

A deep 2D management sim about building an indie game studio from a bedroom and $5,000.
The full game design lives in [`Design/`](Design/README.md). This repository currently contains
**milestone M1**, a playable slice of the solo "Bedroom Developer" years (Design/20).

## Play it

1. Open the project in **Unity 6000.0.32f1**.
2. Open `Assets/Scenes/SampleScene.unity` and press **Play**. The game starts itself; no scene setup is needed.
3. Old Dev Doug walks you through your first day (you can skip him), starting with your studio's name. **There is no clock:** time only moves when you decide what to do with the morning, afternoon and evening, using the **day panel** on the left, and then go to sleep.
4. **Work is played.** Building a feature of your game is **Wire It Up**, a connection puzzle: turn tiles until the flow reaches every module. It dresses up as the work: a circuit for code, paint pipes for art, patch cables for audio, level paths for design, red string on a corkboard for writing. Tiny projects bolt a few tiles in place; bigger ones have wobbly tiles. Each kind of gig has its own minigame: **Bug Squash** (programming, also used for polishing your game), **Pixel Match** (art), **Typo Hunt** (writing), **Synth Tuner** (audio) and **Feedback Sort** (design). The better you play, the more the session gets done.
5. The HUD shows the part of the day, your money and runway, how you feel, and your **goal**: the current chapter, starting with *Ship your first game*. Click it for the steps. The **menu** (☰, top right) shows who's signed in and your **achievements** (24 of them), and has **relaxed minigames** (no timers), the save status, "Replay the tutorial", "Rename your studio" and "Start a new studio…".
6. **The game saves itself** a second after every change and on quit, to `Saves/studio.isav` in Unity's `persistentDataPath` (on Windows, under `AppData/LocalLow/McHunter Studio/Ship It!/`; a save from the old `DefaultCompany/IndieStudio` folder is copied over once). Next time you press Play you're welcomed back where you left off. A damaged save is set aside and the backup loaded. On Steam the save also goes to **Steam Cloud**, so your studio follows you to any computer.
7. The room is a status display:
   - The window shows the time of day, and your studio's sign hangs above it.
   - You sleep in bed when you rest. Pizza boxes pile up with burnout, and trophies appear when you ship.
   - The laptop shows **your game as it grows**: gray boxes and a magenta placeholder hero become art, levels, music and dialogue, one feature at a time.
   - **Interactions:** click the bed to rest (or sleep at night), or click the laptop, calendar, phone, savings jar or yourself. Click the **mug** to brew a coffee, and pet the **cat** when you're feeling low.

**What you can do in M1:**
- Spend every morning, afternoon and evening yourself: Build, Gig work, Marketing (screenshots, a devlog or a meme), Research, Learn, Rest. Weeks over 45 hours build burnout.
- Take GigGrid freelance gigs to pay the rent. Pay grows with skill and rating; careful sessions earn better ratings; some clients never pay.
- Start a Tiny or Small game from an idea card, choosing genre, scope and art style. A game is a list of named features ("Player controls", "Hero sprite", "Jump scares"…), and you wire them up one session at a time.
- Work through four chapters (ship a game, find players, ship a Small game, live off your games) plus side goals.
- Keep a **flow** streak going (up to +15% for staying on the same work), drink coffee for a boost that costs a little burnout, and finish features to a burst of confetti.
- Open a scratch.io or Vapor store page and collect wishlists. Sign a Vapor game up for **Next Fest** (February, June, October; once per game): make a demo in Build sessions, and the fest week brings wishlists depending on how good the game and the demo are.
- **Launch.** You get a launch-day show with ticking counters and players' first impressions of the features you built.
- Then play **launch week**: each morning brings yesterday's sales, the reviews and the critics. You decide whether to hotfix (early fixes count most), answer players, post while the game is hot, or move on. Word of mouth carries the reviews into every month of sales after.
- Read the day-30 post-mortem: it reveals what players actually saw, and you pick a lesson.
- Survive: rent on the 1st, burnout, money stress, a dying laptop, day-job offers, game jams, rent increases, and bankruptcy past the credit limit.
- Hover numbers with a "Why?" explanation: runway, burnout, rates, output.

## Steam

The game is prepared for Steam:
- **Signing in:** your Steam name is used in the game.
- **24 achievements:** with ready-made icons.
- **Steam Cloud saves:** a conflict between two computers is put to you.
- **A sign-in ticket** for a future game server.

It all stays off, and the game plays offline, until Steamworks.NET is installed and an App ID is set. The steps,
and the sheet to fill in on the Steamworks site, are in [`Steam/README.md`](Steam/README.md). Store-page
assets are in `Marketing/Steam/`.

## Project layout

| Path | What it is |
|---|---|
| `Design/` | The game design document (21 docs) and `Tools/economy_sim.py`, the Python economy model used to explore strategies (`python Design/Tools/economy_sim.py`) |
| `Assets/IndieStudio/Simulation/` | **The game rules.** Plain C#, no UnityEngine references, deterministic per seed. Start at `GameSimulation.cs`. Other key files: <ul><li>`Content/Tuning.cs`: all tunables</li><li>`Core/SaveGame.cs`: save games</li><li>`Core/SaveSync.cs`: which copy wins between this computer and the cloud</li><li>`GameSimulation.Life.cs`: flow, coffee, the cat and the studio's name</li><li>`Content/Reviews.cs`: player reviews</li><li>`Content/Achievements.cs`: achievements</li></ul> |
| `Assets/IndieStudio/Simulation/Tests/` | NUnit tests: rules, full game flows, and bot-driven 2-year **balance tests** |
| `Assets/IndieStudio/Game/` | Unity layer: <ul><li>`GameBootstrap`: auto-start</li><li>`GameController`: owns the simulation, the platform and the autosave; no clock</li><li>`Persistence`: the save file and its cloud copy, player settings</li><li>`Platform/`: Steam or offline, and achievements</li><li>`UI/`: UI Toolkit built in code: `TodayPanel` (the day), `Coach` (the tutorial), `Activities` (activity → minigame), `Minigames/`, `LaunchShow` (launch day), `Celebrate` (confetti and hearts), `AchievementView`</li></ul> |
| `Assets/IndieStudio/Steam/` | The Steam module (Steamworks.NET): sign-in, achievements, Steam Cloud. It compiles only once the package is installed (`Steam/README.md`). |
| `Assets/IndieStudio/Game/UI/Drawing/` | Things USS can't draw, done with Painter2D: the cartoon `Avatar`, line `Icon`s (from SVG path data), `GamePreview` (your game, drawn from its genre, art style and progress), the cash-forecast chart, the cat, generated textures |
| `Assets/IndieStudio/Game/Resources/IndieStudio/` | `Styles.uss` (the warm "sticker" look: all colors and layout), `Theme.tss`, and `Fonts/` (Lilita One, SIL OFL) |
| `Assets/IndieStudio/Game/Tests/` | Play-mode tests. The smoke test boots the real game, then: names the studio and follows the tutorial; plays every minigame and every look of Wire It Up (relaxed too); brews a coffee and pets the cat; sleeps; launches a first game with its launch-day show; and opens every screen. The save tests autosave, continue in a new session and recover from a damaged save (in a temp folder). The platform tests use a fake Steam: the studio follows you to a second computer, a conflict asks which studio to keep, and achievements unlock with a toast. The Next Fest test signs up from the Project page, makes a demo session and shows the fest week. |
| `SimHarness/` | .NET projects that build and test the simulation **outside Unity** in seconds, and type-check the Unity layer and the Steam module |
| `Steam/` | The Steamworks setup: steps, the achievements sheet and their icons (`AchievementIcons/`) |

## Test it

**Fast (no Unity, about 5 seconds):**

```bash
dotnet test SimHarness/Tests
```

It runs the same test files Unity uses: 71 tests. They cover the day/slot rules, flow, coffee, tired evenings, Next Fest and demos, launch week, player and critic reviews, achievements and the cloud-save decision. Save tests check that every field survives and that a loaded game plays on identically (older save versions still load). The balance tests print a two-year economy report per strategy.

**Type-check the Unity layer against the engine DLLs, without opening the editor:**

```bash
dotnet build SimHarness/GameCompileCheck
```

**The same for the Steam module**, against a stand-in for the Steamworks.NET API (so it works without the package):

```bash
dotnet build SimHarness/SteamCompileCheck
```

**Inside Unity:** Window → General → Test Runner (Edit Mode and Play Mode). Or headless, with the editor closed:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.0.32f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/TestResults-PlayMode.xml
```

Set the environment variable `INDIE_SCREENSHOTS=1` before a Play-mode run to save screenshots of the tutorial, every minigame, launch day, every screen and the Steam features to `Logs/UiScreenshots/`. `INDIE_ACHIEVEMENT_ICONS=1` redraws the achievement icons in `Steam/AchievementIcons/`. `INDIE_GIFS=1` (with `INDIE_FFMPEG` set to an ffmpeg.exe) records the gameplay clips for the store-page GIFs; `Marketing/Steam/gifs/make_gifs.sh` cuts them into GIFs.

## Balance at a glance (40 seeds × 2 years, bot players)

Bots play every session as an ordinary one (×1.0), and their week plans earn flow streaks like a person would. A careful human playing the minigames well does a little better.

| Strategy | Went negative | Bankrupt | Median low cash | First (Tiny) game | Small game |
|---|---|---|---|---|---|
| All-in on your own game | 40/40 | 40/40 | −$4.8k | ~day 16 | — (broke first) |
| Balanced (20h gigs, 20h game, 8h marketing) | 1/40 | 0 | $2.3k | ~day 22 | ~day 519 |
| Survival (28h gigs) | 0/40 | 0 | $3.3k | ~day 40 | not within 2 years |
| Full-time job + evenings | 0/40 | 0 | $4.9k | ~day 36 | not within 2 years |

These match the shape the design promises (Design/13 §12): going all-in needs outside money, balanced play survives by a thread, gig-heavy play is safe but slow, and keeping a full-time job never finishes a Small game. That's why you quit. The balance tests fail if a change breaks that shape.
