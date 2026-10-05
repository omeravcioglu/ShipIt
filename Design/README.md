# Indie Studio — Game Design Document

> **Working title:** *Indie Studio* (placeholder)
> **Genre:** Deep 2D management / business simulation
> **Premise:** Start alone in a tiny apartment with $5,000, a cheap laptop and basic skills. Survive, ship bad games, learn, and grow into a major independent developer.
> **Target:** 15–30+ hours per studio, 10–15 in-game years, different stories every playthrough.

---

## How to read this

Start with **01** (vision, pillars, loops). After that, read any system doc in any order. Every system doc follows the same template:

| Section | Question it answers |
|---|---|
| **Decisions** | What does the player actually decide here, and why is it hard? |
| **Model** | What are the rules? Reference formulas are tunable, not final. |
| **Numbers** | Starting tuning values. |
| **Connections** | What feeds this system, and what does it feed? |
| **Visibility** | What the player sees exactly (**V**), sees as an estimate (**E**), or must discover (**H**). |

Cross-cutting documents:
- **17 Information & Visibility**: the master table of what's visible and what's hidden.
- **19 System Interactions**: cause-and-effect chains, feedback loops and a sample playthrough.
- **13 Finance & Economy**: all the money in one place, including results from the economy simulator.

## Document map

| # | Document | Covers |
|---|---|---|
| 01 | [Vision, Pillars & Core Loop](01_Vision_Pillars_CoreLoop.md) | Fantasy, pillars, decision audit, resources, time model, loops at four scales, added mechanics |
| 02 | [Founder & Survival](02_Founder_and_Survival.md) | Starting conditions, backgrounds, living costs, the player-driven day (three slots, work minigames), burnout, founder skill tree, personal life |
| 03 | [Income & Funding](03_Income_and_Funding.md) | Freelancing, day jobs, jams, contracts, asset store, Patronly, crowdfunding, grants, mobile, money-now-vs-later |
| 04 | [Ideas & Prototyping](04_Ideas_and_Prototyping.md) | Idea sources, idea cards and traits, combining ideas, prototype questions, greenlight |
| 05 | [Game Development](05_Game_Development.md) | Concept parameters, features, phases, work model, quality model, decision cards, scope creep, tech debt, crunch, bugs, delays |
| 06 | [Assets & Outsourcing](06_Assets_and_Outsourcing.md) | Asset Bazaar, originality and asset-flip risk, consistency, middleware, outsourcing, make/buy/hire |
| 07 | [Team & People](07_Team_and_People.md) | Roles, stats, hidden traits, hiring, onboarding, span of control, morale, burnout, poaching, events, culture |
| 08 | [Studio Infrastructure](08_Studio_Infrastructure.md) | Hardware, software, offices, rooms, location, remote versus office |
| 09 | [Marketing, Audience & Virality](09_Marketing_Audience_Virality.md) | Audience model, social platforms, content, press, ads, influencers, buzz cascades, community, promises |
| 10 | [Store, Wishlists, Events & Launch](10_Store_Wishlists_Events_Launch.md) | Store page, wishlists, pricing, festivals, demos, release calendar, Launch Week |
| 11 | [Reviews, Post-Launch & Franchises](11_Reviews_PostLaunch_Franchises.md) | Critic and player reviews, refunds, Early Access, live ops, DLC, ports, sequels, fatigue, merch |
| 12 | [Publishers & Negotiation](12_Publishers_and_Negotiation.md) | Publisher profiles, pitching, deal terms, recoupment, hidden clauses, negotiation, platform deals |
| 13 | [Finance & Economy](13_Finance_and_Economy.md) | Statements, revenue waterfall, payout timing, taxes, loans, investors, economy targets, simulator results |
| 14 | [Reputation, World & Events](14_Reputation_World_Events.md) | Reputation dimensions and tags, contacts, market and trends, competitors, industry event deck |
| 15 | [Research & Perks](15_Research_and_Perks.md) | Capability tree (done by people, with upkeep), reusable tech, earned studio perks |
| 16 | [Progression, Goals & Failure](16_Progression_Goals_Failure.md) | Studio tiers, unlock schedule, pacing, goals and ambitions, crisis and recovery, endings, modes, replayability |
| 17 | [Information & Visibility](17_Information_Visibility.md) | Visibility classes, knowledge sources, estimate display, master table, post-mortems |
| 18 | [UI / UX](18_UI_UX.md) | Presentation frame, screens, wireframes, "Why?" breakdowns, alerts, accessibility |
| 19 | [System Interactions](19_System_Interactions.md) | Big map, 23 cause-and-effect chains, interaction matrix, feedback loops, sample 12-year story |
| 20 | [Production Roadmap](20_Production_Roadmap.md) | Build order for this game, Unity 6 architecture notes, tuning pipeline |
| — | [Tools/economy_sim.py](Tools/economy_sim.py) | Monte-Carlo simulator of the solo years, used to tune the early game |

