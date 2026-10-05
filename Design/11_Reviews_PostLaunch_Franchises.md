# 11 — Reviews, Post-Launch & Franchises

Launch isn't the end. Reviews explain what happened, post-launch decisions decide how long a game keeps earning, and successful games can grow into franchises, with their own kind of fatigue.

## Decisions
- **What to fix after launch,** and whether it's worth the money.
- **Keep supporting a game, or move on?** Abandoning a game costs trust.
- **Early Access:** when to enter, how ambitious the roadmap is, when to call it 1.0.
- **Monetizing a success:** DLC, expansions, ports, soundtrack, merch, bundles, subscription deals.
- **Sequel, spin-off, or something new?** Fans versus fatigue versus creative morale.

---

## 1. Critic reviews

- About 15 generated **outlets**, each with a personality that biases its scores:

| Outlet archetype | Cares about | Forgives |
|---|---|---|
| Visual snob ("PixelPerfect") | Graphics, art direction | Thin content |
| Indie champion ("The Indie Corner") | Innovation, heart | Rough visuals |
| Hardcore specialist | Depth, difficulty, balance | Accessibility gaps |
| Tech outlet ("FrameRate") | **Performance**, stability | Story |
| Story critic | Writing, voice acting | Gameplay flaws |
| Mainstream site | Overall polish, value | — |
| Cozy / family outlet | Charm, accessibility | Depth |

- **Sub-scores** follow the brief's format: `Gameplay 8/10 · Graphics 6/10 · Performance 4/10 · Innovation 9/10 · Content 7/10` (plus Story and Audio where relevant). They're computed from the pillar model (05) plus outlet bias.
- **ScoreHub** aggregates critic reviews into a score. It matters more for consoles, press and awards than for Vapor sales.
- **Generated review lines** come from templates that combine the strongest and weakest pillars with genre and feature context:

| Pattern | Example |
|---|---|
| High concept, low performance | *"Fantastic concept ruined by terrible optimization."* |
| High visuals, low content | *"Gorgeous, and over in three hours."* |
| High gameplay, low story | *"Come for the combat, skip the cutscenes."* |
| High innovation, low polish | *"Rough around every edge, and unlike anything else this year."* |
| Asset-flip index high | *"Another asset-flip with a clever title."* |
| Multiplayer bugs | *"When the netcode works, it's chaos in the best way. It rarely works."* |
| Sequel, low innovation | *"More of the same. Fans won't mind; nobody else will notice."* |

**Implemented (light, M2):**
- **How many critics.** A scratch.io game gets one blogger. A Vapor game gets 2 critics, plus 1 per 500 launch wishlists, plus 1 for Small scope, up to 6.
- **Scores.** Each critic scores the launch build: 100 × (0.28 + 0.72 × quality), minus 1.5 per missing point of stability, plus the outlet's taste (−4 to +4) and some noise (σ 6). Each review comes with a one-line quote.
- **Timing.** Reviews are published over days 1–5 of launch week. **ScoreHub** is their average once three are out, with the usual labels.
- **Effect.** Critics don't change sales yet. Store visibility from ScoreHub comes with the live launch dashboard.

## 2. Player reviews (Vapor-style)

- **Simulated player samples:** each buyer belongs to an audience segment (05) with preferences and hardware. Their experience depends on the pillars, value for money, the bugs they actually hit, performance on their hardware, and whether the game matches its marketing and tags. About 1–3% of buyers write a review.
- **Labels:**

| Label | Threshold |
|---|---|
| Overwhelmingly Positive | 95%+ with 500+ reviews |
| Very Positive | 80%+ with 50+ reviews |
| Mostly Positive | 70–79% |
| Mixed | 40–69% |
| Mostly Negative / Very Negative | Below 40% |

  Labels affect conversion (from ×1.8 down to ×0.2). The **10 / 50 / 500 review thresholds** also affect store visibility.
- **Review themes** are aggregated: *"Players praise: art style (41%), atmosphere (33%). Complaints: performance (32%), short length (18%)."* These themes are the player's to-do list, and they map directly to pillars and unfixed findings.
- Example review text: *"The game is fun but multiplayer barely works."* The player can decide whether to spend money fixing it (§5).
- **Implemented in M1 (`ReviewWriter`):** review lines are generated per game, and the same game always gets the same lines.
  - Each line reacts to a pillar the game was really judged on, weighted by the genre: praise picks its strong pillars, complaints its weak ones. Stability gets praised only when the game is spotless and complained about when it isn't.
  - Lines name a feature you actually built: *"Love the Leaderboards. Tight and responsive."*, *"Skipped every line of Chapter 2."*
  - The share of thumbs-up follows the game's positive share.
  - They appear on Launch Day as first impressions and on the released game's card as "What players say".
