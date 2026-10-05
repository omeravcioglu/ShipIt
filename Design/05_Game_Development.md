# 05 — Game Development

This is the largest system. A game project is a long chain of tradeoffs between **quality, time, money, risk and audience**. It's made in phases, by people with limited skill and energy, and judged against expectations the player helped create.

## Decisions
- **Concept:** genre, theme, art style, perspective, engine, platforms, audience, business model, price, scope.
- **Features:** what goes in, what's hygiene, what's a differentiator, and what gets cut.
- **Team and pipeline:** who works on what, the polish level per workstream, bottlenecks, contractors versus hires.
- **Decision cards:** dozens of quality, time and money tradeoffs per project, often with uncertain outcomes.
- **Risk management:** scope creep, technical debt, crunch, QA depth, ship versus delay.

---

## 1. Project anatomy

A project holds:
- **Concept:** everything in §2.
- **Feature list:** §3.
- **Content target** in content units: roughly hours of content, adjusted per genre.
- **Workstreams** per discipline, with task backlogs.
- **Team** assignments.
- **Codebase** (with its technical debt) and **asset library** usage.
- **Bug database:** known bugs plus a hidden population of unknown ones.
- **Performance load** per target platform.
- **Pillar quality estimates:** eight pillars, each shown as a range.
- **Schedule and budget forecast.**
- **Promises** attached to this game.
- **Marketing state:** store page, wishlists, hype (09, 10).
- **Phase** (§4).

---

## 2. Concept parameters

Every choice changes **development difficulty, cost, audience size, marketing potential, performance requirements and review expectations**. The **Concept Forecast** panel updates live as the player changes parameters (§2.10).

### 2.1 Genres

Pillar weights are the share each core pillar contributes to the review score (they sum to 100). Performance and Stability act as **penalties** for every genre, scaled by performance sensitivity.

| Genre | Market | Dev complexity | Content hunger | Streamer appeal | Trend volatility | Typical price | Weights: Gameplay / Graphics / Audio / Story / Content / Innovation | Perf. sensitivity | Expected features |
|---|---|---|---|---|---|---|---|---|---|
| Platformer | 3 | 2 | 3 | 2 | Low | $10–20 | 40/20/10/0/15/15 | Med | Controller support |
| Puzzle | 2 | 1 | 2 | 1 | Low | $5–20 | 35/10/10/5/10/30 | Low | Tutorial |
| Horror | 3 | 3 | 2 | **5** | High | $10–25 | 25/25/25/15/5/5 | Med | Settings (brightness), save system |
| Roguelike | 4 | 3 | 3 | 3 | Med | $15–25 | 45/10/5/0/25/15 | Med | Progression, procedural generation |
| Deckbuilder | 3 | 2 | 3 | 3 | Med-High | $15–25 | 50/10/5/0/20/15 | Low | Progression |
| Survival | 4 | 4 | 4 | 4 | High | $15–30 | 35/15/5/5/25/15 | High | Crafting, base building, co-op |
| Simulation | 4 | 3 | 3 | 3 | Med | $15–30 | 40/10/5/0/25/20 | Med | Tutorial |
| Management | 3 | 3 | 3 | 2 | Low | $15–30 | 45/15/5/0/20/15 | Low | Tutorial, settings |
| Strategy | 3 | 5 | 4 | 2 | Low | $20–40 | 50/10/5/5/20/10 | Med | Advanced AI, tutorial |
| RPG | 5 | 5 | 5 | 3 | Low | $20–40 | 30/10/5/30/20/5 | Med | Story, progression, save system |
| Action | 5 | 4 | 3 | 3 | Med | $15–30 | 40/25/10/5/10/10 | High | Controller support, settings |
| Metroidvania | 3 | 3 | 4 | 2 | Low | $15–25 | 35/20/10/5/20/10 | Med | Controller support, progression |
| Narrative Adventure | 2 | 2 | 3 | 2 | Low | $10–25 | 5/20/15/45/5/10 | Low | Save system |
| Cozy Life Sim | 4 | 3 | 5 | 3 | Med-High | $15–30 | 25/25/10/5/30/5 | Low | Character customization, save system |
| Party (Multiplayer) | 3 | 3 | 2 | **5** | Very High | $5–15 | 40/10/5/0/15/30 | Med | Local co-op **or** online + matchmaking |
| Bullet Heaven | 3 | 2 | 3 | 4 | Very High | $3–10 | 50/10/5/0/25/10 | High | Progression, controller support |

*Market, complexity, content hunger and streamer appeal are on a 1–5 scale. Market is the baseline audience before trends and saturation (14).*

### 2.2 Themes

Themes: Fantasy, Dark Fantasy, Sci-Fi, Space, Cyberpunk, Post-Apocalyptic, Zombies, Cosmic Horror, Folk Horror, Medieval, Pirates, Western, Historical, Modern City, Mundane Life/Office, Farming & Nature, Ocean, Animals, Mythology, Steampunk, Dreams/Surreal, Cooking & Food, Detective/Noir, Spooky-Cute.

- Each **genre × theme** pair has a hidden **fit value** from −2 to +2. Examples: Farming × Cozy +2, Ocean × Horror +2, Cooking × Party +2, Office × Simulation +2, Zombies × Survival +1 (but chronically saturated), Western × Puzzle −1.
- Fit is discovered by shipping games, researching, playing games in that combination, and post-mortems. Once found, it's permanently shown in the Knowledge base (17).
- Themes also have **trend cycles** (14) and **press angles** (09). *Mundane Life* is often a comedy angle; *Mythology* and *Historical* can qualify for heritage grants (03).

