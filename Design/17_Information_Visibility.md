# 17 — Information & Visibility

The brief asks what should be visible and what should be hidden. The answer is the backbone of the pillar **"Success is learned"**: the simulation knows the truth; the player sees **evidence** whose quality they can improve.

## Principles

1. **Show every cause and every outcome. Hide the exact mapping between them, and let the player learn it.** The player always sees what they chose and what happened. What stays hidden is the formula that connects the two, and that's revealed through investment and experience.
2. **Be honest about uncertainty.** Estimates are shown as ranges with confidence levels. No false precision, and no silent secrets.
3. **Hidden doesn't mean random.** Hidden values are consistent, so players can learn them across projects and playthroughs.
4. **Explain after the fact.** Post-mortems decompose outcomes, turning results (good or bad) into knowledge.
5. **Better information is a reward you can buy** with skills, hires, tools, research, network and time.

## 1. Visibility classes

| Class | Meaning | Display |
|---|---|---|
| **V: Visible** | An exact, known fact | A plain number |
| **E: Estimated** | A true value exists; you see a range whose width depends on your knowledge | Range + confidence ("74–86%, medium"), a band ("High"), or a fan chart |
| **H: Hidden, discoverable** | A fact you don't know yet but can learn | 🔒 with a tooltip saying what would reveal it |
| **I: Internal** | Dice rolls, formulas, exact probabilities (unless a skill reveals them) | Never shown, except in Analyst mode (§7) |

## 2. How information is revealed

| Source | Reveals | Examples |
|---|---|---|
| **Experience** (shipping, post-mortems) | Genre × theme fits, estimate calibration, audience preferences, rules of thumb | "We ran 41% over": estimates are adjusted from now on |
| **Skill and judgment** | Narrower ranges in a discipline, up to **your skill + 2** (02, 07) | An Art 2 founder can't judge an artist's portfolio precisely |
| **Roles** | Producer: true schedule and crunch decay. Lead Programmer: code health, bug estimates. Data Analyst: analytics. Community Manager: sentiment detail. HR: morale detail. Finance: forecast bands. | Hiring a Producer shows the "Actual crunch output" chart |
| **Tools** (08) | Analytics (retention funnels, quit points), crash reporting, GameStatz (market data), A/B tests, attribution | "Half of players quit in the tutorial's second room" |
| **Research** (15) | Influencer Database, Optimization Profiler, Market Research Methods, Legal Review | Creator genre preferences become visible |
| **Network gossip** (14) | Publisher honesty, rival plans, vendor reliability, typical clauses | "Vortex pays 90 days late. Everyone knows." |
| **Prototypes and playtests** (04, 05) | Fun, technical difficulty, market interest, findings | "Players scream when the lighthouse turns." |
| **Time** | Employee traits, vendor reliability, creator conversion | A new hire's *Perfectionist* trait appears after 3 months |

## 3. The Knowledge base ("Studio Wiki")

The player's growing understanding, made concrete:

| Section | How it grows | What it does |
|---|---|---|
| **Genre knowledge** (0–100 per genre) | Ship +25, prototype +5, play games +2, research +5 | Design quality +0 to +1.5 in that genre; narrower estimates |
| **Genre × theme fits** | Discovered through releases, prototypes, research and post-mortems | Fits shown permanently (for example Ocean × Horror: great) |
| **Market knowledge** | Research actions, GameStatz | Accuracy of the Market Report. **Decays over ~6 months.** |
| **Audience knowledge** | Analytics, reviews, community | Segment preferences (price tolerance, expected features) |
| **Tech knowledge** (per feature) | Shipping games that use the feature | Feature bug rate −5% per shipped use (up to −25%); reveals interaction penalties (05) |
| **Rules of thumb** | Unlocked by *experiencing* the phenomenon | "Launch-month buyers ≈ 10–15% of wishlists." "Crunch stops helping after ~4 weeks." "Hook-first screenshots convert better." "November launches are crowded." |
| **Contacts' intel** | Gossip | Notes on publishers, vendors and rivals |
| **Lessons** | Post-mortems (§5) | Permanent studio practices |

## 4. How estimates are displayed

- **Ranges with confidence:** "Gameplay potential 74–86% (medium confidence)".
- **Qualitative bands:** Very Low → Very High, for things like streamer appeal or poaching risk.
- **Fan charts** for forecasts (cash, wishlists, sales): a solid line for the median and shaded bands for the uncertainty.
- **"?"** when there's no information at all.
- **🔒 locked** facts, with a tooltip naming what would reveal them: *"Hire a Producer or reach Production 5 to see actual crunch output."*
- **"Why?" breakdowns** (18): every important number lists its known contributors plus an **"Unknown factors"** slice. That slice shrinks as knowledge grows, which makes the player's learning visible.

## 5. Post-mortems and Lessons

The conversion of results into knowledge. Every release gets one at **day 30**, and optionally at **6 months**.

