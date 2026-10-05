# 13 — Finance & Economy

All the money in one place: statements, the revenue waterfall, payout timing, taxes, borrowing, equity, economy targets per tier, and the results of the early-game simulator.

## Decisions
- **Spend, save, or invest?** Every expense competes with runway.
- **Borrow, sell equity, cut costs, or gamble?**
- **Incorporate** (unlocks, admin, taxes) and **when**.
- **How to bridge timing gaps:** revenue that's earned but not yet paid.
- **Growth pace:** scale headcount before or after proof of revenue?

---

## 1. Accounts and statements

| Account | Meaning |
|---|---|
| **Cash** | Money in the bank. Spendable now. |
| **Pending payouts** | Revenue earned on platforms, not yet paid (§3) |
| **Receivables** | Contract payments and publisher tranches due |
| **Liabilities** | Credit card, loans, taxes owed |
| **Equity** | The cap table (§8) |

- **The monthly report** (the brief's categories):
  - Revenue by source.
  - Payroll, rent, software, hardware, marketing, contractors, assets, events, legal/accounting, interest and taxes.
  - Platform fees and the publisher's share, shown in the waterfall.
  - **Burn rate**, **runway** and a **12-month forecast drawn as a band.**
- **The runway meter** is always visible in the top bar ("Payroll at risk in 38 days").
- **Budgets:** per project (plan versus actual), marketing budget, hiring plan.
- The accuracy of the forecast bands depends on Business skill (*Bookkeeper*) and a Finance Manager (17).

## 2. The revenue waterfall

Worked example: a $14.99 game sells 10,000 units in its launch month with a 10% launch discount.

| Step | Amount |
|---|---|
| Gross: 10,000 × $14.99 × 0.90 | **$134,910** |
| − Refunds (7%) | −$9,444 → $125,466 |
| − Platform fee (30%) | −$37,640 → **$87,826** |
| − Publisher share (if any, after recoup) | See 12 |
| − Engine royalty (Unreel: 5% of gross beyond $1M lifetime gross) | $0 here |
| **Developer revenue** | **$87,826**, paid ~30 days after the month closes |
| − Taxes | On annual profit (§4) |

Rule of thumb, as used by the simulator: **net per unit ≈ 58% of list price** after an average 10% discount, 8% refunds and a 30% fee. Regional taxes are abstracted into platform pricing.

## 3. Payment timing ("rich on paper, broke in the bank")

| Source | When the cash arrives |
|---|---|
| Vapor | Monthly, ~30 days after the month ends (4–8 weeks after the sale) |
| Consoles | 45–60 days after the month ends |
| Publishers | Quarterly royalty statements + ~45 days |
| Crowdfunding | ~2 weeks after the campaign closes |
| Contracts | Net-30 or Net-60 after invoicing |
| Gigs | On delivery |

A launch month can coincide with a **cash crunch**: payroll is due before the first payout arrives. **Bridges:** revenue-based financing, a line of credit, delaying hires, or careful launch timing. The Finance screen shows expected payouts on the calendar.

## 4. Taxes and incorporation

| | Sole proprietor (start) | Incorporated studio |
|---|---|---|
| Setup | — | $500–1,500 |
| Admin | Yearly filing | 1 block/week, or an accountant at $150–400/month (Business ≥5 can self-file) |
| Tax | ~25% of income over a $10k allowance (simplified; the same as the simulator) | ~21% corporate tax on **profit**. **Losses carry forward,** so early losses reduce later taxes. |
| Founder's pay | Studio money is your money | The founder draws a **salary** (minimum = living costs). Going below that temporarily costs morale and burnout. |
| Liability | Personal | Limited |
| Unlocks | — | **Publisher contracts, grants, bank loans, investors, tax relief, employees on payroll** (+20% employer costs) |

Incorporating is a meaningful milestone. It opens doors and adds overhead. The first publisher term sheet usually forces the decision.

## 5. Expense catalog (typical monthly, mid-cost city)

| Category | Bedroom | Small team (3–5) | Established (8–15) | Successful (20–40) | Major (60–120) |
|---|---|---|---|---|---|
| Living costs / founder salary | $1,460 | $3,000 | $6,000 | $10,000 | $15,000 |
| Payroll (fully loaded) | — | $10–25k | $45–100k | $130–300k | $400k–1M |
| Rent + utilities | (in living costs) | $0–2.5k | $2.5–8k | $8–25k | $25–60k |
| Software and services | $0–50 | $300–800 | $1.5–4k | $5–15k | $15–40k |
| Hardware (amortized) | $50 | $300 | $1k | $3k | $8k |
| Marketing (cycle average) | $0–300 | $1–5k | $5–30k | $20–100k | $80–400k |
| Contractors / assets | $0–500 | $1–5k | $5–20k | $10–50k | $30–150k |
| Legal / accounting | — | $300 | $1k | $3k | $8k |
| **Total burn** | **~$1.5–2.3k** | **$15–40k** | **$60–170k** | **$180–500k** | **$0.6–1.7M** |

## 6. Salary bands

Monthly, fully loaded (including ~20% employer costs), in a mid-cost city.

| Level | Programmer | Designer | Artist | Audio | Writer | QA | Producer | Marketing / Community |
|---|---|---|---|---|---|---|---|---|
| Junior | $4,200 | $3,900 | $3,800 | $3,600 | $3,500 | $3,000 | $4,200 | $3,600 |
| Mid | $6,000 | $5,500 | $5,300 | $5,000 | $4,800 | $3,800 | $6,000 | $5,000 |
| Senior | $8,500 | $7,800 | $7,500 | $7,000 | $6,800 | $4,800 | $8,500 | $7,200 |
| Lead / Director | $11,500 | $10,800 | $10,500 | $9,800 | $9,500 | $6,500 | $11,500 | $10,000 |

- **Premiums:** Network Programmer +15%, Technical Artist +10%.
- **Location:** hub city ×1.3, low-cost city ×0.75, remote international ×0.6–0.9.
- **Drift:** market salaries rise ~3% a year. Staff credited on hits expect +10–30%.
- **A fully loaded mid-level person-week costs about $1,550** (≈ $1,270 salary + ~$300 for desk, software and equipment). This is the number behind every budget in 05.

## 7. Loans

| Loan | Requirements | Amount | Rate | Terms | If you default |
|---|---|---|---|---|---|
| **Credit card** | None | $4,000 limit at start, growing with history | 24% APR | Minimum payment 3%/month | Credit score crash, collections event |
| **Family loan** | An event (once) | $2–10k | 0% | Flexible | Relationship damage, guilt (morale) |
| **Microloan / small business loan** | Incorporated, **or** 6 months of income history | $5–50k | 9–14% | 2–5 years | Hardware seized, credit frozen |
| **Bank loan / line of credit** | Incorporated + 12 months of revenue + collateral | $50k–2M | 6–11% | **Covenants** (minimum cash, debt ratio) | Covenant breach: penalty rate or the loan is called |
| **Revenue-based financing** | After a launch with a revenue history | Up to 6–12 months of projected revenue | Repay 1.15–1.35× | 10–25% of revenue until repaid | — |
| **Government-backed loan** (some regions) | Incorporated + grant history | $25–250k | 3–5% | Long | — |

## 8. Investors and equity

- **The cap table:** the founder starts at 100%. It changes through co-founder vesting (07), the employee option pool, and investors.

| Investor | Amount | Equity | Requirements | Strings |
|---|---|---|---|---|
| **Angel** | $50–250k | 8–20% | Tier 2 + a pitch (vertical slice or traction) | Personality: *Mentor* (advice events, introductions), *Silent*, or *Meddler* (opinions, pressure) |
| **VC** (seed / Series A) | $1–10M | 15–35% | Tier 3 + traction (a hit or strong growth) | A **board seat**. **Growth KPIs** (for example revenue +50%/year, a headcount plan, a second project within 12 months). Quarterly board meetings (founder blocks). Pressure events. An exit expected within 7–10 years. |
| **Strategic** (platform or publisher) | Varies | Varies | Relationship | Conflicts of interest: exclusivity expectations, first refusal on any acquisition |

- **Valuation** depends on revenue, growth, IP portfolio, reputation and team. **Down rounds** dilute more.
- **Control thresholds:**
  - Founder voting power **below 50%**: the board can override major decisions (cancellations, pivots, layoffs). Events like *"The board wants a live-service game."*
  - **Below 25%**: risk of the *"founder replaced as CEO"* event. You can stay as Creative Director or leave. This is a dramatic late-game branch.
- **Late-game tools:** **share buybacks** (regain control with profits) and **acquisition offers** from platform holders and large publishers. Accept (an ending, or continue as a subsidiary with budget approvals and mandated projects), counter, or refuse.

---

## 9. Economy targets by tier

These are design targets for tuning. "Par pace" is a competent player, not an optimal one.

| Tier | Years (par) | Team | Monthly burn | Typical project | Project budget | Revenue per release (P10 / P50 / P90) | Breakout chance |
|---|---|---|---|---|---|---|---|
| Bedroom Developer | 0–2 | 1 | $1.5–2.3k | Tiny → Small | Time + < $5k | **$6k / $16k / $45k** (first Small game, simulated year one) | ~2% |
| Small Indie Team | 2–4 | 2–5 | $12–35k | Small+ / low Medium (60–250 pw) | $80k–400k | $30k / $150k / $600k | ~4% |
| Established Indie Studio | 4–7 | 5–15 | $40–120k | Medium (250–500 pw) | $0.4–1.2M | $150k / $700k / $3M | ~6% |
| Successful Studio | 7–12 | 15–40 | $120–350k | Large (800–2,000 pw) | $1.5–5M | $1M / $4M / $15M | ~6% |
| Major Independent Developer | 10+ | 40–120 | $0.35–1.2M | Huge + multiple projects | $5–20M | $4M / $15M / $60M | ~5% |

**What these targets imply:** a single hit at Small-team tier funds about 2 years of a 5-person team. **A hit buys runway, not retirement.** Growth requires repeated good bets, a catalog, and (for many studios) partners.

## 10. Sales model reference

- **Launch month:** buyers ≈ wishlists × wishlist conversion (base 10% × (0.5 + quality), with noise) + organic discovery (~30% × quality on top).
- **Year one:** units ≈ launch month × (1.8 + 2.2 × quality), with a monthly decay of ~22% plus a long tail.
- **Virality:** games above quality 0.6 have a base ~3% chance of a breakout (median ×8). Virality traits and buzz raise this (09).
- **Rules of thumb players can learn:**
  - Launch-month buyers ≈ 10–15% of wishlists.
  - Year one ≈ 2–4× the launch month.
  - Roughly 1–3% of buyers leave a review.

## 11. Snowball and death-spiral protection

| Keeps success from trivializing the game | Keeps failure recoverable |
|---|---|
| Expectations rise with fame, price and franchise history (05) | Freelancing and day jobs are always available |
| Salaries rise with success; hits make staff targets for poaching | Grants and jams cost little |
| Growth needs bigger projects, and coordination costs scale faster than headcount | Unknown studios are judged gently (low expectations) |
| Market saturation and trend shifts | Community sympathy for transparent underdogs (a small trust bonus) |
| Franchise fatigue; the sophomore slump after a hit | Emergency options (16) |
| Investor and board pressure | Tiny projects rebuild cash, morale and audience |
| Taxes | Losses carry forward |

---

## 12. Early-game validation: `Tools/economy_sim.py`

A Monte-Carlo model of the solo years. It includes:
- Weekly time allocation and the living costs above.
- Freelance rates and gig scarcity, burnout and vacations, skill XP.
- A Tiny game then a Small game, store-page runway, wishlists, sales and payout lag, and taxes.
- Projects sized for played sessions (Tiny 1.2 pw, Small 30 pw), and the minigame **work multiplier** (`session_mult`: 1.0 is an ordinary session, careful play about 1.1–1.2), applied to gig earnings and game progress (02 §4).
- **Tired evenings:** sessions after a day-job shift count ×0.8 (part-time jobs ×0.9 on average, since only some sessions follow a shift).

It deliberately **excludes** grants, publishers, loans, crowdfunding, Patronly and events. Negative cash is allowed, to show *when* a strategy would need those tools. It is coarser than the game itself: the bot-driven balance tests in `Assets/IndieStudio/Simulation/Tests` play the real rules and are the authority. This model is for exploring strategies quickly.

**Results** (5,000 runs per strategy, seed 1; updated 2 Oct 2026: Small games back to 30 pw, plus tired evenings):

| Strategy (h/week) | Cash < 0 | Lowest cash (median) | Small game ships (median month) | Launch wishlists (median) | Small game year-1 net P10 / P50 / P90 | Breakout (≥ $100k) | Cash at month 24 / 36 (median) |
|---|---|---|---|---|---|---|---|
| All-in on own game (46) | **100%** (month 3) | −$38.7k | **8** | 3,282 | $4.2k / $11.0k / $32.3k | 1.2% | −$21.7k / −$38.7k |
| **Balanced** (48) | 1.1% | **$931** | 11 | 3,945 | $5.2k / $13.7k / $38.9k | 1.6% | $18.6k / $32.4k |
| Balanced, careful play ×1.12 (48) | 0% | $1.5k | 10 | 3,857 | $4.9k / $12.9k / $36.8k | 1.5% | $23.0k / $39.5k |
| Balanced, skip the Tiny game (48) | 1.0% | $915 | 11 | 3,651 | $4.1k / $11.1k / $32.6k | 1.1% | $16.5k / $29.6k |
| Balanced + crunch (60) | 6.1% | $663 | 11 | 4,213 | $5.6k / $14.3k / $41.6k | 1.8% | $16.1k / $27.7k |
| Freelance-heavy (48) | 0% | $3.6k | **19** | 4,488 | $5.9k / $16.3k / $47.5k | 2.4% | $46.3k / $81.3k |
| Freelance + outsource (48) | 0% | $2.9k | 12 | 4,113 | $5.5k / $14.7k / $41.6k | 2.3% | $31.8k / $58.4k |
| Part-time job (48) | 3.5% | $1.7k | 15 | 4,680 | $5.7k / $15.4k / $42.8k | 1.8% | $9.5k / $11.1k |
| Full-time job + evenings (55) | 0% | $4.9k | **Not within 36 months** | — | — | 0% | $22.5k / $31.7k |

Every strategy ships its Tiny game within the first month or two.

**What this confirms about the design:**
1. **Going all-in is a funding game.** It ships the Small game first (month 8), but without outside money it goes broke in about 3 months in every run. The player needs roughly $25–40k from crowdfunding, grants, Patronly, family or credit cards. That makes those systems essential, not optional.
2. **A balanced split survives by a thread.** The median lowest balance is $931: *every dollar matters*, and one bad event pushes a run into the red. **Playing the minigames carefully** (×1.12) ships about a month sooner and is worth about $7k by month 36, a real reward for skill that never decides the run.
3. **Ship a Tiny game first.** It takes a few weeks and makes the Small game earn about 23% more (lessons, audience carry-over, skill). That's why Chapter 1 asks for it (16 §4.1).
4. **Crunch doesn't ship sooner.** The work and the wishlist runway set the date, so 60-hour weeks buy only ~4% more year-one revenue. They cost ~28 vacation weeks over three years and make going broke 5× more likely. Sprints are a tool; sustained crunch is a trap.
5. **Freelance-heavy play is the safe, rich and slow path.** It ships 8 months later than balanced play, with the most cash ($81k by month 36). The simulator doesn't price time; the full game does, through trend drift, rival collisions, wishlist staleness and fewer games per career.
6. **Freelancing plus outsourcing is the specialist's path.** It's efficient once your hourly rate beats a contractor's, but breadth skill stagnates, vendor quality is a gamble, and management hours add up.
7. **Keeping a full-time job never ships the Small game within three years.** Evenings are few, and tired after a day's work (×0.8). It's safe ($32k by month 36) and burns the founder out (19 vacation weeks). **That's why the premise begins with quitting.** A part-time job is the middle road: the Small game at month 15, little money left over.
8. **Breakouts are rare (1.1–2.4%).** The first real game rarely makes the founder rich. It builds audience, knowledge and reputation for the second.

**How to rerun it:** `python Design/Tools/economy_sim.py --runs 5000 --seed 1` (about 15 seconds). All constants live in `CONFIG` at the top of the file. The real rules' balance tests run with `dotnet test SimHarness/Tests` (20).

## 13. Main tuning knobs

| Knob | Value | Where |
|---|---|---|
| Start cash / living costs / rent growth | $5,000 / $1,460 / 4% a year | 02 |
| Skill XP curve | 70 × L² hours per level | 02 |
| Freelance rate / gig availability | (5 + 4.5 × skill) × up to 1.25; 60% → 95% | 03 |
| Efficiency | 0.55 + 0.09 × skill | 05 |
| Burnout | +0.35 per hour over 45; −0.5 per hour under | 02 |
| Store page conversion | 2–15% | 10 |
| Wishlist conversion (launch month) | 10% × (0.5 + quality) | 10 |
| Year-one multiplier | 1.8 + 2.2 × quality | 11 |
| Net per unit | ≈ 58% of list price | §2 |
| Fully loaded person-week | ≈ $1,550 | §6 |
| Estimate optimism bias | 0.70 → 0.95 | 05 |
