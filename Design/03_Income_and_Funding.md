# 03 — Income & Funding

The brief's central tension: **"Do I make money now, or do I invest time in my own game?"** This document lists every way to get money and what each one costs: in time, obligations, control, or future revenue.

## Decisions
- **Money now or investment?** Every block spent earning is a block not spent building your future.
- **Which kind of money?**
  - **Earned** money costs time.
  - **Borrowed** money costs interest.
  - **Sold** money (equity, IP) costs control.
  - **Pre-sold** money (crowdfunding, Early Access, publisher advances) costs obligations.
  - **Granted** money comes with strings.
- **Which obligations can I actually keep?** Every promise made to raise money goes into the Promises ledger (09).

---

## 1. Income matrix

| Source | Unlocks | Cash timing | Time cost | What it builds | Risk | Obligations |
|---|---|---|---|---|---|---|
| **Freelance gigs** (GigGrid) | Start | On delivery (days to weeks) | Founder slots (played as minigames) | Main skill, rating, some contacts | Non-payment, scope creep | Deadlines |
| **Day job** | Start | Monthly | Fixed slots | Nothing | None | Fixed hours |
| **QA / playtest gigs** | Start | Weekly | Blocks | Little | Low pay | — |
| **Game jams** | Start | Rarely any | A weekend or 1–2 weeks | Ideas, contacts, skill, audience, occasional viral hit | Burnout | — |
| **Asset Bazaar products** | First shipped project, or a craft skill ≥3 | Monthly, long tail | Weeks upfront plus support | Passive income, developer reputation | Engine updates break products | Support |
| **Patronly** | 250+ followers | Monthly | Content cadence | Trust | Churn | Monthly posts or builds |
| **Small mobile games** | After the first release | Monthly | 2–8 weeks per game | Paid-marketing knowledge (CPI/LTV) | Ad spend lost | Live ops |
| **Contract game development** | Tier 2, or solo at skill ≥5 with 4★ | Milestones (Net-30/60) | Team capacity | Specific capabilities, contacts, cash | Overruns, cancellation | Deadlines; IP goes to the client |
| **Porting contracts** | Programming ≥4 | Milestones | Programmers | **Platform knowledge, dev-kit access, platform contact** | Certification failure | Deadlines |
| **Consulting and talks** | Tier 3 reputation | Per engagement | Founder blocks | Industry reputation | — | — |
| **Crowdfunding** (Fundstarter) | Vertical slice; 500+ engaged followers recommended | Lump sum after the campaign | Campaign month | Audience, early community | Public failure | Rewards, stretch goals, updates, delivery |
| **Early Access** | Playable core loop plus store page | Ongoing sales | Ongoing updates | Feedback, community, funding | Permanent first impressions | Roadmap promises (11) |
| **Grants** | Mostly after incorporation | Tranches | Application plus reporting | — | Rejection | Reports, milestones, content/location rules |
| **Tax relief** | Incorporated in a qualifying region | Annual refund | Admin | — | Audit | Eligibility |
| **Publisher advance** | Vertical slice plus pitch | Milestone tranches | Pitch plus negotiation | Services (marketing, QA, ports) | Cancellation | Milestones, revenue share, rights (12) |
| **Platform deals** | Traction | Lump sum or tranches | Negotiation | Exposure | Community backlash | Exclusivity terms (12) |
| **Loans** | Varies | Immediate | Admin | — | Interest, default | Repayments (13) |
| **Investors** | Incorporated plus traction | Tranches | Pitch meetings | Network | Loss of control | Board, KPIs, exit expectations (13) |
| **Selling or licensing IP** | Owning a successful IP | Lump sum or royalties | Negotiation | — | Losing your IP | — |
| **Post-launch extras** | After release | Ongoing | Varies | — | Fatigue, trust | DLC promises, fulfilment (11) |

---

## 2. GigGrid freelancing (the survival backbone)