### 2.3 Art styles

| Style | Art cost ×, Animation cost × | Skill floor | Visual ceiling | Perf. load (1–5) | Store asset availability | Ages | Screenshot pop | Strong with | Weak with |
|---|---|---|---|---|---|---|---|---|---|
| **Pixel Art** | 0.8, 1.2 (frame-by-frame) | 3 | 8 | 1 | 3 | Slowly | 3 | Platformer, metroidvania, roguelike, cozy | Realism-dependent horror |
| **Hand-Painted 2D** | 1.4, 1.3 | 5 | 10 | 1 | 2 | Slowly | 5 | Narrative, metroidvania, cozy | Fast-iteration projects |
| **Minimalist** | 0.4, 0.5 | 2 (plus Design ≥4, or it looks cheap) | 6 | 1 | 1 | Slowly | 2 (3 with a strong hook) | Puzzle, strategy | Action spectacle |
| **Cartoon** | 1.0, 1.1 | 3 | 8 | 2 | 3 | Slowly | 4 | Party, cozy, sim | Grim tone |
| **Anime** | 1.4, 1.3 | 5 | 9 | 2 | 2 | Slowly | 4 | RPG, narrative, action | — |
| **Low Poly** | 0.7, 0.8 | 2 | 7 | 2 | **5 (asset-flip risk)** | Moderately | 2 | Survival, sim, strategy | Premium pricing |
| **Retro PS1** | 0.7, 0.8 | 3 | 7 | 1 | 2 | Slowly (nostalgia trends) | 4 in horror | Horror, weird games | Cozy |
| **Stylized 3D** | 1.3, 1.2 | 5 | 9 | 3 | 4 | Slowly | 4 | Action, adventure, cozy | — |
| **Voxel** | 0.8, 0.8 | 3 | 7 | 3 | 3 | Moderately | 3 | Sandbox, survival | Narrative |
| **Realistic** | **2.5, 2.0** (mocap helps) | **7** | 10 | **5** | **5 (asset-flip risk)** | **Fast** | 4, but 1 below the floor ("uncanny") | Horror, action | Cozy, Nimbus ports |

- **Skill floor:** below it, the Graphics pillar is capped and reviews say "cheap-looking" or "uncanny".
- **Ages:** graphics expectations drift upward every year (14). Realistic art loses a noticeable amount of its Graphics score each year; stylized art barely ages. This matters for long projects and for remasters.

### 2.4 Perspectives

| Perspective | Animation load | Level design cost | Camera/programming complexity | Perf. | Streamer appeal | Notes |
|---|---|---|---|---|---|---|
| 2D side view | Med | Med | Low | Low | 2 | Platformers, metroidvanias |
| 2D top-down | Med | Med | Low | Low | 2 | Roguelikes, cozy |
| Isometric | High (8-direction sprites) | High | Med | Med | 2 | Strategy, RPG, sim |
| 2.5D | Med | Med | Med | Med | 3 | Readable 3D with 2D gameplay |
| **First-person 3D** | **Low** (hands only: a real indie trick) | High | Med | Med-High | **4** (facecam reactions) | Motion-sickness complaints if the camera is poor |
| Third-person 3D | **High** (the character is always visible) | High | **High** (camera work) | High | 3 | Expensive but marketable |
| Top-down 3D | Med | Med | Low-Med | Med | 3 | Action, party |
| UI / text-driven | None | None | Low | Low | 1 | Management, narrative, card games |

### 2.5 Engines

| Engine | Strengths | Cost model | Asset store | Talent pool | Consoles | Risks |
|---|---|---|---|---|---|---|
| **Unify** | 2D and 3D generalist | Free until $200k revenue/year, then Pro $2,000/seat/year | Huge | Large | Needs Pro | **Pricing controversy events** (14) |
| **Unreel** | Best for 3D and realistic | Free until $1M lifetime gross per title, then **5% royalty** | Large | Medium-large | Excellent | Heavy hardware (slow builds without a workstation). Weak 2D. |
| **Forge** | Strong 2D, 3D improving over the years | Free, no royalty | Small | Small but growing | Through porting partners ($15–40k per platform, or a revenue share) | Missing features lead to workarounds (tech debt) |
| **Maker2D** | Fastest 2D prototyping | $100/year; console export $800/year | Small | Small | Export add-on | Scales poorly past Medium (complexity penalty) |
| **Custom engine** | Perfect fit, no fees | Research only (15) | None | None (onboarding −50% for 3 months) | Build it yourself | Big upfront cost; tech-debt risk; long-term advantage |

**Engine familiarity:** each person has a familiarity per engine (0–100%). Switching engines drops their output to ×0.7, recovering to ×1.0 over about 3 months. Switching is a real decision, not a menu toggle.

### 2.6 Platforms (details in 10, 11 and 12)

