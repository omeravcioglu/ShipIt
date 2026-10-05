# 16 — Progression, Goals, Failure & Replayability

How the studio grows, how the game reveals its systems gradually, why there's always another goal, what happens when things go wrong, and why the 30th hour doesn't feel like the 3rd.

## Decisions
- **When to scale up,** and when staying small is the smarter play.
- **Which Ambitions to pursue:** the player defines what "winning" means for this studio.
- **How to handle a crisis:** which sacrifice hurts least.
- **When to stop:** retire, sell, or keep going.

---

## 1. Studio tiers

Tiers are earned by **combinations** of achievements, never by money alone.

| Tier | Requirements | What unlocks | What changes |
|---|---|---|---|
| **1. Bedroom Developer** | Start | The day's three slots, work minigames, gigs, ideas, Tiny/Small projects, social posts, scratch.io, Vapor store page | Personal survival. Money and hours. |
| **2. Small Indie Team** | Incorporated + ≥1 employee or co-founder + ≥1 commercial release | Hiring, contracts, publishers, grants, crowdfunding, coworking and small offices, culture policies | Payroll pressure. First management. |
| **3. Established Indie Studio** | ≥5 staff + an office or a remote-first setup with a Producer + ≥3 releases + $500k lifetime net revenue + Developer Reputation ≥ "Respected" | Leads, **founder role choice** (02), VC and angels, console dev programs, bigger events, franchises | Coordination. Post-launch portfolio. Team health. |
| **4. Successful Studio** | ≥15 staff + 2 concurrent projects + (a game with ≥250k units **or** an award) + $5M lifetime revenue | Multiple teams, acquisitions, **publishing label**, licensing, large offices | Delegation. Portfolio risk. Rivals and scrutiny. |
| **5. Major Independent Developer** | (≥40 staff **or** a label with ≥5 external titles) + (a franchise with ≥3 entries **or** a 1M-unit game) + $30M lifetime revenue + **independence** (founder controls ≥50%, or employee-owned) | Legacy options, IPO path, industry influence events, acquisition offers | Stewardship. What does the studio stand for? |

Tier promotion is a celebrated story moment (a studio-history entry, a small party event, press) and changes the event deck.

## 2. Unlock schedule (progressive disclosure)

The game never shows every system on day one. Systems appear **when the player first needs them**, usually introduced by an event, so complexity grows at the pace of understanding.

| System | Appears when |
|---|---|
| The day panel, work minigames, gigs, runway, Idea Notebook, the chapter goal, Doug's tutorial | Day 1 |
| Social posting (Chirp) | Week 3 |
| Game jams | First jam announcement (weeks 5–6) |
| Store page, pricing, Launch Week | First project reaches 60% |
| Post-mortems, Knowledge base | First release |
| Patronly | 250 followers |
| Festivals (Next Fest) | Store page live + a demo possible |
| Creator outreach | 500 followers **or** a store page live |
| Incorporation | First publisher interest **or** $20k revenue **or** the player asks |
| Hiring | Incorporated, **or** co-founder events |
| Contracts | Tier 2 (solo founders: a craft skill ≥5 and a 4★ rating) |
| Publishers and negotiation | Vertical slice + incorporated |
| Grants | Incorporated (small prototype grants earlier) |
| Crowdfunding | Vertical slice + 500 engaged followers |
| Offices and rooms | 3 people |
| Research tree | First employee |
| Culture policies | 4 employees |
| Early Access | Second project |
| Consoles | A platform-program invite, a porting contract, or Tier 3 |
| Investors | Tier 2 (angels), Tier 3 (VC) |
| Multiple teams | Tier 4 |
| Publishing label, acquisitions | Tier 4 |

## 3. Pacing targets