---

## The game in one page

**The fantasy.** You are an indie developer. Early on you do everything yourself: code, art, marketing, taxes, and deciding whether to pay rent with freelance work or bet another month on your game. Later you run a studio with people you hired, office leases you signed, publishers you negotiated with, and a community that remembers every promise you made.

**The arc.** Bedroom Developer → Small Indie Team → Established Indie Studio → Successful Studio → Major Independent Developer. Tiers are earned through combinations of shipped games, team, reputation and money, not through money alone (see 16).

**The currencies.** The player constantly converts one currency into another, and no conversion is free:

- **Money** (cash, and cash that's owed to you but not yet paid).
- **Time** (your weekly hours, then your team's capacity, then your own attention).
- **Knowledge** (what you actually know about genres, audiences, tech and your own estimates).
- **Audience** (followers, wishlists, mailing list, Discord-style community).
- **Reputation and trust** (five kinds, plus earned tags like "Abandons games").
- **People** (skills, morale, loyalty and burnout of everyone, including you).

**The core loop (per project).** Find an idea → prototype it to buy information → build it while making dozens of tradeoff decisions → market it and build wishlists → launch (a tense, live climax) → support it or move on → run a post-mortem that turns results, good or bad, into knowledge → pick a bigger, better-informed bet.

**Why it stays interesting for 30 hours.**
1. The problems change with scale. First rent, then payroll, then coordination, then portfolio risk and investor pressure.
2. The market moves: trends, rivals, platforms and ad prices shift, so last year's winning formula decays.
3. Micro-decisions become delegable as the studio grows. The player's attention climbs to higher-level choices instead of repeating the same clicks.
4. People with hidden traits, rivals, creators and publishers produce stories that aren't scripted.
5. Every launch is a live event with real stakes.

## Design rules (apply to every system)

1. **Decision test.** Every mechanic must create a decision, risk, tradeoff, strategy or progression opportunity. If it doesn't, redesign it or cut it.
2. **No flat buffs.** Upgrades unlock options, reveal information, or solve problems that appear at scale. They cost upkeep, time or flexibility. "Office Level 2 = +10% money" is banned.
3. **Problem before solution.** The game makes the player *feel* a problem (lost work, merge chaos, missed estimates) before it offers the upgrade that fixes it. That's how upgrades become desirable and how systems get taught.
4. **Information is a resource.** The simulation knows the truth. The player sees estimates whose accuracy they buy with time, money, people and experience.
5. **Every gain has a shadow.** Fame raises expectations. A bigger team adds coordination cost. Hype raises the bar. Money from partners comes with obligations.
6. **Failure produces knowledge and story.** A flop yields lessons, audience and experience. Bankruptcy opens hard choices before it ends anything.
7. **Time is the scarcest resource.** Money can be borrowed; weeks can't.

---

## Glossary of fictional names

All brands are placeholders. The real-world analog is given only to explain intent.

| In game | Real-world analog | In game | Real-world analog |
|---|---|---|---|
| **Vapor** | Steam | **Unify** | Unity |
| **Vapor Next Fest** | Steam Next Fest | **Unreel** | Unreal Engine |
| **Vapor Deck** / "Deck Verified" | Steam Deck | **Forge** | Godot |
| **Epoch Store** | Epic Games Store | **Maker2D** | GameMaker |
| **scratch.io** | itch.io | **Asset Bazaar** | Unity Asset Store / Fab |
| **Nimbus** | Nintendo Switch | **GigGrid** | Upwork / Fiverr |
| **Vertex** | PlayStation | **Fundstarter** | Kickstarter |
| **Axis** / **Axis Pass** | Xbox / Game Pass | **Patronly** | Patreon |
| **Orchard** / **Droid** stores | App Store / Google Play | **ScoreHub** | Metacritic / OpenCritic |
| **Visor** | VR headsets | **GameStatz** | SteamDB / Gamalytic |
| **ViewTube** | YouTube | **Gamedoor** | Glassdoor |
| **LiveWire** | Twitch | **DevCon** | GDC |
| **Clipz** | TikTok | **PlayExpo** | gamescom |
| **Chirp** | Twitter / X | **PlayersCon** | PAX |
| **Threadit** | Reddit | **Summer Showcase** | Summer Game Fest |
| **Chorus** | Discord | **Indie Excellence Awards** | IGF |
| **CreatorConnect** | Keymailer / Lurkit | **Golden Controllers** | The Game Awards |