**Gig card:**
- Client: name and type, plus rating if they have a history.
- Task, discipline, minimum skill.
- Estimated hours, with an "overrun risk" tag.
- Pay (fixed or hourly), deadline.
- Extras: portfolio piece, contact, repeat potential.
- Risk flags: *vague requirements*, *new client*, *rush*.

**Pay rate** (matches the simulator): **$/hour = (5 + 4.5 × skill) × (1 + 0.25 × stars/5)**

| Skill | 2 | 4 | 6 | 8 |
|---|---|---|---|---|
| New profile | $14 | $23 | $32 | $41 |
| 5★ | $17.50 | $28.75 | $40 | $51 |

- **Availability:** a new profile finds only **60%** of the hours you want. This rises to **95%** as your rating grows. The rating is about 3★ after ~400 hours of good work. With no gig to work on, a Gig work session goes into **looking for clients** (a 50% chance of a new offer).
- **Rate setting:**
  - **Budget:** −20%. More gigs, worse clients.
  - **Market:** default.
  - **Premium:** +25%. Fewer, better gigs. Requires 4★.
- **Client types:**

| Client | Pay | Character |
|---|---|---|
| Friends & family | Low | Flexible; relationship stakes |
| Small studios | Fair | May become a contact, collaborator or future hire |
| Agencies | Steady | Repeat work; boring |
| Startups | High | ~8% non-payment risk; scope creep |
| Corporate (gamification) | High | Pays Net-60; morale −1 per gig |
| Other indies | Revenue share instead of cash | A lottery ticket in their game |

- **Risks:**
  - Non-payment: 6% per week at first, falling to 1% with rating.
  - Scope creep: +20–50% hours. Renegotiate (risks the relationship) or absorb it.
  - Deadline clashes with your own milestones.
- **Retainers:** after 3 good gigs, a client may offer a retainer: fixed slots every week for 3–6 months at a 10% discount. Stable, but inflexible. Breaking it early hurts your rating.
- **Not only programming:** sprites and models (Art), SFX packs (Audio), game text and localization proofreading (Writing), trailer editing (Marketing ≥3), design consulting (Design ≥5), bookkeeping (Business ≥3).
- **Every gig session is played** (02 §4). A morning, afternoon or evening on a gig is a minigame for its discipline:

| Gig skill | Minigame | What you do |
|---|---|---|
| Programming | **Bug Squash** | Click the bugs crawling out of the client's code before they escape; leave the green "features" alone |
| Art | **Pixel Match** | Copy a small sprite pixel by pixel; done the moment it matches |
| Writing | **Typo Hunt** | Proofread the client's text: click every misspelled word, and only those |
| Audio | **Synth Tuner** | Turn pitch and volume until your wave sits on the reference, three times |
| Design | **Feedback Sort** | Sort playtester comments into problems, wishes and praise |

  The session's score scales the hours it gets done (×0.5–×1.2), so careful play finishes gigs sooner. The average across a gig's sessions also moves the client's rating (+2.5 × (average multiplier − 1)). Harder clients (a higher required skill) get bigger boards and pickier targets, and founder skill buys extra time.

> **Example card:** *"Startup offers $2,400 for a 60-hour prototype due in 2 weeks. New client. Vague requirements."* Accept, counter at $3,000 with a revision cap, or decline?

**What the simulator shows (13):** at 20 hours/week from a new profile, gigs don't cover rent until about month 6. After that they do, with a small surplus. Freelance-heavy play is the safest and banks the most, but its first real game is weaker: less marketing and less breadth skill (13 §12).

## 3. Day jobs

| Job | Hours | Net pay | Notes |
|---|---|---|---|
| Part-time | 20, fixed blocks | $1,600/month | Covers living costs. No skill growth. |
| Full-time | 40, fixed blocks | $2,900/month | In the simulator, with 12 hours/week of tired evening development (×0.8 after a shift), the first Small game does **not** ship within 3 years. That's why the premise starts with quitting. |

