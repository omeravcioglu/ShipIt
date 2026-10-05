# 06 — Assets & Outsourcing

Every piece of work in a project can be **made** (your time), **bought** (assets and middleware), **outsourced** (contractors), or done by someone you **hire** (07). This document covers buying and outsourcing.

## Decisions
- **Make, buy, outsource or hire,** per discipline and per task.
- **Popular or obscure assets?** Cheap, polished and recognizable, or less recognizable and possibly off-style?
- **How much to customize bought assets?** Time spent versus originality.
- **Which vendors to trust** with which work, and how much of your time to spend managing them.

---

## 1. The Asset Bazaar

| Category | Examples | Replaces (share of that discipline's work, per category) | Visibility to players |
|---|---|---|---|
| Environment packs | Forest kit, dungeon kit, city blocks | Environment art 20–40% | **High** |
| Character packs | Heroes, monsters, NPCs | Character art 30–60% | **Very high** |
| Animation packs | Locomotion, combat, emotes | Animation 20–50% | Medium |
| UI packs | Menus, icons, HUD kits | UI art 40–70% | Medium |
| VFX packs | Explosions, magic, weather | VFX 30–60% | Medium |
| Sound effects | Foley, UI sounds, ambience | SFX 30–70% | Low |
| Music | Tracks, loops | Music 50–100% | Medium (recognizable tracks) |
| Shader packs | Toon, water, stylized lighting | Tech art 30–50% | Medium |
| **Tech (invisible)** | Networking frameworks, dialogue systems, save systems, inventory, AI behavior trees | Programming for that feature 30–70% | **None** |
| Game templates | "Roguelike starter", "Farming template" | Programming and design 20–40% of core systems | High if unmodified |

**Every asset has:**
- Category, **style tag** (must match your art style), quality (1–10), and price ($5–$400; tech middleware $50–$25,000).
- **Popularity:** how many games use it. This is shown ("used in ~1,200 games") once you've bought it or researched the Bazaar.
- **License terms:** almost always fine. Rare risk events include a "no commercial use in the free tier" misunderstanding, or a creator whose license is revoked for stolen art (a takedown scare).
- **Coverage:** how much of the relevant work it replaces.

**Effects of using an asset:**
- It **reduces work** by its coverage share.
- It **sets the quality of what it covers** to the asset's quality, which can be higher than your own skill. That's why beginners use assets.
- It carries **originality risk** and **consistency risk** (§2, §3).

**Prices scale with quality and exclusivity:**
- A $20 low-poly forest pack is quality 6 and used by 5,000 games.
- A $180 hand-painted pack is quality 8 and used by 300.
- A commissioned custom set (§5) is exclusive, costs thousands, and has zero recognizability.

## 2. Originality and the asset-flip model

Every visible bought asset contributes **recognizability** = popularity × player-visibility weight (characters 1.0, environments 0.8, VFX and UI 0.5, animation 0.4, music 0.4, SFX 0.1, invisible tech 0).

```
Asset-flip index = Σ (recognizability × share of the game's visible content it covers)
```

| Asset-flip index | Consequence |
|---|---|
| Low | Nothing. Nobody notices a UI pack. |
| Medium | Innovation −1. Occasional comments: *"The trees are from that forest pack everyone uses."* |
| High | Innovation −2 to −3. Critics and players call it out: ***"This looks like another asset-flip."*** Store conversion −15%. Creators pass on it. |
| Very high | The **"Asset flipper"** studio tag (14) after two such releases. Future games are judged harsher. |

**Mitigations:**
- **Customize / kitbash:** spend 15–40% of the asset's saved work to modify it. Recognizability drops by 50–80%.
- Mix in custom hero assets for the most visible content (main character, key art, capsule).
- Use popular assets only where they're invisible.
- The *Eye for Style* perk halves the consistency penalty (§3).

**The lesson players discover:** assets are best for invisible and low-visibility work, and dangerous for your hero content.

## 3. Consistency (style mismatch)

- Bought assets have style fingerprints: palette, line weight, polygon density, shading. Mixing several packs, or packs with custom art, creates a **consistency penalty** on Graphics (−0.5 to −2.5).
- The penalty shows as **"Visual consistency: Mixed"** on the Graphics estimate. Reviews say *"feels stitched together"*.
- A good art director, or the founder's *Art Director* perk, can apply a **style unification pass** (shader, palette and relighting work) to recover most of the penalty.

## 4. Tech assets and middleware (invisible, with other risks)

- Networking frameworks, physics middleware, dialogue systems and save systems have **no originality cost**. Their risks are different:
  - **Limitations:** a framework caps some feature quality. For example, a basic P2P framework can't support dedicated servers without a rewrite.
  - **Bugs:** vendor code adds its own bug rate.
  - **Vendor abandonment:** an event years later ("Framework no longer maintained. Migrate: 4–10 pw. Or stay: growing bug risk").
  - **License changes:** a price increase at renewal.
- **Buy versus build** (decision card 14 in 05): buying saves time now. Building creates **reusable tech** (15) that makes sequels and similar games cheaper, and can be sold on the Bazaar (03).

## 5. AI-generated assets (optional setting)

This reflects a real, current industry debate. It can be toggled off for players who don't want it.

| Upside | Downside |
|---|---|
| Very cheap placeholder art, concept exploration, some texture and SFX work | **Disclosure** required on Vapor. Community backlash risk: trust −, especially with Cozy and Story audiences. |
| Speeds up prototyping and store mock-ups | Consistency issues. Quality ceiling around 6. Some festivals and awards are ineligible. |
| | Artist morale and Employee Reputation drop if used for final art |

This is the same make/buy logic as everything else, with a larger reputational shadow. Used for prototypes only, it has almost no downside. Used for final hero art, it has a large one.

## 6. Your own asset library (reuse)

- Everything your studio makes (art, audio, tech, templates) goes into the **Studio Library**, tagged by style and category.
- **Reuse** is free, but reusing visible assets across games creates **studio self-similarity**: "their games all look the same". That costs a little Innovation per reuse, and more for hero assets.
- **Sequels** reuse legitimately: fans expect some continuity (11).
- You can **sell** library assets on the Bazaar (03). The twist: they become common, and clones start to look like your game.

---

## 7. Outsourcing (contractors and vendors)

**Why outsource:**
- Handle peaks of work without adding permanent payroll.
- Get skills you don't have (a composer, a trailer house, a porting studio).
- Hit a date.

**Vendors** are generated freelancers and outsourcing studios. Each has:

| Attribute | Visible? | Notes |
|---|---|---|
| Discipline(s), style specialties | V | |
| Price per person-week | V | Budget $800–1,100 (skill 3–5); mid $1,200–1,600 (skill 4–6); premium $2,000–3,000 (skill 7–9) |
| Portfolio quality | E | A range. Narrower with your judgment in that discipline (founder skill + 2, or your lead's skill). |
| Reliability (on-time delivery, revisions) | H | Revealed by working with them, or by network gossip |
| Style match | E | Mismatch causes a consistency penalty |
| Capacity and availability | V | Top vendors are booked months ahead |

**Management overhead:** every outsourced person-week needs **about 5 hours of briefing and review** from you or a lead (the simulator uses the same number). Poor briefs mean revisions. A good art director or producer reduces the overhead.

**Outcomes:** delivery can be on time and on quality, late (+1–4 weeks), off-style (needs revision rounds), or occasionally a no-show (you're refunded, but you lose time). Reliability is hidden until you have history, which rewards **stable vendor relationships**. Good vendors can be retained and later **hired** (07).

**Specialist services:**

| Service | Cost |
|---|---|
| Capsule art commission | $800–5,000 |
| Trailer house | $5–30k |
| Composer (buyout or royalty; see the music card in 05) | Varies |
| Voice casting and recording | $3–120k |
| Localization vendors | $0.08–0.15/word |
| Porting studio | $15–100k per platform, or a 15–25% revenue share |
| QA agency | $2–10k per test pass |
| PR agency | $5–15k/month |

## 8. The make / buy / outsource / hire decision

| Option | Best when | Costs | Hidden costs |
|---|---|---|---|
| **Make yourself** | Your skill is decent here, and your time is cheap | Your hours | Slow, and you can't do something else. Quality is capped by your skill. |
| **Buy assets** | The work is low-visibility, or you need speed | Money (cheap) | Originality, consistency, vendor tech limits |
| **Outsource** | A peak of work, or a missing specialty | Money (moderate) | Management hours, reliability lottery, style drift, no knowledge stays with you |
| **Hire** | Recurring need across projects | Salary (ongoing), onboarding | Burn rate, management, commitment (07) |

**The comparative-advantage insight** players can discover: once your freelance hour earns more than a contractor costs (about skill 5–6), freelancing in your strong discipline and outsourcing your weak one is financially efficient. The simulator confirms it (13). But it has real shadows: your **breadth skill stagnates**, your **judgment** in those areas stays foggy, vendor quality is a gamble (P10 revenue is lower), and the game feels less like yours (no founder morale bonus). It's strong, and it isn't free.

## 9. Example: the brief's "custom versus premade" question as a full decision

> *"Your artist asks how detailed the environments should be."*

| Option | Cost | Time | Performance | Visual quality | Originality | Notes |
|---|---|---|---|---|---|---|
| A: Simple stylized (custom) | $2,000 | 2 weeks | Excellent | Medium | High | Safe on Nimbus |
| B: Detailed hand-painted (custom) | $7,500 | 6 weeks | Good | High | High | Screenshot pop +1 → store conversion |
| C: Realistic (custom) | $20,000 | 12 weeks | Heavy | Very high | High | Needs skill 7+; ages fast; hurts Nimbus port |
| D: Popular realistic asset pack + kitbash | $450 + 1 week | 1 week | Heavy | High | **Low** (asset-flip risk) | Cheapest route to "very high" fidelity, with a reputational tax |
| E: Obscure stylized pack + unification pass | $180 + 2 weeks | 2 weeks | Good | Medium-high | Medium | A consistency risk if the pack doesn't match |

No option dominates. The right answer depends on genre, platform, budget, team skill, originality stakes and time to launch.

---

## Connections
- **Feeds:** work saved (05), Graphics, Audio and Innovation pillars (05), store conversion (10), reputation tags (14), studio library and reusable tech (15), Bazaar income (03).
- **Fed by:** cash (13), art style and scope (05), founder and lead judgment (02, 07), network gossip about vendors (14).

## Visibility
- **V:** price, coverage, style tag, license terms, vendor price and availability.
- **E:** asset quality (a range until bought), portfolio quality, style match, the Asset-flip index (shown as a band once the Originality forecast unlocks after your first release).
- **H:** vendor reliability, exact popularity (until bought or researched), future vendor abandonment.
