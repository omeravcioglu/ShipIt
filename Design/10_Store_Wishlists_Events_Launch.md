# 10 — Store Page, Wishlists, Events & Launch

This is where marketing becomes sales. The store page converts traffic. Wishlists store future buyers, and they go stale. Festivals and demos are high-stakes set pieces. Launch Week is the climax of every project.

## Decisions
- **When to open the store page,** and what to put on it (capsule art, screenshots, trailer, tags, description).
- **Which festival or showcase to use, and when.** Next Fest is **once per game**.
- **Whether to make a demo:** how long, which part of the game, how polished.
- **Price, launch discount, regional pricing, and later discounts.**
- **The release date:** competition windows, seasonality, runway, promises.
- **Launch Week:** hotfix now or later, how to respond, whether to push marketing.

---

## 1. The store page

| Component | Made by | Quality driver | Share of pre-launch conversion |
|---|---|---|---|
| **Capsule art** (main image) | Founder art, or a commission ($800–5,000) | Art skill, style pop, whether the hook is visible | 30% |
| **Trailer** | In-house or a trailer house (09) | The game's visuals + trailer craft | 25% |
| **Screenshots** (5–10, chosen from candidates) | Captured from the build | The Graphics estimate, whether they show the hook, variety | 20% |
| **Short description** | Writing | Writing skill + hook clarity | 10% |
| **Tags** (up to 20) | Player choice | Accuracy versus popularity | 10% |
| Other (features list, languages, price) | — | Localization, Deck Verified, a sensible price | 5% |

**Store page quality (0–100) sets the visit → wishlist conversion from 2% (weak) to 15% (excellent).** The brief's example: *100,000 visits → 2,000 wishlists on a weak page, 15,000 on an excellent one.* After launch, reviews dominate purchase conversion (0.5–5% per visit).

- **Screenshot selection** is a small, meaningful choice. Candidate shots are tagged (*shows hook*, *action*, *UI-heavy*, *environment*, *character*), and **the first screenshot matters most**. "Hook first" is a rule players can discover through A/B testing research or post-mortems.
- **Tags** connect the game to "More like this" traffic and tag browsing.
  - **Accurate** tags bring the right audience: higher conversion, better reviews.
  - **Misleading popular** tags bring more visits, lower conversion, **"not what I expected"** reviews and refunds.
  - Tag popularity and competition are visible with GameStatz.
- **A/B testing** (research): test capsule and screenshot variants.
- **Store page localization** costs $50–200 per language and raises conversion in that language by 20–40%.
- **Opening early** means more time to collect wishlists, but also staleness and a longer marketing commitment. 6–12 months before launch is typical for Small+ games (the simulator uses a 24-week minimum). The Vapor Direct fee is **$100 per game**.

## 2. Wishlists

**Sources:** festivals (Next Fest), creators, social posts, press, ads, word of mouth, the demo, events, **Vapor's own discovery** ("More like this", tags, Popular Upcoming), the mailing list, cross-promotion (other developers' pages, bundles).

- **Velocity matters.** The dashboard shows wishlist adds per day. Vapor's **Popular Upcoming** list ranks unreleased games by *recent* velocity. Getting on it brings a lot of extra traffic.
  - The exact rule is hidden. Players learn the rule of thumb: "about 7,500+ wishlists with strong velocity in the last two weeks before launch."
- **Wishlists go stale.** Each wishlist has an age.
  - Beyond 6 months, conversion drops 4% a month (to a floor of ×0.6).
  - Inactive pages lose ~0.5% of wishlists per week.
  - Beats refresh attention. Vapor emails wishlisters at launch and on discounts.
- **Attribution** is only shown with tracking links and analytics. Otherwise sources are just "Vapor internal" versus "External (unknown)".
- **Milestones:**