| Platform | Fee | Good for | Requirements |
|---|---|---|---|
| **Vapor** (PC) | 30% (25% after $10M, 20% after $50M per game) | Almost everything | Store page. Next Fest. Deck Verified badge (10). |
| **Epoch Store** | 12% | — | Small organic audience. Exclusivity deals and paid giveaways (12). |
| **scratch.io** | Developer-chosen (default 10%) | Jams, prototypes, tiny games | Tiny audience; pay-what-you-want |
| **Nimbus** | 30% | 2D, cozy, platformer, party | **Low performance budget.** Dev kit needs a shipped game or a porting contact. Certification. |
| **Vertex** | 30% | Action, horror, narrative | Strict certification. Dev kit. |
| **Axis** | 30% | Broad | Axis Pass deals (12). Dev kit. |
| **Orchard / Droid** (mobile) | 15–30% | Casual, free-to-play | Touch controls (design work). Premium games are a niche. |
| **Web portals** | Ad revenue share | Jam and tiny games | — |
| **Visor** (VR) | 30% | Niche but hungry | 3D only. Comfort requirements. |

### 2.7 Target audiences

| Audience | Size | Price tolerance | Best channels | Expects | Review harshness |
|---|---|---|---|---|---|
| Casual | Huge | Low | Clipz, mobile stores | Simple onboarding, short sessions | Forgiving on depth, harsh on friction |
| Cozy | Large | Med | Clipz, Chirp, cozy festivals | Charm, no stress, lots of content | Kind, but harsh on grind |
| Core | Large | Med | ViewTube, Threadit, Vapor | Solid gameplay, value | Medium |
| Hardcore | Small | High | Threadit, niche forums | Depth, difficulty, balance | Harsh on bugs and balance |
| Competitive | Med | Low (expects free-to-play) | LiveWire, esports | Fairness, netcode, updates | Brutal on netcode |
| Story Lovers | Med | Med | ViewTube essays, press | Writing, voice acting | Harsh on writing |
| Streamers & Viewers | Large reach | Low | LiveWire, ViewTube, Clipz | Spectacle, chaos, reaction moments | Short attention |
| Kids & Family | Large | Low-Med | Nimbus, parents | Age-appropriate, local co-op | Age rating matters |

A mismatch between audience and design produces predictable reviews: "Way too hard for a cozy game." "Too shallow for strategy fans."

### 2.8 Business model and price

- **Models:** Premium; Premium + DLC; Early Access → Premium; free-to-play with cosmetics; free-to-play with ads (mobile); subscription deal (12).
- **Price bands:** Free, $2.99–4.99, $9.99, $14.99, $19.99, $24.99, $29.99, $39.99+.
- **Value perception** = (quality × content hours) ÷ price, relative to genre norms. It drives player-review positivity, conversion and refunds (11). Too high: "overpriced". Too low (for Hardcore, Strategy or RPG): a slight "cheap" signal.

### 2.9 Scope

| Scope | Work (person-weeks) | Content | Comfortable feature count | Solo founder | Typical team and time | Typical budget |
|---|---|---|---|---|---|---|
| **Tiny** | 1–2 (≈ 15 Build sessions, ~8 named features) | 0.5–2 h | 2–3 | 2–4 weeks alongside gigs | — | < $2k |
| **Small** | 20–40 (≈ 300 Build sessions, ~60 features) | 3–8 h | 4–6 | About a year alongside gigs; out of reach with a full-time job | 1–3 people, 6–12 months | $5–60k |
| **Medium** | 150–500 | 8–20 h | 6–9 | Not realistic | 4–8 people, 18–30 months | $200k–$800k |
| **Large** | 800–2,000 | 20–40 h | 8–12 | Not realistic | 10–25 people, 2–3 years | $1.2M–$4M |
| **Huge** | 2,500–6,000 | 40–80 h | 10–16 | Not realistic | 30–60 people, 3–5 years | $5M–$15M |

Scope isn't a preset. It emerges from the content target plus the features; the bands are labels. *Tiny was resized (from 3–8 pw) when every work session became a minigame (02 §4), so a first game takes weeks, not months. Small briefly shrank too, but went back to 30 pw: a real indie game should take about a year of focused work, so that quitting your job matters (13 §12).* Budget ≈ person-weeks × ~$1,550 (a fully loaded mid-level person-week, 13) + assets + contractors + marketing (20–40%).

### 2.10 The Concept Forecast panel

This panel updates live as the concept changes:
- Work (range), duration with the current team (range), budget (range).
- Audience size (band), review expectation bar, **Complexity Rating (1–5)**.
- Performance load per platform, streamer appeal, hook clarity, marketability.
- **Risk flags**, for example "No one on the team has Networking skill", "Realistic art is above your art skill floor", "3 features with multiplayer interactions".

Every number is a range, and ranges narrow with Knowledge (17).

---

## 3. Features

### 3.1 Hygiene versus differentiators
- **Hygiene features** are cheap and *expected*. Missing ones cause specific review complaints, block badges (Deck Verified) or fail console certification. They only become a decision when budget is squeezed.
- **Differentiators** drive audience size, streamer appeal and virality, and add complexity, bugs and sometimes running costs.

### 3.2 Feature table

Work is in person-weeks at **Small scope** and scales with scope (content-heavy features scale strongly; systems scale weakly). Codes: **P** Programming, **D** Design, **A** Art, **An** Animation, **Au** Audio, **W** Writing, **N** Networking.