Quitting has no penalty. Getting a job again takes 2–6 weeks. "Return to a day job" is a recovery option when things go wrong (16).

## 4. QA testing and odd jobs

Always available: $12–15/hour, 8–40 hours. **Playtest swaps** with other developers pay in goodwill: they'll playtest your game later (a free external playtest).

## 5. Game jams

| Jam | When | Length |
|---|---|---|
| Global jam | January | 48 hours |
| Ludum Jam | April, August, December | 72 hours |
| scratch.io themed jams | Monthly | 1–2 weeks |
| Sponsored jams | Occasional | Prizes $500–$5,000 |

- **Cost:** a 72-hour jam takes the weekend (6 blocks) and adds burnout +6.
- **Team jams** pair you with 1–3 generated developers, and you see their **real skills and traits**. This is the most reliable hiring pipeline (07).
- **Output:**
  - A jam game (1–2 person-weeks) and a ranking among entrants.
  - An idea card with the **Jam-proven** trait (the "is it fun?" question is already answered).
  - XP ×1.3, 50–500 followers, contacts.
- **Luck:** about 1% of jam games go viral. Virality traits raise the odds. Results: publisher interest, player demands to "make it a full game", creator coverage.
- **Theme constraints** push you toward unusual ideas: more Innovation, sometimes a smaller market.

## 6. Contract game development (work-for-hire)

Unlocks at Tier 2, or for a solo founder with a craft skill ≥5 and a 4★ rating.

| Contract type | Typical value | Duration | Team | What it builds |
|---|---|---|---|---|
| Educational / museum game | $30–80k | 3–6 months | 2–3 | Grant eligibility, industry reputation |
| Advergame for a brand | $40–120k | 2–4 months | 2–4 | Marketing contacts (tight deadlines) |
| **Porting** an indie game to Nimbus, Vertex or Axis | $25–100k, or a 15–25% revenue share | 2–5 months | 1–3 programmers | **Console knowledge, dev-kit access, platform contact** |
| Co-development / outsourced work for a bigger studio | $15–60k/month | Ongoing | 2–10 | Prestige, AAA contacts, recruits |
| Corporate VR/AR training simulation | $80–250k | 4–8 months | 3–5 | Money (creative morale −) |
| Prototype for a publisher (they own it) | $20–60k | 1–3 months | Small | Publisher relationship |
| Mobile game for a client | $30–90k | 3–6 months | 2–4 | Mobile knowledge |

- **Terms:**
  - **Fixed bid:** you absorb overruns, and the planning fallacy bites (05).
  - **Time and materials:** safe, lower margin, more client scrutiny.
  - Milestone payments, often Net-30 or Net-60.
  - The client owns the IP. Kill fees are 20–50%. Clients differ in strictness (revision rounds).
- **Why take contracts:** they pay payroll, and many build a capability your own games need. The best example: a porting contract teaches console development, which later makes porting your own game cheap.
- **Why not:** contracts delay your own games, build no IP or audience, and creative employees lose morale on contract work that doesn't interest them (−2/month).
- **The service trap:** more than 60% of capacity on contracts for 12+ months earns the **"Service Studio"** tag. Contracts get easier to land, creative recruits get harder to attract, and your audience forgets you. It's a legitimate but capped path; 16 describes the late-game "Co-dev Partner" track.

## 7. Consulting, teaching and talks

- **Consulting** (Tier 3+, a relevant skill ≥6 or a hit game): $1,500–$10,000 per engagement for 1–5 founder blocks. +Industry reputation.
- **Courses:** recording a course takes 20–40 blocks. Passive income of $200–$3,000/month, decaying. Builds a *developer* audience, not players.
- **DevCon talks** (for example a post-mortem talk after a notable release): +Industry reputation and recruiting.

## 8. Asset Bazaar products

