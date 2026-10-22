# 20 — Production Roadmap (building this game in Unity 6)

A design this large needs its own lessons applied to it: **prototype the core loop first, cut scope ruthlessly, and ship in slices.**

## 0. Status (updated 2 Oct 2026)

| Milestone | Status |
|---|---|
| M0: Paper prototype | **Done.** `Tools/economy_sim.py` |
| M1: Core sim + the player-driven day | **Done, playable.** Reworked after the first playtest (below). See the project README. |
| M2: First launch | **Mostly done:** store page (scratch.io, Vapor), wishlists, **Next Fest with demos**, launch day and **launch week** (played as days: hotfixes, answers, launch posts, word of mouth), **critic reviews** (ScoreHub), sales and payout lag, player reviews, post-mortem with lessons. **Missing:** social platforms as separate channels, the live hour-by-hour launch dashboard. |
| M3–M7 | Not started |

**What M1 implements** (`Assets/IndieStudio/Simulation`, 46 tests, plus PlayMode tests of the whole UI and of saving):
- A real calendar, and days of three slots spent by the player (02 §4). There is no clock. Bots use a repeating week plan.
- Work played as minigames: **Wire It Up** for building features, **Bug Squash** for polish and programming gigs, **Pixel Match**, **Typo Hunt**, **Synth Tuner** and **Feedback Sort** for the other gig skills. Each score becomes a ×0.5–×1.2 work multiplier.
- Story chapters as the always-visible goal (16 §4), and Doug's step-by-step tutorial (02 §10).
- Burnout, morale and money anxiety. Collapses.
- Skills with the 70 × L² curve.
- GigGrid freelancing: offers, ratings, scope creep, non-payment, deadlines, prospecting.
- Tiny and Small projects: named features per discipline, task quality, bugs, polish, the planning-fallacy estimate bias, asset packs.
- Idea cards with knowledge-dependent ranges. Research. Game jams.
- Finances: recurring costs, monthly reports, scheduled payouts, taxes, the credit-card limit, bankruptcy.
- 20 context-conditioned events, a shop of problem-solving upgrades, goals, lessons, knowledge.

**Changes to the design found while implementing:**
- **Burnout:** weekday evenings no longer carry the off-hours penalty; only weekend work does (02 §5). Otherwise the design's own "48 hours ≈ +1 burnout a week" rule of thumb was impossible.
- **Freelancing:** unfilled Gig blocks become **prospecting**, which can find new offers. Without it, gig scarcity made balanced play go bankrupt 20% of the time.
- **Task quality:** now `2.5 + 0.75 × skill` (minus the difficulty gap), instead of raw skill. A beginner's tiny game lands at "Mixed", not "Overwhelmingly Negative". Tiny games are also judged against a lower bar.
- **Small scope:** set to 30 person-weeks (inside the 20–60 band). The balanced bot ships its first Small game at about month 19.

**Reworked after the first playtest (1 Oct 2026).** The player's verdict on the real-time version was that it "starts very fast". Nobody wants to pause and unpause a clock, and work should *feel* like building something. Changes:
- **No clock, no speeds, no Week Planner.** The day has three slots and only moves when you spend them and sleep. The HUD's speed buttons became a time-of-day badge, and a **day panel** sits on the left of every screen.
- **Work is played.** Every Build and gig session is a 25–85 second minigame whose score scales the session (02 §4, 03 §2, 05 §5.1). Marketing, research, learning and rest stay instant: they are decisions, not dexterity.
- **Projects resized for sessions:** Tiny 1.2 pw (≈ 15 sessions). Bug density and polish rates were scaled ×4 to keep the same bug counts. Small first shrank to 8 pw, then went back to **30 pw (≈ 300 sessions, ~60 features)**. With tired evenings (sessions after a day-job shift count ×0.8), a full-time job never finishes one, so quitting matters again (13 §12). Asset packs cost $80 / $1,200.
- **A goal and a tutorial:** chapters (Ship your first game → Find your players → Ship a Small game → Live off your games) and Doug's guided first day.
- **Marketing posts** got a kind: screenshots, a devlog or a meme. All three have the same average reach but differ in spread and in followers versus wishlists.
- **Balance after the rework** (40 seeds × 2 years, bots play ordinary ×1.0 sessions): Balanced dips below $0 in 8 of 40 runs (never bankrupt), bottoms out around $2.0k, ships its Tiny game at about day 22 and its Small game at about day 528. Survival and a full-time job are safe but don't ship the Small game within two years. All-in goes broke within months. A careful human (×1.1–1.2) does a little better than the bots, which is intended. `Tools/economy_sim.py` was updated to the same scale (13 §12).
- **Accessibility:** timed minigames conflicted with the "no timed inputs" goal (18 §9), so there is now a **relaxed minigames** switch: no timers, accuracy-only scoring, at most ×1.10.