| Feature | Work | Complexity | Bug risk | Effects and notes |
|---|---|---|---|---|
| **Hygiene** | | | | |
| Controller Support | P1 D0.5 | 1 | Low | Required for consoles and Deck Verified. Action genres complain loudly without it. |
| Achievements | P0.5 D0.5 | 1 | Low | Completionist appeal. Minor complaint if missing. |
| Cloud Saves | P0.5 | 1 | Low-Med | Minor complaint if missing. Required on some platforms. |
| Settings & Rebinding | P1 D0.5 | 1 | Low | "No FOV slider or rebinding" complaints. Partial accessibility. |
| Tutorial / Onboarding | D1.5 P1 W0.5 | 1 | Low | −30% refunds for Casual and Core audiences. Complex genres expect it. |
| Robust Save System | P1.5 | 2 | Med | Prevents **save corruption** after patches (a catastrophic review event). |
| **Multiplayer** | | | | |
| Local Co-op | P3 D2 | 2 | Med | +Party and streamer appeal (couch play). Requires Controller Support. |
| Online Multiplayer (P2P) | P4 D2 **N8** | 4 | **Very High** (desync) | Audience ×1.2 in multiplayer-friendly genres. +Streamer appeal (friend groups). Player-population dynamics (11). Enables the three below. |
| Online Matchmaking | P2 N4 | 2 | High | Needed for competitive play with strangers. Small server cost. |
| Dedicated Servers | N6 P2 | 3 | High | **Running cost $0.05–0.15 per monthly active user.** Better experience, anti-cheat. |
| Crossplay | N5 P3 | 3 | High | Bigger population. Needs each platform's approval. |
| **Content and narrative** | | | | |
| Story Campaign | D3 W6 A2 Au1 | 2 | Med (progression blockers) | Raises the Story pillar's weight. More content hours. |
| Branching Dialogue | W5 D1 P1 | 2 | Med | +Story. Localization word count ×1.5. |
| Multiple Endings | W4 D2 A1 | 2 | Med | +Replayability. +Community theory videos (reach). Requires Story Campaign. |
| Voice Acting (partial / full) | Au2 W1 + $5–20k / Au4 W2 + $20–120k | 1 | Low | +Story and immersion. Dubbing multiplies localization cost. |
| Cinematics | A3 An4 Au2 W1 (+ mocap $) | 2 | Low | +Trailer quality, +Story. |
| **Systems** | | | | |
| Procedural Generation | P6 D4 | 4 | High (edge cases) | +Replayability. Content work ×0.7. −Handcrafted feel. Synergy with roguelike and survival. |
| Advanced AI | P6 D3 | 4 | High | +Gameplay depth (strategy, stealth, horror). CPU performance cost. |
| Physics Sandbox (funny physics) | P4 D1, An −1 (ragdolls replace animation) | 3 | High (glitches) | **"Funny Physics" virality trait.** Produces lucky bugs. |
| Crafting System | P2 D3 A2 | 2 | Med | Expected in survival. Item content scales with scope. |
| Progression / Skill Tree | P1.5 D2.5 A0.5 | 2 | Med (balance) | +Retention. |
| Base Building | P3 D3 A3 | 3 | High | +Engagement. +Player screenshots (organic reach). |
| Open World | P4 D6 A10, content ×1.5 | **5** | **Very High** | +Audience, but +expectations. Heavy on performance. |
| Dynamic Weather & Day/Night | P2 A2 Au1 | 2 | Med (performance) | +Atmosphere, +Graphics. Performance cost. |
| Character Customization | P2 D1 A6 An2 | 2 | Low-Med | +Engagement, +identity. Enables cosmetic DLC. |
| **Community and longevity** | | | | |
| Level Editor (player-facing) | P5 D2 A1 | 3 | High | +UGC, longevity, streamer challenges. |
| Mod Support | P6 D1 | 4 | High | +Longevity, +community, UGC virality. The *Modding SDK* research halves the cost. |
| Workshop Integration | P2 | 1 | Med | Requires Mod Support or Level Editor. +UGC reach on Vapor. |
| Leaderboards & Daily Challenges | P1.5 D1 N1 | 2 | Med | +Retention, +competitive streamers. Online required. |
| Streamer Integration (chat voting) | P2 D1 N1 | 2 | Med | **+Streamer appeal (large), +virality.** |
| Replay / Clip Sharing | P2 A0.5 | 2 | Med | +Organic virality. |
| Photo Mode | P1 A0.5 | 1 | Low | +Organic reach for visually strong games. |
| Pettable Animals | A0.5 An0.5 | 0 | Low | A tiny "Can you pet the dog?" meme bump and community goodwill. |
| **Reach and monetization** | | | | |
| Accessibility Suite | P1.5 D1.5 A0.5 | 1 | Low | +Audience, +press, +awards. Unlocks the accessibility grant. |
| Localization (per language) | First language P2 (text pipeline), then $0.08–0.15/word + 0.3 pw QA | 1 each | Med (text overflow; CJK fonts) | Adds that language's market share, which varies by genre (11). |
| Endless Mode / New Game+ | D1.5 P1 | 1 | Low | +Replayability and Content score, cheaply. |
| Cosmetic Microtransactions | P2 A4 + live ops | 2 | Med | +Revenue per player. −Player trust with premium audiences. Review risk. |
| Seasons / Battle Pass | P3 D2 A6 per season | 3 | High | Requires online. Creates content-cadence obligations. Trust risk. |

### 3.3 Complexity and feature interactions

- **Project Complexity** = feature complexity + scope factor + **interaction penalties**. It's shown as a **Complexity Rating from 1 to 5**.