| Wishlists | Meaning |
|---|---|
| 1,000 | Validation. Publishers start noticing. |
| 7,500 | Popular Upcoming becomes reachable |
| **10,000** | The brief's goal: a solid indie launch |
| 50,000 | Publishers call you |
| 100,000+ | A breakout candidate |

- **Launch forecast:** *"Expected first-month sales: 900–2,400 units."* The band narrows with past launches and Business/Marketing knowledge.
- **Rules of thumb** (they appear in the Knowledge base once the player has experienced them):
  - First-month buyers ≈ **10–15% of wishlists**: up to 30% for beloved games, 5% for disappointing ones.
  - Year-one units ≈ **2–4× the first month.**

## 3. Pricing

- **Price tiers and value perception:** see 05.
- **Regional pricing** (a toggle, recommended): more units in lower-income regions and a small revenue gain. Without it, you get price complaints.
- **Launch discount** (0–20%): wishlisters get an email and conversion rises. Deep launch discounts slightly lower perceived value.
- **Seasonal sales:** deeper discounts spike volume but **train your audience to wait.** Vapor-style cooldown rules apply (no new discount within 4 weeks of the last one). A premium game that's "never discounted" signals value.
- **Price changes:** a price drop is news for wishlisters (a mini-spike). A price hike causes backlash, except when leaving Early Access (11).
- **Bundles** with other developers: high volume, low per-unit revenue, audience expansion.

## 4. Festivals, showcases, conferences and awards

| Event | When | Cost | Requirements | Typical result | Notes |
|---|---|---|---|---|---|
| **Vapor Next Fest** | Feb, Jun, Oct | Free | Demo + store page. **Once per game.** | +500 to 30,000+ wishlists (median ~1,500 for a decent demo) | Placement follows momentum. Streamers browse Next Fest. |
| **Vapor themed fests** (Horror, Roguelike, Cozy, Strategy…) | Various | Free | Genre match, application | +300–5,000 | Repeatable |
| **Indie showcases** (curated, online) | Various | Free–$500 submission | Trailer | +2,000–50,000 | Acceptance depends on quality and visual pop (odds shown as a band) |
| **Summer Showcase** (major) | June | Paid slot $25k–$250k, or curated | A strong trailer | +10,000–100,000+ | Big hype, and big expectations (09) |
| **DevCon** (industry) | March | $1–2.5k pass + travel | — | Publisher, press and developer contacts, talks, awards | Business networking |
| **PlayExpo** (consumer + business) | August | Booth $8–40k + travel + staff | Demo build | +1,000–10,000 wishlists, press, publisher meetings | Booth crowds give playtest data |
| **PlayersCon** (consumer) | Spring, Autumn | Booth $5–25k | Demo | Community, wishlists, creators | — |
| **Online festivals** (small) | Monthly | Free–$200 | — | +100–2,000 | Cheap |

**Implemented: Vapor Next Fest (M2).**
- **Dates.** Three fests a year (February, June, October), each starting on the first Monday on or after the 10th and lasting a week. Sign-ups close four weeks before.
- **Signing up.** From the Project app's store card. It needs a Vapor page, and it's **once per game**. A game released before its fest leaves it.
- **The demo** is made from the day panel (Build your game → Make the demo, played as Bug Squash). It needs the game 30% built.
  - **Progress:** each session adds 0.18 × efficiency, so about 7 sessions at an ordinary pace. A session also squashes 4 × efficiency of the demo's bugs; it starts with 30% of the game's.
  - **Demo quality** = the game as built so far × (0.55 + 0.45 × demo progress), minus a penalty for the demo's bugs.
- **The fest week.** It brings 900 wishlists × (demo quality ÷ 0.55)² × (genre market ÷ 3) × (1 + followers ÷ 3,000) × luck (σ 0.45). Most arrive in the first days.
  - No demo at all brings 12% of that (the page in the list). Posts that week bring 50% more wishlists.
  - In a seeded test of a 45% built Small game, no demo brought about 90 wishlists, half a demo about 470, and a finished one about 850. A balanced player's Small game reaches ~4,000 wishlists by launch.