**Done since (2 Oct 2026):**
- **Save/load.** The whole state is written to a versioned binary save (`SaveGame.cs`). Numbers are stored bit-exact and the random streams keep their position, so a loaded game continues exactly as the original would; the tests check every field and an identical continuation. The game autosaves a second after every change (and on quit) to `persistentDataPath/Saves/studio.isav`, writing through a temp file and keeping a backup. A damaged save is set aside and the backup loaded. Returning players get a "Welcome back" card with Continue or Start a new studio.
- **The relaxed-minigames switch** (18 §9) and a HUD menu.
- `Tools/economy_sim.py` updated to session-sized projects, with a careful-play strategy (13 §12).
- **A fun pass** (the player asked for "creative things so the game is fun to play"):
  - **Small rhythms of the day (02 §4).** Flow streaks (+5/10/15% for staying on the same work). Coffee from the mug (×1.15 on the next session for +1 burnout). The cat, which cheers you up when you're low. Naming your studio, as the tutorial's first step.
  - **Seeing the game grow (18 §10).** A procedural picture of your game, drawn from genre and art style, fills in feature by feature. It shows on the Project page, the room's laptop, after build sessions and on Launch Day. Finishing a feature or the whole game gets a banner and confetti.
  - **Wire It Up variety.** One look per discipline (circuit, paint pipes, patch cables, level paths, red string), bolted anchor tiles in Tiny projects, and wobbly tiles in Small ones (18 §5).
  - **Launch Day.** A show with ticking counters and players' first impressions that name your features (`ReviewWriter`). Released games keep a "What players say" section.
  - **Balance with flow** (40 seeds × 2 years): Balanced dips below $0 in 1 of 40 runs, is never bankrupt, bottoms out around $2.3k and ships its Small game at about day 519. A full-time job stays safe and still never ships it.
- **Steam, prepared** (Steam/README.md). It all switches on when Steamworks.NET is installed and the App ID set; until then the game plays offline.
  - **Signing in:** the Steam name appears in the game.
  - **24 achievements** (16 §4.3): icons are drawn by the game, and a sheet lists them for Steamworks.
  - **Steam Cloud saves:** a three-way check against the version both copies last agreed on. A real conflict is put to the player, and the losing copy is kept as a backup.
  - **A Web API sign-in ticket** for a future server.
  - **Tests:** a fake Steam in the play-mode tests covers two computers and a conflict, and a stand-in for the Steamworks API type-checks the module without the package.

- **Launch week** (10 §7):
  - **Seven days of choices**, a slot at a time: hotfix (Bug Squash), answer players (Feedback Sort), post while it's hot, or move on.
  - **How it lands:** reviews are written each day in the players' mood, so early fixes count most. Word of mouth carries the final mood into later sales.
  - **Critics** publish over the first five days (ScoreHub).
  - **A summary** closes the week.
  - **Balance:** with no launch-week actions the numbers are exactly the plan made at launch. The bots' launch-week marketing now sells a few copies, so the balanced bot's median game income rose from $7.8k to $8.6k over two years. Every other number is unchanged.

- **Next Fest with demos** (10 §4–5): February, June and October; once per game.
  - Sign up from the store card, then make the demo in Build sessions (Bug Squash).
  - A week of wishlists follows, depending on how good the game and the demo are.
  - The bots don't enter fests, so the balance numbers are unchanged.