| Rating | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Bug multiplier | 1.0 | 1.15 | 1.35 | 1.6 | 2.0 |
| Estimate variance | ±10% | ±15% | ±25% | ±35% | ±50% |

- **Interaction penalties:**
  - Online × Physics Sandbox +3 (syncing physics).
  - Online × Procedural Generation +2.
  - Online × Advanced AI +1.
  - Mod Support × Online +2 (cheating, version mismatch).
  - Crossplay × Mod Support +2 (console mod policies).
  - Open World × each of Weather, AI and Physics +1.
  - Procedural Generation × Story Campaign +1 (pacing).
- **Multiplier interactions:** Branching Dialogue × Localization = words ×1.5. Voice Acting × dubbed Localization = voice cost per language.
- **The lesson:** every feature looks cheap alone. Three that interact can double the bug count. The Concept Forecast shows the interaction penalties once the team has shipped a game using both features, or has a lead programmer with skill ≥6. Until then they appear as "unknown complexity".

---

## 4. Production pipeline

| Phase | Share of work | What happens | Gate (milestone review) |
|---|---|---|---|
| **Concept** | — | Choose parameters. The forecast is shown. | — |
| **Prototype** (optional, 04) | 1–4 pw | Buy information | Greenlight |
| **Pre-production** | 10–15% | Pillars, art direction, tech foundations. **Vertical slice** built. | **Vertical slice review:** reveals estimated final quality. Needed for publisher pitches, grants, trailers, showcases. |
| **Production** | ~60% | Features and content. Decision cards, bugs accumulating, playtests. | **Alpha:** feature complete. New features now cost ×1.5 (scope creep). |
| **Beta** | ~15% | Content complete. Balance, bugs, optimization, polish. | **Beta review:** new content costs ×2. |
| **Release candidate** | ~5% | Certification for consoles, day-one patch, final checks | Ship-or-delay decision (§14) |
| **Launch → Post-launch** | — | See 10 and 11 | Post-mortem (17) |

**The milestone review screen shows:**
- Pillar estimates as ranges, and bugs (known, plus an unknown estimate).
- Performance per platform, and the schedule and budget forecast against plan.
- Open playtest findings, team health, and the promises due.

Options: **Proceed / Extend the phase / Cut features / Pivot / Cancel.** Publisher milestones (12) sit on top of these gates.

---

## 5. Work model

**Disciplines:** Programming, Design, Art, Animation, Audio, Writing, Networking, QA. (A Technical Artist works across Art and Programming on performance. UI work is Design plus Art.)

**Output per person per week (in person-weeks):**

```
Output = (hours / 40)
       × (0.55 + 0.09 × skill)                    // efficiency: skill 2 → 0.73, skill 5 → 1.00, skill 10 → 1.45
       × Speed (0.8–1.2) × Morale (0.85–1.10)
       × (1 − 0.004 × max(0, Burnout − 30))
       × Tools/Hardware fit (0.8–1.15, depends on project type — 08)
       × Engine familiarity (0.7–1.0)
       × (1 − Coordination overhead)
```

- **Coordination overhead** is 0% for up to 3 people on a project, then +3% per extra person. It's reduced by a Producer, leads, project-management software and a meeting room, and capped at 35%. This is **Brooks's law**: adding people late makes a project later in the short term, because new hires also need onboarding (07).
- **Workstreams:** each discipline has a task backlog. People are assigned by primary skill by default and can be dragged across; generalists can split time.
- **Bottleneck / critical path:** the discipline with the most remaining work per unit of capacity sets the ship date. It's highlighted, and the player's fixes are to hire, outsource, buy assets or cut scope in that discipline.
- **Polish level per workstream:**

| Level | Time | Quality |
|---|---|---|
| Rough | ×0.8 | −1 |
| Standard | ×1.0 | — |
| Polished | ×1.4 | +1 |
| Perfectionist | ×2.0 | +1.5 (diminishing returns) |

- **Content units** carry per-unit costs from art style (§2.3), perspective (§2.4) and genre.

### 5.1 Features and Build sessions (the solo founder)

- **A project is a list of named features.** Each discipline's work is split into features of about 0.15 pw (Tiny) or 0.5 pw (Small), so a Tiny game has ~8 features and a Small game ~60. Examples: Programming "Player controls", "Collision", "Save system"; Art "Hero sprite", "Tileset"; Design "Core mechanic", "Forest level". Each discipline's core feature comes first, and genres add their own ("Jump physics", "Jump scares", "Puzzle set 1"). Features are a view of the workstream, built in order; they add no extra state.
- **A Build session is played as Wire It Up.** It's a cable puzzle (a random spanning tree on a 4×4 board for Tiny, 5×5 for Small): turn the tiles until power flows from the battery to every module of the feature. Each lit module is a named part ("Input", "Physics", "Shading"…). Score = 75% for the share of modules lit, plus up to 25% for finishing early. The score becomes the work multiplier (×0.5–×1.2), and careful sessions also raise task quality (02 §4).
- **The player picks the feature,** which is the old "develop focus" decision. The default is the bottleneck (the discipline with the most work left).
- **Estimates are shown in sessions** ("about 14 Build sessions"), still with the planning-fallacy bias of §6.
- **Bugs:** 16 per pw of programming and 4 per pw of other work (× skill and burnout factors). Polishing fixes 60 per pw. The full polish bonus needs 15% of the project's work (at least 0.1 pw). Polishing is played as **Bug Squash**.