- **Create** an asset pack, template, tool, shader pack or sound pack: 2–8 weeks solo.
- **Sales:** a launch spike, then a long tail. The Bazaar has its own demand trends ("stylized nature packs are hot"; "inventory tools are saturated"). Reviews matter. Prices are $5–$80.
- **Support upkeep:** 0.5 block/week per supported product. Unsupported products lose rating and sales. Major engine updates break products (event: *"Unify 2031 broke your tool: update it (1–3 weeks) or delist it"*).
- **Dual use:** tools your people build through research (15) can be turned into products for about 30% of the original work (documentation and polish).
- **The twist:** selling your own game's art pack makes those assets common. Clones appear and future originality suffers ("Hey, that asset-flip uses the art from your game!"). Extra income versus a distinctive brand.
- **Typical income:** $50–$500/month per product. A hit product makes $2,000–$10,000/month.

## 9. Small mobile games (optional side business)

- Quick games (2–8 weeks), monetized with ads or light in-app purchases.
- **Revenue** ≈ installs × retention × ad revenue per daily user. Installs come from organic discovery (small) or paid user acquisition (cost per install $0.20–$2.00).
- **The CPI test:** a mobile publisher spends ~$500 advertising your prototype. If the cost per install is under target and day-1 retention is above ~35%, they fund marketing and take 50–70%. That typically returns $5k–$100k over months. Most tests fail.
- **What it builds:** paid-marketing knowledge (CPI, lifetime value, retention) that transfers to paid ads on PC (09).
- **What it costs:** no fans carry over to your core games. Creative staff find it formulaic (morale −). A pattern of ad-heavy games earns the "Mobile ad mill" perception, which mostly hurts creative hiring.

## 10. Patronly (supporters)

- Unlocks at 250+ followers.
- Supporters ≈ 0.5–2% of *engaged* followers, averaging $5/month.
- **Tiers:**
  - $3: name in the credits.
  - $10: monthly build access.
  - $25: vote on features. This creates promises.
- **Obligations:** a monthly devlog, plus a monthly build if you offer that tier. Missing a month causes 8–15% churn and a trust loss. Every tier reward is recorded in the Promises ledger (09).
- **Typical income:** $100–$1,500/month in the solo phase. Small, but recurring. It bridges the gaps between gigs.

## 11. Crowdfunding (Fundstarter)

- **Requirements:** a vertical slice or strong prototype, plus a trailer. Recommended: 500+ engaged followers or mailing-list subscribers.
- **Setup decisions:**
  - Goal amount and a 30-day duration.
  - **Reward tiers:** digital rewards cost almost nothing; physical rewards cost 30–60% of the tier price plus fulfilment time and shipping chaos.
  - **Stretch goals:** each one is a promise (new features, languages, console ports), which means scope creep.
  - Campaign video (trailer quality), pre-campaign marketing, and launch timing (avoid big competing campaigns).
- **Dynamics:**
  - A spike on days 1–2 from your own community. Campaigns that hit 30% in 48 hours usually succeed.
  - A mid-campaign lull. Updates, creator coverage and press can bump it.
  - A final-48-hour spike worth 20–30% of the total.
  - Backers ≈ engaged community × 3–8%, plus press and creator coverage, plus platform discovery. The average pledge is about $28.
- **Money:** all-or-nothing. About 10% goes to platform and payment fees. Rewards cost extra. Funds arrive ~2 weeks after the campaign closes.
- **Obligations:** monthly backer updates, a delivery date, and every stretch goal.
  - Late delivery or cut stretch goals → trust drops, and the studio may earn the **"Broken crowdfunding promises"** tag (14).
  - Success → trust rises, you get a dedicated early community, and backers become a free QA pool (backer beta).
- **Failure** costs no money and only a small reputation dent, and it gives you real data about market interest. Relaunching with a smaller goal is a classic comeback story.
- **Typical results:** $10k–$80k. Standouts reach $250k+.

## 12. Early Access (summary)