- **The decision.** The demo's sessions are sessions the game doesn't get, and an early fest with a rough game wastes the one chance.

The themed fests, showcases, conferences and awards come later.

- **Awards:**
  - **Indie Excellence Awards** at DevCon ($100 submission). Categories: Grand Prize, Visual Art, Audio, Design, Narrative, Innovation.
  - **Golden Controllers** in December: Best Indie, Best Debut Indie. Jury nominations from released games.
  - Regional awards.
  - A win brings a **sales bump (+20–100% for several weeks)**, reputation, recruiting, publisher interest and press.
- **Attending in person** costs founder blocks (travel), money and burnout (+3–5). It pays back in contacts, press and publisher meetings.

## 5. Demos

| Demo type | Notes |
|---|---|
| Festival demo | Time-limited, built for a festival |
| Permanent demo | Keeps generating wishlists, with **demo fatigue** after ~6 months |
| **Prologue** (research unlock) | A free standalone mini-game with its own store page, reviews and followers that funnels wishlists |
| Playtest sign-ups | Collects sign-ups and interest before a demo exists |

- **Cost:** 10–20% of the vertical slice's work plus polish (1–4 weeks for a small team). **Demo quality = slice quality × polish × stability.**
- **Which part to show:**
  - **Opening:** shows onboarding, but may bury the hook.
  - **Best slice:** hook first.
  - **Sandbox:** free play.
- **Length:**
  - **Short** (15–30 minutes): hooks players and leaves them wanting more.
  - **Medium** (45–90 minutes).
  - **Long** (2+ hours): risks "I've seen enough", and streamers can cover the whole demo. Conversion drops for linear and narrative games. Roguelikes tolerate long demos.
- **Metrics** (with analytics): players, median playtime, completion %, wishlist rate among demo players (typically 10–30%), crash rate.
- **A bad demo** (bugs, poor performance, unclear hook) **lowers interest** (as the brief asks): lower conversion, negative comments, and streamers who skip it.
- **Updating** a demo after feedback is allowed and matters for the permanent demo, since Next Fest happens only once.

## 6. Release planning

- **Release Calendar:**
  - Announced major releases, plus a chance of surprise AAA "shadow drops".
  - Vapor sales and festivals, holidays.
  - Your own milestones and the promises due.
- **Launch-window competition:** a **window score** built from competing releases in your genre and audience within ±2 weeks.
  - Big AAA releases take −20–40% of attention.
  - Big-sale weeks are poor for launches (discount competition).
  - The best days of the week are hidden and learnable (Tuesday–Thursday).
- **Seasonality:**
  - Q4 is crowded, with expensive ads.
  - January is quiet, and players spend less.
  - October is great for horror.
  - December is bad for small indies.
- **Platform strategy:**
  - **PC first, consoles later:** safer. You learn from the PC launch, and the console release becomes a second marketing beat.
  - **Simultaneous:** a bigger launch, with more QA, certification and risk.
- **Deck Verified badge:** needs controller support, readable text and "OK" performance on Deck. Gives **+5–10% PC sales** and visibility on Deck.
- **Launch checklist** (visible; completion affects the launch):
  - [ ] Store page final and localized; trailer; press kit.
  - [ ] Review keys sent 2+ weeks early, with an embargo.
  - [ ] Creator keys sent; demo up to date.
  - [ ] Launch discount set; release date announced 4+ weeks ahead; mailing-list blast scheduled.
  - [ ] Community launch event planned.
  - [ ] Day-one patch plan; crash reporting integrated; support channels ready.
  - [ ] Next Fest done or planned; Deck verification requested.

## 7. Launch Week (live mode)

The climax of every project. Time switches to **hours** for 7 days. It can be auto-played for players who prefer that.