## 6. Estimation and the planning fallacy

- **Displayed estimate = true work × optimism bias × noise.** Bias starts at **0.70**, so new studios estimate about 30% low. It improves by +0.03 per Production level and +0.04 per shipped project, capped at 0.95. Noise follows the Complexity Rating.
- The game **never announces this.** The first post-mortem shows: *"Estimate accuracy: we ran 41% over."* From then on, a "historical overrun" adjustment appears next to estimates: *"Adjusted: 38–52 weeks"*.
- The **Realistic Planning** perk halves the bias. A **Producer** shows adjusted estimates from day one.

---

## 7. Quality model

**Eight pillars, each scored 0–10:**
- **Core** (weighted by genre): Gameplay, Graphics, Audio, Story, Content, Innovation.
- **Penalties:** Performance, Stability.

**Task quality:**

```
q_task = clamp( S − max(0, Difficulty − S) × 0.5 + polish_bonus + tool_bonus , 1, 10 )
S = skill + genre knowledge (0–1.5, design/writing) + morale (−1…+0.5) − burnout (0…−2) − engine unfamiliarity (0…−1.5)
```

Difficulty comes from the art style's skill floor, feature complexity and scope.

**Each pillar is the work-weighted average of the tasks that feed it, then modified:**
- **Gameplay:** + the idea's hidden fun potential + playtest findings fixed + feature synergy − unfixed findings.
- **Graphics:** × style fit (art style × genre) × asset quality and consistency (06) × aging (14).
- **Content:** content delivered ÷ content expected for this genre and price.
- **Innovation:** hook strength + originality + novel feature combinations − Derivative − asset-flip − saturation − franchise fatigue.
- **Performance:** 10 × min(1, platform budget ÷ load), weighted by each platform's expected share of sales (§13).
- **Stability:** 10 − severity-weighted remaining bugs per hour of play (§12).

**Critic score (0–100):**

```
Base          = 10 × Σ genre_weight[p] × Pillar[p]              (six core pillars)
Score         = Base × (1 − min(0.35, bug_severity_index))
                     × (1 − min(0.25, perf_shortfall × genre_perf_sensitivity))
                − 0.6 × max(0, Expected − Base)                  (the expectation gap)
                ± critic personality bias (up to ±6)
```

**Expected quality** (the bar) = baseline for price and scope + **hype** (09) + **studio fame** (14) + the previous game's score in the **franchise** (11). Overhyping, raising your price, getting famous and making sequels all raise the bar.

**Player reviews (% positive)** are a logistic function of Gameplay, **value** (quality × content ÷ price), Stability, Performance *on the player's hardware*, whether the game matches its marketing and tags, and a Community Trust bonus. Details in 11.

---

## 8. Development decision cards

The brief's "custom versus premade" and "rewrite versus keep" choices become a **deck of contextual cards**.

- **Structure:**
  - Context and requester (an employee, or your own thought).
  - 2–4 options, each with cost, time and effects. Some effects are **uncertain**.
  - A deadline: if ignored, the card auto-resolves using the delegate's preference.
  - A delegation option.
- **Frequency:** about one card every 2–4 weeks per project. More in pre-production and at milestones. Frequency scales with team size and complexity.
- **Uncertainty:** shown as Low/Medium/High risk labels. Exact odds appear if the requester's skill is ≥7, the founder's Production is ≥6, or the Architect perk applies.
- **Delegation** (the anti-repetition valve): leads resolve cards according to their personality.
  - Perfectionist → quality; Pragmatist → time; Frugal → cost; Visionary → innovation.
  - The player sets a policy, for example *"Ask me about anything over $10k or 3 weeks"*, and reads a weekly digest of delegated decisions.