- **A free demo** (22 Oct 2026; README, "The demo"): chapter 1 in 15–20 minutes, for the Steam page and Next Fest.
  - **Rules** (`Content/DemoRules.cs`, `Edition` in the state, save v6): two weeks, ending at the first launch or the morning after day 14. Tiny games only, at ×0.7 work. scratch.io only, so no Vapor, Next Fest or achievements. A warning comes three days before the end.
  - **UI:** a welcome card and a How to play page (in the full game's menu too). Locked apps say what they do in the full game. Marketing, research and learning are left out of the day panel. After the first day, Doug's guide points at the next step until launch. The end card sums up your two weeks, with Wishlist / Play again / Quit.
  - **Builds:** the demo is a separate build (`SHIPIT_DEMO`, product name "Ship It! Demo", its own save) and a separate Steam app.
  - **Length:** `DemoTests` plays 40 seeds against a time model of the minigames. A typical player ships on day 5 in about 16 minutes. A careless one (weaker sessions, evenings off) still ships, on day 10, in about 20 minutes.

**Next (M2 remainder):** Chirp/Clipz/ViewTube as separate channels, and the live hour-by-hour launch dashboard. Both rework existing systems (marketing's single follower pool, the launch week), so they're worth a playtest of the current slice first.

## 1. Scope reality check

The full design is a multi-year project for a small team. The **Bedroom Developer phase** (the first two in-game years) is where the fun has to be found. If spending a day, playing a gig and launching a tiny game isn't tense and satisfying for an hour, no amount of late-game systems will fix it. Build that first.

## 2. Milestones

Each milestone answers one question. Durations are for a small team; multiply by 2–3 for a solo developer.

| Milestone | Build | The question it answers | Rough duration |
|---|---|---|---|
| **M0: Paper prototype** | The economy simulator (done: `Tools/economy_sim.py`); a spreadsheet or board-game version of the Week Planner and one project | Are weekly decisions interesting on paper? | 2–4 weeks |
| **M1: Core sim + the day** | Time system, founder, gigs, finances (cash, runway, monthly report), a Tiny project with the basic work model, inbox events | Is the survival loop tense and fun for an hour? | 6–10 weeks |
| **M2: First launch** | Store page, wishlists, 2 social platforms, the sales model, **Launch Week**, player and critic reviews, **post-mortem** | Is launch a satisfying climax? Does the post-mortem teach? | 6–10 weeks |
| **M3: Development depth** | A subset of concept parameters (8 genres, 6 styles), 15 features, 15 decision cards, bugs and QA, scope creep, tech debt, estimate calibration | Do projects feel different? Do decisions matter? | 8–12 weeks |
| **M4: People** | Hiring, traits, morale, burnout, onboarding, management overhead, office v1 | Does going from solo to team feel great? | 8–12 weeks |
| **M5: Business** | Publishers and negotiation, grants, crowdfunding, influencers and the buzz cascade, festivals and demos | Do funding choices have long-term consequences? | 8–12 weeks |
| **M6: World and long game** | Market trends, competitors, event deck, reputation and tags, research, tiers and goals | Does year 8 feel different from year 2? | 10–16 weeks |
| **M7: Scale and polish** | Multiple teams, publishing label, franchises, Analyst mode, accessibility, balancing, content volume (events, names, review lines) | Is it deep for 30 hours without being tedious? | Ongoing |

**MVP** (the smallest version that proves the fantasy): M1–M4 with 8 genres, 20 features, 30 events, 20 decision cards, one publisher negotiation, 2 social platforms, Next Fest, Launch Week, post-mortems, and Tiers 1–3.

## 3. Architecture notes (Unity 6000.0, this project)

- **Simulation core in plain C#** (no MonoBehaviours), in its own assembly (`IndieStudio.Simulation.asmdef`).
  - It's **deterministic given a seed** and advanced one day at a time.
  - It can be tested with EditMode tests and run **headless** for batch balancing.
  - The UI lives in `IndieStudio.UI` and only observes the simulation and issues commands.
- **Data-driven content:** genres, themes, art styles, features, decision cards, events, gig templates, publisher archetypes and research nodes are ScriptableObjects (or JSON), with stable string IDs for saves. Designers tune without touching code.
- **An explainable modifier pipeline:** every derived value (output, quality, conversion, morale) is computed from a list of **labeled contributions**. That list *is* the "Why?" breakdown (18), for free. Build explainability into the math from day one.
- **A knowledge lens:** the simulation stores true values. The UI reads them through a lens that turns the truth into ranges, bands or 🔒, based on the player's knowledge sources (17). Analyst mode is simply a lens with perfect knowledge.
- **Events as data:** each event has conditions (composable predicates such as `runway_days < 60 AND team_size >= 3`), a weight, a cooldown, an optional chain, options, and effect lists.
- **Seeded RNG per subsystem** (market, sales, events, people), so changing one system doesn't reshuffle the others. That gives reproducible balancing runs and bug reports.
- **Saves:** versioned JSON with migrations, a monthly autosave, and Ironman support.
- **UI:** use **UI Toolkit** for the data-heavy screens (tables, lists, data binding, virtualized lists). The Studio diorama is 2D sprites. The project currently uses the built-in render pipeline; the URP 2D renderer is worth considering if you want 2D lighting in the diorama.
- **Packages to add as needed:** 2D Sprite and 2D Animation (diorama), **Localization** (this is a text-heavy game, so make strings localization-ready from day one), Addressables (later). The Input System and Test Framework are already in the project.
- **Time control:** the simulation runs on fixed ticks, decoupled from frame rate. The UI subscribes to simulation events.

## 4. Content pipeline

- Author events, decision cards and gigs in a spreadsheet and import them to data assets. Designers then iterate without code.
- **Event writing style:** short, specific, a clear choice, visible stakes, and no filler.
- Name generators for studios, people, games, creators and outlets.
- **Review-line templates** keyed to pillar extremes (11).

## 5. Tuning and playtesting

- Turn the economy targets (13) into **automated tests** that run the simulator with scripted strategies. For example:
  - *"Balanced strategy: cash dips below zero in 2–8% of runs; the Small game ships in months 12–18."*
  - *"All-in without outside money goes broke before month 6."*
- Port `economy_sim.py` into a C# **Balance Lab** editor window once the simulation core exists.
- **Watch for dominant strategies** (the simulator already flags freelance-plus-outsourcing as strong). Fix them with costs and shadows, not bans.
- **Playtest questions per milestone**, and decision logs (telemetry) to see which choices players actually agonize over and which they click through. The ones they click through fail the decision test.

## 6. Risks

| Risk | Mitigation |
|---|---|
| Scope | Milestone gates; MVP = M1–M4 |
| Complexity overwhelms new players | Progressive disclosure (16); onboarding through events; newcomer playtests |
| Fragile balance | Simulation tests, seeded RNG, target curves |
| Content volume (events, lines, names) | Data-driven templates and generators |
| Repetition in the late game | Delegation, varied project types, a moving market (16) |
| UI performance with large lists | UI Toolkit list virtualization; paged data |