**The live dashboard shows:**
- Concurrent players and a sales ticker (units and gross).
- Review percentage and count. The label forms at 10 reviews.
- Refund rate and **top review themes**.
- A creator coverage feed and social sentiment.
- A **bug report feed** by severity.
- Wishlist conversions.
- Rank on Vapor's **Popular New Releases** (front-page visibility).
- Server status (for multiplayer games).

**Decisions during Launch Week:**

| Decision | Tradeoff |
|---|---|
| **Hotfix now** | Programmer hours plus regression risk. **Fixing crash bugs in the first 48 hours has an outsized effect on reviews.** |
| **Respond to reviews** | Developer responses help. Tone matters: defensive replies backfire. |
| **Acknowledge issues publicly** | Trust + |
| **Marketing push** | Last-minute ads or creator sponsorships to ride momentum |
| **Team pace** | Launch crunch is normal and costly: burnout +5 for everyone |

The price is **locked** during a launch discount, which is a Vapor rule players learn.

**Emotional design:** big, satisfying moments for the first 100 sales, the first "Very Positive" label and reaching the front page. A creeping dread when the crash-report feed spikes.

After Launch Week: weekly summaries, then monthly reports. The **post-mortem** comes at day 30 (17).

**Implemented: launch day and launch week, played as days (no clock, 02 §4).**

**Launch day.** Pressing Launch puts on a short show:
- The finished game's picture and title with an "OUT NOW" stamp.
- Counters ticking up to the copies and money day one is expected to bring (the first day of the sales curve, after the launch discount, refunds and the store's cut), and the launch wishlists.
- Players' first impressions popping in one by one (11 §2).

**Launch week.** For seven days the day panel opens with the morning's numbers: yesterday's copies, the reviews (or the players' mood before there are any), and the critics published so far. Each slot is a decision:

| Choice | How it plays | What it does |
|---|---|---|
| **Hotfix** | Bug Squash on your own game | Squashes bugs (like polish). Stability rises, and with it the players' **mood**: the share of the day's reviews that are positive. A fix can break something (2–5 new bugs). The chance is 16%, minus 2.5 points per Programming level (at least 3%), and ×1.5 after a rough session. |
| **Answer players** | Feedback Sort on your players' comments | Goodwill: +1.2 review points per ordinary session, up to +6 in total. Followers: +2% (×1.5 during launch week). |
| **Post about the launch** | An ordinary marketing post | Launch momentum: twice the reach. A quarter of the new followers buy at once, and on Vapor a few wishlisters are reminded too. |
| **Anything else** | Gigs, the next game, rest | The launch carries on as it is. |

**Why it matters:**
- **Early fixes count most.** Reviews are written every day in that day's mood, and most copies sell in the first days. A seeded test of a rushed tiny game ends its first month at 57.6% positive when untouched, 61.1% with two days of hotfixes at the end of the week, and 64.4% with the same work on days one and two.
- **Word of mouth.** From then on, sales run at (0.4 + 1.2 × today's mood) ÷ (0.4 + 1.2 × the mood at launch) of the launch plan.
- **Afterwards.** Seven days in, a summary arrives (copies, money, reviews, ScoreHub, what you did). Hotfixes and answers stay possible until the post-mortem on day 30, and the post-mortem notes how far they moved the reviews.

The hour-by-hour live dashboard (concurrent players, front-page rank, creator feed) stays a later goal.

---

## Connections
- **Feeds:** launch sales and the long tail (11), reviews (11), cash-flow timing (13), reputation (14), post-mortem knowledge (17).
- **Fed by:** marketing and buzz (09), game quality and stability (05), hook and idea (04), assets (06, capsule and screenshots), founder skills (02), market calendar and competitors (14).

## Visibility
- **V:** store page elements, wishlist totals and velocity, festival requirements, the release calendar (announced dates), the launch dashboard.
- **E:** store page quality score, conversion forecast, launch sales forecast, showcase acceptance odds, window score.
- **H:** the exact Popular Upcoming rule, best launch days, true demo-length effects (learned), creator coverage decisions.