| # | Card (trigger) | Options (cost / time / effects) | Hidden twist |
|---|---|---|---|
| 1 | **"How detailed should the environments be?"** (pre-production, art-heavy; from the brief) | **Simple stylized** $2,000 / 2 weeks / performance excellent / visuals medium · **Detailed hand-painted** $7,500 / 6 weeks / good / high · **Realistic** $20,000 / 12 weeks / heavy / very high | Realistic below skill floor 7 risks "uncanny". Hand-painted strongly raises screenshot pop. |
| 2 | **"Our programmer wants to rewrite the inventory system."** (debt > 30; from the brief) | **Rewrite** +$8,000 / +3 weeks / lower bug risk / better performance / debt −25 · **Keep** $0 / no delay / higher bug risk / debt +10 | 25%: the rewrite takes 5 weeks. 10%: it uncovers deeper problems (more debt revealed). |
| 3 | Music approach | Asset-store music ($200; recognizable) · Composer **buyout** ($6–15k; you own the soundtrack) · Composer **royalty deal** ($2–5k + 50% of soundtrack revenue; they own the rights) · **Licensed tracks** ($10k+) | Licensed tracks cause **streamer DMCA strikes** unless a streamer mode is added. |
| 4 | Voice acting scope | None · Key scenes ($8k, 2 weeks) · Full ($40k+, 6 weeks; multiplies dubbing cost) | Unskilled direction (Audio < 4) risks "wooden acting" complaints. |
| 5 | Playtest found a difficulty spike | Rebalance (1 week) · Add difficulty options (2 weeks; +Accessibility) · Keep it ("the hardcore will love it") | Keeping it splits reviews unless the audience is Hardcore. |
| 6 | Netcode approach | P2P with host migration (cheap; cheating) · Dedicated servers (+running cost; better) · Rollback netcode (+4 weeks; best feel) | P2P combined with Physics Sandbox makes desync bugs spike. |
| 7 | Save system | Robust and versioned (2 weeks) · Simple | Simple saves risk save corruption after a patch: catastrophic reviews. |
| 8 | Tutorial | Full (3 weeks) · Contextual tips (1 week) · None | None raises refunds from Casual and Core players. |
| 9 | Levels | Procedural (tools investment) · Handcrafted (content cost) · Hybrid | Procedural without research: edge-case bugs. |
| 10 | "An engine upgrade is available." | Upgrade (new features, 2–4 weeks of migration bugs) · Stay | Staying eventually costs support and features. |
| 11 | **"The designer wants to add a crafting system."** (scope creep) | Add (+6 pw, +complexity, creative morale +) · Prototype it first (1 pw) · Decline (designer morale −5) | It can be great: 30% chance of +Gameplay synergy. |
| 12 | Optimize before the demo? | Optimize (2 weeks) · Ship the demo as is | Performance complaints in the demo hurt festival results. |
| 13 | New artist's style drifts from the old art | Retrain (1 week) · Redo old assets in the new style (4 weeks, +quality) · Accept | Accepting costs consistency. |
| 14 | Middleware | Buy physics, dialogue or netcode middleware ($3–25k, saves 4–10 pw) · Build it (own the tech, reusable) | Vendor abandonment event years later. |
| 15 | "The producer suggests cutting a feature." | Cut (refunds the remaining work) · Keep | If it was promised publicly, a promise breaks (09). |
| 16 | The trailer would show unfinished visuals | Use them now (hype now) · Wait 4 weeks | "Not representative" backlash risk. |
| 17 | Streamer mode / chat integration | Add (+2 pw; +streamer appeal) · Skip | — |
| 18 | Cosmetic DLC at launch | Yes (+revenue) · Post-launch · No | "Day-one DLC" backlash, depending on the audience. |
| 19 | **A lucky bug:** playtesters love a physics glitch | Keep it as a feature (+Viral, −polish) · Fix it | Kept glitches can become the game's meme. |
| 20 | Gore level (horror) | Full (mature rating, press angle, some creators can't monetize) · Stylized · None | Affects platform ratings and ad eligibility. |
| 21 | Localization method | Machine ($200 per language; "bad translation" risk) · Professional (~$0.10/word) · Community (free, slow, trust +, variable quality) | — |
| 22 | Accessibility pass | Full (2 weeks: +awards, +audience) · Basic (0.5 week) · None | — |
| 23 | "Can we pet the dog?" | Add (+0.5 pw) · No | Tiny social buzz and goodwill. |
| 24 | Nimbus port performance | Reduce effects (visuals −) · Dynamic resolution (+2 weeks) · Ship it | Shipping risks the "unplayable on Nimbus" meme. |

---

## 9. Scope creep

- **Sources:** employee pitches, community requests, publisher asks, rival features, your own inspiration, and legitimate playtest findings.
- **Cost:** a feature added after pre-production costs its work ×1.25 (integration), ×1.5 after Alpha, ×2 after Beta, plus complexity.
- **The Scope Creep meter** (visible) is the percentage of work added after pre-production.
  - Above 20%: producers' morale drops and estimate variance grows.
  - Above 40%: risk of the **"Development hell"** event chain (repeated re-plans, burnout, a publisher warning).
- **Upside:** creative staff morale rises, and a good feature raises quality and innovation. Scope creep is a temptation, not a pure trap.

## 10. Technical debt

- Tracked **per codebase**: each project's code, and reusable tech in the library. Range 0–100.
- **Grows from:** "keep it" choices, Rough polish on programming, crunch (+0.5/week), code by people with skill below 4, missing code review or automated testing, and rushed features.
- **Effects:**
  - Bug rate × (1 + debt/100).
  - Feature speed × (1 − debt/250).
  - Patches, ports and **sequels built on the same tech** cost more: **debt carries over**.
- **Paying it down:** refactor tasks (pw), rewrites (decision cards), or a "Tech week" policy (10% of programmer time).
- **Visible** as Code Health (Excellent / Good / Fair / Poor / Critical). The exact number shows with a lead programmer at skill ≥7 or the Architect perk.

## 11. Crunch

| Policy | Hours | Nominal output | Bugs | Burnout/week | Morale |
|---|---|---|---|---|---|
| Normal | 40 | 100% | ×1.0 | −2 (recovery) | — |
| Overtime | 50 | +20% | ×1.3 | +3.5 | −1/week |
| Crunch | 60+ | +35%, **then decays** | ×1.7 | +7 | −3/week |

- **The hidden truth:** after 4 weeks, crunch's real bonus decays and reaches **−10% by week 8.** The UI shows *nominal* output until a Producer is hired or the founder reaches Production 5. Then the "Actual output" chart reveals the decay. Discovering this is part of learning the game.
- **Voluntary or mandatory:** "Ask for volunteers" (a Leadership check; smaller morale loss, fewer people) versus mandatory.
- **Overtime pay or bonuses** reduce the morale loss for money. **Time off in lieu** afterward reduces burnout.
- **Reputation:** 3 crunch periods (over 4 weeks each) within 2 years earn the **"Crunch Studio"** tag (14): lower Employee Reputation and more turnover.

## 12. Playtests and findings

| Playtest | Cost | Reveals | Bias or risk |
|---|---|---|---|
| Internal (team) | 1 block per person | Gameplay issues | Team bias ("we're used to it") |
| Friends & family | Cheap | Broad feel | Positive bias |
| Community closed beta | Needs a community | Broad issues + bugs (free QA) | Leak risk |
| Paid external agency | $2–10k | Unbiased findings + metrics | — |
| Early Access (11) | — | Everything, publicly | Reviews are permanent |

- **Findings** are named problems: *"Combat feels floaty"*, *"Level 3 difficulty spike"*, *"UI is confusing"*, *"Co-op desyncs"*. Each has a hidden severity (revealed by repeated tests or skill) and a fix task. Fixed findings raise their pillar. **Unfixed findings show up in reviews** almost word for word.
- Each playtest also narrows the Gameplay estimate.

## 13. Bugs and QA

- **Generation per person-week of work:**

```
bugs = 1.0 × complexity_mult (1.0–2.0) × (1.5 − 0.1 × skill) × pace (1.0 / 1.3 / 1.7)
         × (1 + 0.01 × max(0, burnout − 30)) × (1 + debt/100) × practices (code review ×0.8; automated tests ×0.85 on regressions)
```

- **Severity mix:** Minor 70%, Major 22%, Crash 6%, Critical 2% (save corruption, progression blockers). Networking work generates **Multiplayer bugs** (desync, disconnects) that count double. Performance is tracked separately (§14).
- **Discovery:** bugs are hidden until found. Each week, QA finds `unknown × (1 − e^(−QA_effort / (k × project_size)))`, so the last bugs are the hardest to find. QA effort = QA hours × skill × tools (automated tests, test devices, crash reporting).
- **Display:** known bugs by severity (**V**) and *"Unknown: est. 40–120 (QA coverage 35%)"* (**E**).
- **Fixing** (programmer hours at skill 5): Minor 1, Major 4, Crash 10, Critical 20. Each fix has a 12% chance of a regression (6% with automated tests).
- **At launch,** players find bugs fast: 80% of the remaining Crash and Critical bugs in week 1. Bugs drive review complaints, refunds and "broken launch" news (11).
- **Console certification** fails if Crash bugs exceed a threshold or platform rules are broken (save handling, controller disconnects). Result: 2–4 weeks' delay plus a resubmission fee.

## 14. Performance

- **Budget per platform:** PC minimum spec 100, Vapor Deck 60, Nimbus 40, Vertex/Axis 120, mobile 35.
- **Load** = art style load × content density + feature loads (Open World, Physics, AI, Weather) + engine overhead.
- **Optimization tasks** (Programming, Technical Artist) reduce load.
- **Score per platform:** Runs great / OK / Poor / Unplayable. Poor performance brings complaints and refunds. On Nimbus it can become a meme.

## 15. Ship or delay?

When a release date arrives (or a milestone review predicts a miss), the **Ship or Delay** screen appears. Forecasts are shown as ranges, for example *"Review score if shipped now: 58–71; after a 6-week delay: 66–78."*

| Option | Cost | Effect |
|---|---|---|
| **Release as is** | — | Known risks go live |
| **Delay N weeks** | Burn rate × N; publisher penalties; wishlists go staler; the new date may collide with big releases | Higher quality |
| **Cut features** | Promise breaks if the features were announced | Hit the date |
| **Early Access instead** | Permanent first impressions | Revenue and feedback now (11) |
| **Crunch** | Burnout, morale, bugs | Output now |

**How a delay is communicated matters:**

| How you delay | Trust effect |
|---|---|
| Announced 4+ weeks ahead with a reason | −1 |
| Announced in the final 2 weeks | −4 |
| Silent slip | −8, and risk of the "Unreliable dates" tag |

A transparent delay followed by a great launch earns **"Worth the wait"**, a trust bonus.

## 16. Cancel, pivot or shelve

| Action | Cost | What survives |
|---|---|---|
| **Cancel** | Sunk cost. Team morale −15 (creative staff −25). Publisher clauses apply. | Assets and tech go to the library; knowledge goes to post-mortem notes. |
| **Pivot** | Partial rework | Tech ~60%, art 30–80% (if the style is kept), design insight. Can rescue a doomed project, for example after a rival announcement (14). |
| **Shelve** | Frozen | Can resume later, but 10% of work is lost per 6 months (familiarity fades). |

---

## Connections
- **Feeds:** review scores and sales (11), store page assets and trailer quality (10), streamer appeal and virality (09), performance-driven refunds (11), technical debt into sequels (11), knowledge (17), employee XP and morale (07).
- **Fed by:** ideas (04), people (07), assets and outsourcing (06), hardware and software (08), research (15), market expectations and trends (14), hype and promises (09), funding and milestones (12).

## Visibility
- **V:** parameters, features, assigned people, known bugs, the Scope Creep meter, Code Health band, phase, budget spent.
- **E:** pillar estimates, work and duration (with calibration), unknown bugs, performance per platform, the expected review bar, decision-card risks.
- **H:** true fun potential, genre × theme fit (until discovered), exact card odds (without skill), interaction penalties (without experience), crunch decay (without a Producer).