| Phase (the brief's pacing) | In-game years | Real playtime (par) | Dominant activity |
|---|---|---|---|
| Solo developer survival | 1–2 | 3–6 h | Spending each day by hand: gig and build minigames, the first Tiny game (about an hour in), then a Small one |
| Small team | 2–4 | 3–5 h | First hires, a real project, the first publisher or grant |
| Established indie studio | 4–7 | 4–7 h | Office, catalog, franchise seeds, post-launch operations |
| Multiple projects, larger productions | 7–12 | 5–8 h | Two teams, delegation, awards, rivals |
| Major independent | 10+ | 3–6 h + sandbox | Portfolio, label, legacy |

**Total: about 17–30 hours** for the main arc at par pace. Fast players who take big risks can compress it. Careful or unlucky players stretch it, and that's intended.

The early-game numbers are validated by `Tools/economy_sim.py` (13) and the bot balance tests: a balanced solo player ships a Tiny game within the first month and a Small game around the end of the first year (months 11–17), and ends year three with ~$30k, enough to hire. Keeping a full-time job never ships the Small game in that time.

## 4. "There's always another goal"

### 4.1 Story chapters: the goal you can always see

The HUD always shows the current **chapter**: one clear goal, broken into steps. A chapter completes when all its steps are true (they're checked against the game state, so you can do them in any order). Completing one posts Doug's congratulations (morale +8) and reveals the next.

| Chapter | Goal | Steps |
|---|---|---|
| **1. The Bedroom** | Ship your first game | Deliver your first gig · Start your first game · Build every feature · Open a store page · Launch it |
| **2. Finding Players** | Find your players | Read your first post-mortem · Reach 150 followers · Open a Vapor store page · Earn $500 from your games |
| **3. A Real Indie Game** | Ship a Small game | Start a Small project · Reach 500 wishlists on it · Launch it · Get 70% positive reviews |
| **4. Full-Time Indie** | Live off your games | Reach 1,000 followers · Have a month where game sales pay all the bills · Earn $10,000 from your games |
| **5. To Be Continued** | Keep growing | None yet: hiring, publishers and a real studio arrive with later milestones (M3+) |

Chapters are the spine of the first hours: each one teaches a part of the business (survive, find players, ship something real, live off it). The side goals below run alongside them.

### 4.2 Goal layers

Four goal layers are visible on the **Goals board** at all times:

| Layer | Source | Examples | Reward |
|---|---|---|---|
| **Objectives** (short-term, auto-generated from state) | The situation | "Reach 1,000 wishlists", "Pay rent without dipping below $500", "Fix the crash bug before Friday", "Hire a QA tester" | Small XP, morale, sometimes a hint |
| **Milestones** (tier progress) | Tier requirements | "Ship 3 games", "$500k lifetime revenue" | Tier promotion |
| **Ambitions** (player-chosen, 3 active slots) | Chosen from a list, and new ones unlock | See below | **Legacy points**, story events, perks |
| **Personal goals** (founder) | The personal layer | "Take a real vacation", "Mentor a junior to Senior" | Morale, relationships |

**Ambition examples** (many from the brief):
- Reach 10,000 wishlists. Get a publisher. Build a community of 10k.
- Move into a real office. Win an award. Reach 1 million sales. Create a franchise (3 entries).
- Become financially independent (36 months of runway with no new releases).
- Make a horror masterpiece (ScoreHub 90+ in Horror). Never crunch. Stay 100% independent.
- Build a publishing label. Ship on every platform. Make a game that goes viral. Buy back an IP.
- Hire your jam teammate. Beat your rival's review score. Keep every promise for 5 years.

Ambitions let players **author their own playthrough**, and they create meaningful conflicts: *Never crunch* versus *Ship before the rival*.

### 4.3 Achievements (the platform's badges)

Achievements are Steam's badges (Steam/README.md). They aren't goals the game pushes. They remember the
player's story, and they nudge toward the parts of the game people might skip.

**The four kinds:**

| Kind | Examples |
|---|---|
| The first steps | Name your studio · Deliver a gig · Finish a feature · Ship · Sell a copy · Open a Vapor page |
| The road | Release a Small game · 1,000 wishlists · 1,000 copies · Very Positive · Live off your games |
| Getting good | Three stars in a session (and in 50) · Full flow · Level 5 in a craft · A 5/5 client · Finish a jam |
| Life in the bedroom | Two coffees in a day · Pet the cat on 10 days · A week of vacation · Quit a day job · Climb back to $5,000 from debt |

There are also two hidden achievements, for the stories nobody plans: collapsing from burnout, and shelving a
project.

**Rules:**
- **Every achievement is a pure check of the game state** (`AchievementCatalog`). It works offline, and a loaded
  studio re-sends what it earned.
- **No grind.** The counters are small (10 cat days, 7 vacation days, 50 three-star sessions) and come naturally
  in a long game.
- **Never rewarded in the game.** An achievement changes nothing in the simulation, so it can't distort the
  game's decisions.
- **Moments count.** "Two coffees in a day" and "full flow" are unlocked when they happen, not checked later.

## 5. Failure and recovery

**The rule: failure opens hard choices before it ends anything.**

**Warning signs** (escalating, always visible): runway under 3 months (amber), under 1 month (red), payroll at risk, covenant breach risk, a morale collapse, a publisher warning.

**Crisis mode** triggers when cash can't cover the next month's obligations. The game pauses and shows a **Crisis Table**:

| Option | Immediate effect | Long-term cost |
|---|---|---|
| **Emergency loan / credit card** | Cash now | High interest; money anxiety; credit score |
| **Sell company shares** (angel) | Cash now | Ownership, control, investor expectations |
| **Cancel a project** | Stops the burn | Sunk cost, morale, promises broken |
| **Lay off employees** | Lower burn | Morale, Employee Reputation, named people (07) |
| **Pay cuts / 4-day week** | Lower burn | Loyalty test (who stays?) |
| **Return to freelancing** (studio hibernation) | Founder income; the studio pauses | Team laid off or on leave; the IP and community are kept for a comeback |
| **Day job** | Stable income | Development slows to a crawl |
| **Sell an IP** | A lump sum | You lose the franchise forever (and its sequels) |
| **Accept a bad publisher deal** | Funding | Revenue share, rights, control (12) |
| **Emergency crowdfunding** | Maybe money | Public pressure, promises |
| **Contract pivot** | Steady cash | The "Service Studio" drift |
| **Ask family** (once) | A small bridge | Relationship stakes |
| **Sell equipment / sublet the office** | Some cash | Productivity, space |
| **Sell the studio** | An exit | The end of *your* studio (an ending) |

**Bankruptcy** happens only when debts exceed recoverable options for 3 consecutive months, or when the player chooses to close.
- **Sole proprietor:** personal bankruptcy. A hard restart.
- **Incorporated:** the company closes. The founder keeps their skills, contacts and some reputation.

**The Phoenix restart:** after closing a studio, the player can start a new one in the same world:
- The founder keeps their skills and part of their network.
- The world remembers the old studio (a "Second act" story angle, with some trust carried over or damaged).
- The *"The Phoenix"* achievement is available.

**Struggle creates stories:**
- The studio that laid off half its team and shipped a hit a year later.
- The founder who went back to freelancing for 8 months and returned with a jam game that went viral.

The history log records all of it (§7).

## 6. Endings and the long game

- **Retire:** at any point after Tier 3. The founder hands the studio to a successor (a named senior employee) and gets a **Legacy summary**.
- **Sell the studio:** acquisition offers at Tiers 4–5. Accept for an ending, or continue as a subsidiary with constraints.
- **IPO path** (Tier 5, if VC-backed): quarterly-results pressure as a new late game.
- **Keep going:** sandbox after year 15, with the market and rivals still evolving.
- **The Legacy score** is a multi-dimensional summary, not one number: games shipped, best reviews, total players, awards, people mentored, promises kept, independence, franchises, community size.

## 7. Studio History (the story artifact)

- An automatic **timeline** of releases (with review scores and sales), hires and departures, crises and decisions, awards, rivals, promises and quotes from reviews.
- Exportable as a shareable "Studio Story" page. Every playthrough leaves a readable narrative, which is the brief's "every playthrough creates different stories" made tangible.

## 8. Game modes and difficulty

| Mode | Description |
|---|---|
| **Career** (default) | The brief's premise: $5,000, a laptop and a tiny apartment |
| **Scenarios** | *Second Chance* (start with $15k of debt and a failed game), *Jam Game Gone Viral* (a viral prototype, no team, no money), *Inherited Studio* (take over a declining 12-person studio), *Publisher Darling* (big funding, big obligations), *Remote Collective* (a co-op of 4 founders) |
| **Sandbox** | Custom starting money, market settings, events toggle |
| **Ironman** | One save, no reloading |

**Difficulty sliders:** market harshness (sales multiplier), living costs, event frequency and severity, **information transparency** (see Analyst mode, 17), personal events (02).

## 9. Replayability and anti-repetition

| Strategy | How it works |
|---|---|
| **Problems change with scale** | Each tier brings new dominant decisions (§1), and old ones become delegable or automated. |
| **A moving market** | Trends, saturation, platforms, ad prices and graphics expectations drift (14). Last year's formula decays. |
| **Delegation climbs the ladder** | Leads handle decision cards in their own style; managers run marketing plans; the player sets policy (05, 07, 09). |
| **Projects differ in kind** | A narrative game (writing, voice), a live multiplayer game (servers, population), a port contract (technical), a mobile test (paid-marketing math), Early Access (cadence and promises). Each exercises different systems. |
| **People and rivals** | Hidden traits, relationships and event chains mean no two teams play the same way (07, 14). |
| **Launches are climaxes** | Launch Week is a live, high-stakes set piece (10). |
| **Seeded worlds** | Rivals, creators, publishers, trends and events are generated from a seed. Seeds can be shared. |
| **Backgrounds and Ambitions** | Different starts and self-authored goals (02, §4). |
| **Streamlining repeated actions** | Templates (week plans, project presets, marketing plans), "repeat last", auto-renewing gigs, and delegation thresholds keep the cost of repetition near zero. |
| **Knowledge as progression** | The player's own understanding grows. Estimates narrow, post-mortems explain. The game gets *deeper* on later playthroughs instead of repetitive. |

---

## Connections
- **Feeds:** what appears on screen and when (18), event decks (14), goals shown on the Dashboard (18).
- **Fed by:** every system's milestones and states.

## Visibility
- **V:** tier requirements and progress, all goal layers, the crisis table with each option's immediate effects.
- **E:** the long-term cost of each crisis option (shown as risk labels).
- **H:** future scenarios. Some Ambitions unlock on hidden conditions, as surprises.