Early Access trades permanent first impressions and a roadmap of promises for early revenue, feedback and funding. Full rules are in 11.

## 13. Grants and tax relief

- **Grants** are generated per region and inspired by real funds. Examples:
  - *Creative Media Fund – Prototype*: $10–50k, no matching required.
  - *Creative Media Fund – Production*: $50–250k, requires **25–50% matching funds** from your own money or a publisher.
  - *Cultural Heritage Game Grant*: requires a local history or culture theme.
  - *Accessibility Fund*: requires the Accessibility Suite feature.
  - *Emerging Studio Fund*: founder-owned studios in their first 3 years.
- **Applying:** 5–10 founder blocks (quality depends on Business). Windows open 2–3 times a year. A decision arrives in 8–12 weeks. Success is **15–40%**, based on concept quality, team credibility, application quality and fit with the fund's priorities.
- **Obligations:** a quarterly report (1 block), milestones (missing them risks claw-back), a logo in the credits, and sometimes local hiring or spending rules.
- **The decision:** grants shape your game (a required theme may not be the most marketable one), but they're non-dilutive and take no revenue share.
- **Tax relief** (tied to studio location, 08): for example 20–25% of qualifying development costs refunded each year after incorporation. Requires cultural-test eligibility and paperwork (1–3 blocks a year, or an accountant).

## 14. Publishers and platform deals (summary)

Full rules are in 12.
- **Publisher advances** arrive in milestone tranches. They are **recouped from revenue** and come with deadlines, a revenue share and sometimes IP ownership.
- **Platform deals:** exclusivity minimum guarantees (Epoch), subscription deals (Axis Pass), platform funding.

## 15. Borrowing and selling equity (summary)

Full rules are in 13.

| Source | Terms |
|---|---|
| Credit card | Instant, 24% APR |
| Family loan | One-time, interest-free, relationship stakes |
| Bank loan | Requires incorporation and a revenue history |
| Revenue-based financing | After launch; repaid from revenue |
| Angel investors | 8–20% equity |
| Venture capital | 15–35% equity, a board seat and growth KPIs |
| Strategic investors (platforms, publishers) | Equity with strings attached |

---

## 16. The decision in practice

> **Month 5.** $1,900 in the bank. Rent in 12 days. The Tiny game has made $340 in total.

| Option | Now | Later |
|---|---|---|
| Take the 60-hour startup gig ($2,400) | Rent is safe for 2 months | −3 weeks on your game; 8% non-payment risk; a possible contact |
| Launch a Patronly | +$150/month from 180 followers | A monthly devlog obligation (1 block) |
| Apply for the Prototype Grant ($25k at ~30% odds) | −8 blocks either way | A decision in 10 weeks; huge if it hits |
| Put 2 months on the credit card | +$3,000 of room | 24% APR; money anxiety continues |
| Move in with roommates | −$300/month | Noisy weeks, morale |
| Stay all-in | Ship the Small game ~4 months sooner | Broke in ~6 weeks without a bridge |

In the simulator, going all-in with no outside money goes broke in about 3 months in 100% of runs. A balanced split survives, with a median lowest balance of about **$740**. Every dollar matters, and every option above is right for some player.

---

## Connections
- **Feeds:** cash (13), founder skills and rating (02), contacts (14), audience (Patronly, crowdfunding, jams → 09), promises (09), capabilities (porting and contracts → 05, 11).
- **Fed by:** skills (02), reputation (14), traction (10), studio tier (16), incorporation (13).

## Visibility
- **V:** gig pay, hours estimate, deadline, your rating, retainer terms, grant requirements, campaign progress.
- **E:** a client's reliability (only after history or through network gossip), overrun risk (Low/Medium/High), grant odds (a band), crowdfunding forecast (a band that narrows during the campaign), Asset Bazaar demand.
- **H:** a client's true reliability and a CPI test's true retention until measured. Whether a jam game carries the viral trait.