- **The share moves with the players' mood (Implemented, M2).** Each day's reviews are written in that day's mood (launch week, 10 §7). Hotfixes and answering players raise it, so fixing crash bugs in the first days moves the score the most.
- **Recent versus all-time:** updates can turn "Recent: Very Positive" around, even while "All-time: Mixed" stays visible forever.
- **Developer responses:** replying to negative reviews with fixes can flip some of them (+). Defensive tone backfires.

## 3. Refunds

- **The 2-hour rule:** buyers can refund within a short playtime. Refund rate goes up with:
  - Bugs and crashes in the first hour.
  - Poor performance.
  - "Not what I expected" (misleading tags or trailer).
  - **Games shorter than ~2 hours** (they can be finished and refunded).
- Base refund rate is 5–8%, and can reach 15–30% for broken launches. Refunds are deducted from gross revenue (13).
- This is why a **tiny, short game** can be risky at a premium price, and why tutorials cut refunds.

## 4. Review bombs and controversies

- **Triggers:** an exclusivity deal announced after crowdfunding on Vapor, monetization changes, broken promises, DRM, a platform policy, an employee's controversy, a political flashpoint.
- **Effect:** a temporary flood of negative reviews. Vapor marks off-topic bombs (they're excluded from the score after review), but the drama costs visibility and trust anyway.
- **Responses:** a statement, reversing the decision, or holding firm. The best response depends on whether the community has a point. High Community Trust dampens the damage.

---

## 5. Post-launch operations

| Action | Cost | Effect |
|---|---|---|
| **Bug-fix patch** | Programmer weeks | Stability +. "Recent reviews" improve. Refunds fall. |
| **Performance update** | Programming, Technical Art | Performance + → fewer complaints. Can unlock Deck Verified. |
| **Free content update** | Team weeks | A visibility bump on Vapor ("update" traffic), trust +, returning players, a small sales spike |
| **Paid DLC** | Team weeks | Revenue from the existing audience (attach rate 5–20% of owners). Can split multiplayer player bases. "Day-one DLC" backlash risk (05). |
| **Expansion** | Large | Relaunch-style beat. High attach for loved games. Awards eligibility. |
| **Console port** | Team or porting partner (03, 06) | A new audience and a second launch beat. Certification. Performance targets (Nimbus). |
| **Localization** | Per language (05) | Market share by language **per genre** (for example Chinese is huge for some genres, German for strategy, Japanese for certain RPG styles). Machine versus professional quality risk. |
| **Multiplayer updates** | Networking | Population retention (§7) |
| **Seasonal events** | Content | Activity + for live games |
| **Discount / sale participation** | — | Volume spikes. Trains waiting (10). |
| **Bundles** (with other games) | — | Volume, new audience |
| **Subscription deal** (Axis Pass, 12) | — | A flat fee. Fewer sales on that platform, but word of mouth and multiplayer population. |
| **Sunset** | — | Ends support costs. Trust drops if it's abrupt; a final update plus clear communication softens it. Games sunset early and repeatedly earn the **"Abandons games"** tag (14). |

**The post-launch decision:** each week of team time spent on a released game is a week not spent on the next one. The **Game Lifecycle panel** shows weekly sales against the forecast decay, the estimated uplift from each action (as a range), support costs, and the "Recent reviews" trend.

### Sales lifecycle

```
Weekly sales(t) = Launch × e^(−t/τ) + LongTail
τ (decay)       grows with review score, word of mouth, virality, and updates
LongTail        grows with review label, tags/discoverability, seasonal sales, sequels (halo), awards
```

The simulator's small-game version: year-one units ≈ first month × (1.8 + 2.2 × quality), plus a tail (13).

## 6. Early Access

**Entry requirements:** a playable core loop, a store page, and an **Early Access roadmap.** Every roadmap item becomes a promise (09).

| Advantage (from the brief) | Risk (from the brief) |
|---|---|
| Early revenue | Negative first impressions stay in all-time reviews |
| Community feedback (findings at scale) | Players lose interest ("EA fatigue" after ~18–24 months) |
| Funding for continued development | A poor launch can permanently cap the game |

**Rules:**
- The EA audience is smaller and more forgiving about content, but it **expects regular updates.** Cadence matters more than size. Each update gives a visibility bump.
- **Skipping updates** triggers *"Is this game abandoned?"* posts, and trust and sales decline.
- EA price is usually lower. **Raising the price at 1.0** is accepted if it was announced (a promise).
- **1.0 launch:** a second launch beat. It's smaller than a fresh launch unless the game was transformed (then press re-covers it). Critics may review it again.
- **The decisions:**
  - Enter EA early (more money and feedback, worse first impression) or late.
  - A roadmap that's ambitious (hype) or modest (safety).
  - How long to stay (fatigue versus polish).

## 7. Live and multiplayer games

- **Player population** decides whether online matchmaking works. When concurrent players drop below a threshold for their region or mode, queues lengthen, reviews say "dead game", and population falls further (a **death spiral**).
- **Mitigations:** crossplay, bots / AI fill, fewer modes, a free weekend, a subscription deal (a population boost).
- **Server costs** scale with monthly active users. Dedicated servers cost $0.05–0.15 per user per month; P2P costs almost nothing. The decision to shut servers down, or move to P2P, is a sunset decision.
- **Live ops** (seasons, battle pass) create revenue and **content-cadence obligations.** The team is locked into maintenance, and creative morale suffers over long stretches.

---

## 8. Franchises

- A well-received game becomes an **IP** with:
  - **Brand strength:** awareness among players.
  - **Fan expectation:** the bar for the next entry.
  - **Fatigue.**
- **Sequels:**
  - **Start with an audience:** previous owners and fans convert at 2–4× the rate of strangers.
  - **Reuse tech and assets** (cheaper and faster), and inherit the codebase's **tech debt** (05).
  - **Expectation** = the previous entry's score + an improvement demand. "More of the same" is punished unless the game innovates.
  - **Creative staff morale** drops over repeated sequels (07).
- **Franchise fatigue:**
  - Each release adds fatigue. It decays over about 2 years.
  - High fatigue reduces conversion and Innovation.
  - **Spin-offs** (same world, new genre) add less fatigue and bring over part of the fan base (fan genre affinity, 09).
- **Remasters and remakes:** for old games, when graphics-expectation drift (14) has made the original look dated. Cheaper than a new game, with a nostalgia audience.
- **IP ownership matters most here.** If a publisher owns the IP (12), they can make a sequel **without you.** The event *"Your former publisher announced a sequel to your game, developed by another studio"* is the long-term consequence of a signature years earlier. Buying back an IP is possible and expensive.

## 9. Merchandise, soundtrack and licensing

**Merchandise** (needs brand strength ≥ "Recognized"):

| Option | Margin | Risk |
|---|---|---|
| Print-on-demand | Low | None |
| Bulk inventory | High | **Unsold stock** |
| Licensing partner | Hands-off | Less quality control |

**Soundtrack:** revenue depends on audio quality and fan base, and **who owns the rights** depends on the composer contract (05). A buyout means the soundtrack revenue is yours. A royalty deal means the composer shares it or owns it. A great soundtrack also raises Audio award odds.

**Licensing:** comics, board games, and at high brand strength a **TV adaptation** event. Passive income, plus a small risk that a bad adaptation damages the brand.

**Selling the IP:** a lump sum for someone else's future. Occasionally the right call in a crisis (16).

## 10. Post-mortems

At day 30 (and optionally at 6 months), every release gets a **post-mortem**. It turns outcomes into knowledge by revealing which factors actually drove the reviews and sales. Details are in 17.

---

## Connections
- **Feeds:** cash (13), reputation, trust and tags (14), knowledge (17), franchise and brand (14), creative morale (07), ideas for sequels (04).
- **Fed by:** pillars, bugs and performance (05), marketing accuracy and hype (09), store and price (10), publisher terms (12), market and trends (14).

## Visibility
- **V:** review scores, labels, themes, refund rate, sales, attach rates, server costs, fatigue band (after the first sequel).
- **E:** uplift estimates for post-launch actions, lifecycle forecast, population forecast.
- **H:** individual simulated player experiences, exact outlet biases (learned over time), the exact fatigue value.
