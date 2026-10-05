# 18 — UI / UX

The player spends most of the game **managing systems, making decisions, reviewing information and planning.** The UI has to present a lot of information without overwhelming, make consequences legible, and still feel satisfying.

## 1. Presentation frame

Two layers:

1. **The Studio view:** a 2D pixel-art diorama of your workspace, growing from a bedroom to a coworking desk to an office to an HQ. People are animated to reflect their state: typing, late-night crunch surrounded by pizza boxes, a burned-out slump, celebrating a launch. It's a **diegetic status display**: you can read the studio's health at a glance. Click a person or room for quick info.
2. **The Desktop:** management screens styled as **apps on an operating-system desktop** (Mail/Inbox, Projects, Vapor Dev Dashboard, Chirp, Bank, Chorus, GigGrid…). Your **day** sits beside them on the left (§3).
   - Early game: a cheap laptop's cluttered desktop, one app at a time.
   - Late game: a studio "command center" with multi-panel layouts.
   - The evolution is **cosmetic**. Readability is never gated behind upgrades.

The functional panels are standard, readable UI. The diegetic framing is skin and flavor.

**The room (M1).** The bedroom is both status display and shortcut panel:
- **Status:** pizza boxes pile up with burnout, the savings jar empties with your runway, and trophies appear on the shelf.
- **Your studio:** its name hangs on a wooden sign above the window (click it to rename).
- **Your game:** the poster shows the current game ("A MOTH LAB GAME"), and the laptop screen shows it as it grows.
- **Coffee:** the mug brews one (02 §4). Steam rises while it's waiting.
- **The cat:** it naps on the rug. Pet it when you're low and it wakes up, purrs and sends a few hearts up.

## 2. Principles

1. **Three layers of information:**
   - **Glance:** Dashboard tiles showing a number, a trend arrow and a status color.
   - **Inspect:** a screen with charts and lists.
   - **Analyze:** tables, ledgers, CSV export.
2. **Every number answers "Why?"** Hover or click for a breakdown of known contributors, plus an **Unknown factors** slice that shrinks with knowledge (17).
3. **Consequence previews:** every decision shows its predicted effect on cash, runway, schedule, quality, morale and trust *before* confirming, with ranges.
4. **One visual language:**
   - **Discipline colors:** Programming blue, Design purple, Art orange, Animation pink, Audio teal, Writing yellow, Networking cyan, QA gray-green.
   - **Currency icons:** money, hours/blocks, reputation, trust, hype.
   - **Status:** green / amber / red, always paired with an icon or pattern.
   - **Uncertainty:** always drawn as shaded ranges.
5. **Progressive disclosure:** systems appear when needed (16).
6. **Repeated actions cost almost nothing:** templates, "repeat last", bulk actions, delegation thresholds.
7. **Risks are never hidden, and never spammed:** warnings escalate by severity and batch when minor.

## 3. Global HUD (top bar, always visible)

```
[☀ Afternoon ▮▮▯]  Tue 14 Mar, Year 2  |  💰 $3,420  ⏳ Runway 41 days ▮▮▯▯▯  |  [🙂 Morale 62 · 🔥 Burnout 48]
|  🔔 ⚖ 2 ✉ 5  |  🎯 CHAPTER 1 · GOAL  Ship your first game  2/5  |  ☰
```

There are no time controls: the part of the day replaces them (02 §4). The goal is always the current **chapter** (16 §4); click it for the steps and the side goals. The **menu** (☰) holds the relaxed-minigames switch, the autosave status, "Replay the tutorial", "Rename your studio" and "Start a new studio…" (with a confirmation).

**The day panel (left column, always visible):** the part of the day, then what you can spend it on. Build and Gig work expand into their features and gigs, each labelled with its minigame. A flame badge ("Flow +10%") marks the work that would keep a streak going, and pills warn about tired evenings after a shift or remind you of a coffee waiting (02 §4). Below that is today's log, and a "this week: 28h of 45h" bar with **Call it a day**. At night the panel shows the day's summary and **Sleep**. Forced slots (day job, rest days, jams) replace the list with one button. App windows open to the right of it, so the day is never more than a click away.