**Contents:**
1. **Outcome versus forecast:** sales, reviews, wishlist conversion, revenue.
2. **Quality decomposition:** each pillar with its contributing factors revealed. For example: *"Graphics 6.1: art skill +, Pixel × Horror style fit −0.4 (now known), asset consistency −0.6."*
3. **Market fit:** the genre × theme fit revealed, the trend phase at launch, saturation, launch-window competition.
4. **Marketing attribution:** full with tools, partial without.
5. **Estimate accuracy:** "We ran 41% over."
6. **Review themes mapped to causes:** "*Performance complaints (32%)* ← Realistic art on a cheap laptop pipeline, no optimization pass."
7. **What went right and wrong:** auto-generated bullet points.
8. **Lessons:** 2–4 options **derived from what actually happened.** The player internalizes 1–2 as permanent studio practices:

| Lesson (example) | Trigger | Permanent effect |
|---|---|---|
| **Performance Budget from Day One** | Performance complaints | Optimization tasks auto-scheduled; performance issues −15% |
| **Test Multiplayer Early** | Multiplayer bug wave | Multiplayer bug discovery ×1.5 in Alpha |
| **Hook First** | Low store conversion | Screenshot-selection hints; hook-clarity preview |
| **Buffer Your Estimates** | Schedule overrun | Estimates auto-adjusted +15% |
| **Don't Launch in a Crowd** | A crowded launch window | Window score always shown |
| **Listen to Playtesters** | Unfixed findings showed up in reviews | Findings severity visible after one test |
| **Promise Less** | Broken promises | Promise-making shows its trust risk in advance |
| **Know Your Audience** | Audience mismatch reviews | Audience-fit warnings in the Concept Forecast |

**Failure produces the best Lessons.** A flop with good post-mortem Lessons is real progress, which is exactly what the brief asks for: early games can fail, and success comes from learning.

- **Share it publicly** (a DevCon talk or blog post, 2–4 blocks): Industry Reputation + and Community Trust + (transparency).

## 6. Master visibility table

| System | V (exact) | E (estimate) | H (discoverable) | Revealed by |
|---|---|---|---|---|
| Founder (02) | Hours, skills, perks, burnout, morale | Burnout output penalty, collapse risk | Judgment limits | Experience |
| Freelance (03) | Pay, hours, deadlines, rating | Overrun risk, client reliability | True client reliability | History, gossip |
| Ideas (04) | Genre, theme, hook, visible traits | Market, cost, fun, streamer appeal | Unmet Demand, Viral Hook, Derivative, Late Bloomer | Evaluations, prototypes, research |
| Project (05) | Parameters, features, team, spend, phase | Pillars, duration (calibrated), budget, performance | True fun, interaction penalties | Playtests, experience, leads |
| Bugs (05) | Known bugs | Unknown bugs (with QA coverage) | Individual unknown bugs | QA, tools, launch |
| Tech debt (05) | — | Code Health band | Exact debt | Lead Programmer ≥7, *Architect* |
| Crunch (05) | Policy, hours | Nominal output | Actual output decay | Producer, Production 5 |
| Assets and vendors (06) | Price, coverage, style tag | Quality, style match, asset-flip index | Vendor reliability, popularity | Purchase, history, research |
| Candidates and employees (07) | Role, salary, tenure | Skills, morale, burnout, growth | Traits, potential, loyalty, leadership | Interviews, trials, time, HR, perks |
| Infrastructure (08) | Costs, effects | Failure risk, project-specific gains | Failure rolls, noise thresholds | Events |
| Social and creators (09) | Followers, views, prices | Reach, engagement, coverage odds | Creator preferences, conversion, posting windows, R | Influencer DB, *Algorithm Sense*, campaigns |
| Store and wishlists (10) | Wishlists, velocity, page elements | Page quality, conversion, launch forecast | Popular Upcoming rule, best launch days | Experience, A/B tests |
| Reviews (11) | Scores, labels, themes, refunds | Post-launch uplift estimates | Outlet biases | Experience |
| Publishers (12) | Written terms, tranches | Tendencies, negotiation zones | Honesty, hidden clauses, walk-away limits | Legal review, gossip, skill |
| Finance (13) | Cash, costs, payouts scheduled | Forecast bands | — | *Bookkeeper*, Finance Manager |
| Reputation (14) | Bands, tags | Trends | Exact values | Analyst mode |
| Market and rivals (14) | Announced games, released scores | Trends, competitor sales | Unannounced rival plans, trend triggers | Market research, GameStatz, gossip |
| Research (15) | Everything | Completion time | — | — |

## 7. Analyst mode (accessibility and difficulty setting)

An optional setting that shows the hidden numbers: probabilities, exact values, formulas in tooltips. For players who love spreadsheets or want less uncertainty. **Default: off.** It doesn't change the simulation. Achievements that depend on hidden information are marked, not disabled.

## 8. What is never revealed

- Individual dice rolls and exact future events (rumors can hint at them).
- Rivals' private information without gossip.
- A game's "true" fun value before release. The post-mortem shows its decomposition afterward.