## 4. Screens

| Screen | Purpose | Key widgets |
|---|---|---|
| **Dashboard** | Studio at a glance | Runway, cash-forecast sparkline, project cards, wishlist and sales tiles, team mood, goals, upcoming calendar items, top 3 alerts |
| **Inbox** | Every event, offer and request | Messages with action buttons, filters, deadlines |
| **Day panel** | Founder time (always visible) | The part of the day, activities with their minigames, today's log, the week's hours, Sleep |
| **Minigame overlay** | Playing a work session | Intro (how to play), a timer, the game, then a result card: stars, the work multiplier, and the session's real effect. Build sessions also show **your game, so far**; finishing a feature (or the whole game) gets a banner and confetti. |
| **Launch Day** | The moment you press Launch | The finished game's picture and title, "OUT NOW", counters ticking up to day one's expected copies and money, wishlists, and players' first impressions popping in one by one |
| **Calendar** | Planning | Month, quarter and year views: deadlines, festivals, sales, releases (yours and rivals'), payroll, loan payments, **promises due** |
| **Projects** | Per-project management | Tabs: Overview · Design · Production Board · Team · Bugs & QA · Builds & Playtests · Marketing · Budget · Post-mortem |
| **Concept Builder** | Designing a game | Parameter pickers with the live Concept Forecast (05) |
| **Idea Notebook** | Ideas | A card grid; filter, merge, evaluate, prototype |
| **Team** | People | Sortable roster, profiles (skill ranges, traits, history, relationships), hiring pipeline (sourcing → interview → trial → offer), org chart, policies |
| **Studio** | Space and equipment | Office slot layout (desks and rooms), hardware and software inventory, location |
| **Finance** | Money | Overview, profit and loss, cash flow, **forecast fan chart**, payout calendar, loans, cap table, budgets |
| **Marketing** | Audience | Funnel, channels, content calendar, **creator CRM**, ads, press, **Promises ledger**, hype and buzz meters |
| **Store & Analytics** | Conversion | Page editor (capsule, screenshots, tags), conversion, wishlists (velocity, sources, forecast), demo metrics, post-launch sales and reviews |
| **Community** | Players | Chorus stats, sentiment, feature requests with votes, moderation, events |
| **Publishers & Deals** | Partnerships | Pipeline (pitch → talks → term sheet → signed), **negotiation table**, contracts and milestones, royalty statements |
| **Research** | Capabilities | Tree view, costs and upkeep, active R&D projects |
| **Market** | The world | Trends and saturation, competitor watch, release calendar, Market Report |
| **Reputation & Network** | Standing | Dimensions, tags, history, contacts CRM |
| **Goals & History** | Meaning | Goals board, Ambitions, **Studio History timeline**, Knowledge base, post-mortem archive |

## 5. Wireframes

### Dashboard (Tier 2)

```
┌───────────────────────────────────────────────────────────────────────────────┐
│ RUNWAY  41 days  ▼  (payroll 1st: $11,200)   CASH FORECAST ░▒▓▓▒░  90 days    │
├──────────────────────────────┬────────────────────────────────────────────────┤
│ PROJECT: Hollow Tide         │  WISHLISTS 6,480  +212/day ▲  (Popular         │
│ Production 54% · Art ⚠       │  Upcoming at ~7.5k? ░░░░▓▓▓▓)                  │
│ Pillars  Gam 7.2–8.1  Gra 6–7│  SALES (Tiny game) $41/week ▼                  │
│ Ship est. Oct–Dec Y2 (adj.)  │  TEAM  ● ● ◐ ○   mood: steady                  │
├──────────────────────────────┴────────────────────────────────────────────────┤
│ ALERTS  ⚠ Art is the bottleneck (+9 weeks)  ⚠ Next Fest in 6 weeks: no demo   │
│         ℹ Crimson Anvil viewed your trailer twice                             │
├───────────────────────────────────────────────────────────────────────────────┤
│ GOALS  ◻ Reach 7,500 wishlists  ◻ Hire an artist  ◼ Incorporate (done)        │
│ CALENDAR  Mar 20 payroll · Apr 3 rent · Jun 12 Next Fest · Jun 30 promise due │
└───────────────────────────────────────────────────────────────────────────────┘
```

### The day panel (early game)

```
┌ ☀ AFTERNOON ─────── Thu 12 Jan · day 12 ┐
│ [✓ Morning] [● Afternoon] [ Evening ]   │
│ What will you do this afternoon?        │
│ ⚡ Build your game     Lantern Hollow 3/8 ▾
│    ├ Hero sprite   Art · ~2 sessions   [Wire It Up]
│    └ Jump physics  Prog · ~1 session   [Wire It Up]
│ 💼 Gig work           Sam's bugs · due Sat ▸
│ 📣 Marketing          142 followers      ▸
│ 🔍 Research   🎓 Learn ▸   ☾ Rest        │
│ EARLIER TODAY ☀ Built Player controls ×1.12 │
│ THIS WEEK 28h of work ▮▮▮▮▮▯▯ | 45h   [Call it a day]
└─────────────────────────────────────────┘
```

### Minigame (Wire It Up)

```
MORNING · BUILD   Jump physics                        [Wire It Up]
Lantern Hollow · Programming · feature 4 of 8
⏱ ███████████████░░░░░ 31    4/6 modules online          [I'm done]
┌──┬──┬──┬──┐   MODULES  4/6 online
│ ╋│━━│ ┓│ ■│   ☑ Input  ☑ Physics  ☑ Collision
│ ┃│ ⚡│━┛│  │   ☑ Camera ☐ Events   ☐ Timers
└──┴──┴──┴──┘   Tip: start from the battery and follow the glow.
```

The same puzzle dresses up as the work it stands for:

| Discipline | Board | Source | Modules |
|---|---|---|---|
| Programming | A circuit board | Battery | Chips, "online" |
| Art | Paint pipes | Paint bucket | Canvases, "painted" |
| Audio | Patch cables, a different color on each tile | Oscillator | Speakers, "playing" |
| Design | Paths on grass | Start flag | Stars, "reachable" |
| Writing | Red string on a corkboard | A suspect's photo | Notes, "connected" |

Bolted tiles (four screws) are already right and can't be turned: Tiny projects get two as anchors. Wobbly tiles (red turning arrows) turn by themselves every few seconds until the flow reaches them: Small projects get two or three. Relaxed mode has no wobbly tiles.

### Project: Production Board

```
Workstream   Remaining   Capacity/wk   Weeks   Polish     People
Programming   22 pw        2.1          10.5    Standard   You, Priya
Art           41 pw        1.4          29.3 ⚠  Polished   Sam, (outsource: 0)
Design        12 pw        1.0          12.0    Standard   You (40%)
Audio          6 pw        0.5          12.0    Standard   Contract composer
QA             —           0.3          —       —          —   ⚠ QA coverage 18%
Critical path: ART → est. ship +29 weeks (adjusted for overrun history: +34)
[Buy art assets…] [Outsource…] [Hire…] [Cut content…] [Lower polish…]
```

### Negotiation table

```
TERM              THEIR OFFER    YOUR ASK     ZONE (accuracy: Negotiation 4)
Advance           $150,000       $200,000     ░░▒▒▓▓   yellow
Rev share (pub)   40%            35%          ▓▓▓▓▒░   green
IP ownership      Shared         Developer    ▒▒▓▓▓▓   red?
Marketing         $40k           $60k         ▓▓▓▒▒░   green
Console rights    Included       Excluded     ░▒▓▓▓▓   ?
Patience ●●●○○   Leverage 58 (competing offer: none; runway: 2.8 months ⚠)
[Ask] [Trade…] [Package…] [Request time] [Walk away] [Sign]   ⚖ Legal review: not done
```

### Launch Week (live)

```
DAY 2 · 14:00   Concurrent 1,842 ▲   Units 3,114   Gross $41.2k   Refunds 6.1%
Reviews 212 · 84% "Very Positive" ▲   Themes: +art +atmosphere  −crash on load (11%)
Bugs feed: 🔴 Crash on load (AMD drivers) ×37   🟠 Save icon stuck ×12
Creators: ● "Gamewitch" (320k) streaming now ▲ buzz
Front page: Popular New Releases #7 ▲
[Hotfix crash (Priya, ~10h, regression risk 12%)] [Post acknowledgment] [Push ads $2k]
```

### Finance forecast

```
Cash (90 days)   $38k ┤        ░░░▒▒▓▓▓▓▒▒░░
                      ┤    ░▒▓▓▓▒▒░
                 $10k ┤▓▓▓▒░            ← payroll dips
                   $0 ┼──────────────── ← danger line
Payouts: Vapor Mar $12.4k (Apr 28) · Publisher M3 tranche $30k (if milestone passes: 70%)
```

## 6. Inbox and notifications

| Severity | Examples | Behavior |
|---|---|---|
| **Critical** | Cash crisis, crash-bug wave, key person resigning, publisher termination | A pop-up you must read before you continue |
| **Decision** | Decision cards, offers with deadlines | A pop-up with choices. Shows the deadline and the default if you never answer. |
| **Opportunity** | Gigs, invites, festival calls | Batched daily |
| **FYI** | News, reports, rival releases | Batched into daily or weekly digests |

Delegated decisions arrive in a **weekly digest**. Every item deep-links to the relevant screen.

## 7. Onboarding

- **A guided first day** with Old Dev Doug (02 §10). He explains one thing at a time, points at what to click with a pulsing ring (scrolling the window to it), waits until you've done it, and can be skipped.
- **A goal you can always see:** the current chapter in the HUD, with its steps (16 §4).
- **Through events:** the first weeks (02 §10).
- **"First time" tooltips** that appear once, and a searchable in-game glossary (the **Studio Handbook**).
- **The mentor stays:** after the tutorial Doug sits in the corner of the room with situational advice ("Nobody tells you this, but your first estimate is always wrong.").

## 8. Late-game UI scaling

- A **portfolio view** with every project as a card showing its health.
- A **delegation panel:** thresholds and each lead's decision style.
- Studio-wide KPIs and team-level summaries.
- The UI shifts from **doing** to **overseeing**, which mirrors the fantasy.

## 9. Accessibility

- Colorblind-safe palettes, with **icons and patterns** for status (never color alone). Text scaling. A dyslexia-friendly font option. Reduced motion.
- Full keyboard and controller navigation. There is no clock, so nothing ever happens while you read. Work minigames are timed (25–85 seconds), and the **relaxed minigames** switch in the menu removes the timers. Each game then ends when it's solved or you press Finish. Bug Squash's bugs stop escaping (you squash a fixed number). Scoring is on accuracy only, so a perfect relaxed session counts ×1.10 against ×1.20 for a fast, perfect one. Launch Week can auto-play.
- Screen-reader-friendly labels where the UI framework allows.

## 10. Feedback and juice

- The sales ticker "ticks". Critic scores are revealed one card at a time. Tasteful celebrations for milestones.
- **Implemented in M1:**
  - **Your game, so far:** a little screenshot drawn from the genre and art style that fills in as features are built. It starts as gray boxes and a magenta "missing texture" hero. Art paints them in. Design adds platforms, pickups and enemies. Programming adds the jump arc and the HUD. Audio adds music notes, and Writing adds speech bubbles. Remaining bugs show up as glitches until polish clears them. It appears on the Project page, on the room's laptop, after build sessions and on Launch Day.
  - **Celebrations:** "Feature complete!" and "Content complete!" banners with confetti after the session that finishes them.
  - **Launch Day:** the show described in §4.
  - **Released games:** each card says what players think ("Love the Leaderboards. Tight and responsive."). The first impressions name features you actually built and follow the game's real pillar scores (11).
- The diorama reacts: people cheer at launch and slump during crunch.
- The **founder portrait** shows morale.
- A music shift signals a crisis. A warm theme plays after a good week.

## 11. Visual style

- Clean flat UI with soft shadows. A warm pixel-art diorama.
- A clear sans-serif for data. A slightly playful display font for headers.
- **Data visualization rules:** consistent axis formatting, uncertainty shading, sparklines on tiles, and no chart without a "Why?".
