# 15 — Research, Reusable Tech & Studio Perks

The brief asks for a massive research tree. A tree of "+5%" nodes would fail the decision test, so research here follows three rules:

1. **Research is done by your people.** Every node costs person-weeks of a specific discipline. Those people aren't working on your game. The opportunity cost is the decision.
2. **Nodes never give blanket percentages.** Each one **unlocks** an option, **reveals** information, **fixes a scaling problem**, or **reduces a specific cost for specific work**.
3. **Many nodes can be bought instead** (middleware, services, subscriptions), and **some have upkeep** (maintenance time or fees).

## Decisions
- **Which capability is worth pulling people off the game for?**
- **Build or buy?** Owned tech is reusable and sellable. Bought tech is faster and comes with vendor risk.
- **Which upkeep can the studio afford** in time and money?
- **When to invest ahead of need,** before the problem appears.

---

## 1. How nodes work

- **Cost:** person-weeks (pw) of a discipline, plus money for some nodes.
- **Prerequisites:** other nodes, a role hired, or **experiences**: "shipped a multiplayer game", "survived a broken launch", "ran 10 creator campaigns". You unlock things by having lived through them.
- **Upkeep:** some nodes need maintenance (pw per month) or a subscription. If you stop paying, the benefit decays.
- **Effect types:** **Unlock**, **Reveal**, **Scale**, **Reduce**.
- **Research projects** appear in the Project screen next to games, so the tradeoff is visible on the same timeline.

## 2. The tree

### Development (craft pipelines)

| Node | Cost | Prerequisite | Effect | Upkeep | Buy instead |
|---|---|---|---|---|---|
| Rapid Prototyping Kit | 3 pw P + 1 pw D | — | Prototypes 30% faster; stronger jam games | — | Bazaar templates |
| Internal Level Editor | 6 pw P | Shipped a level-based game | Level content −20% in level-based genres; player-facing Level Editor feature −50% | 0.2 pw/month | — |
| Dialogue System | 4 pw P + 1 pw W | — | Branching Dialogue −30%; localization-ready text | — | Middleware $3k |
| Procedural Toolkit | 8 pw P + 2 pw D | Used Procedural Generation once | Procedural Generation −40%; its edge-case bugs −30% | 0.2 pw/month | Middleware |
| Save System Framework | 3 pw P | A save-corruption event, **or** Programming ≥4 | Robust Save System −70% across projects | — | — |
| Accessibility Toolkit | 4 pw P + 2 pw D | — | Accessibility Suite −50%; +award odds | — | — |
| Animation Pipeline (retargeting) | 5 pw P + 3 pw An | Animator hired | 3D animation cost −25% | — | Plugin $2k |
| Style Guide & Art Bible | 2 pw A | Art Director or Art L7 | Consistency penalties −50%; outsourced art +1 quality | Update per new style | — |
| Demo & Prologue Pipeline | 2 pw P + 1 pw D | One Next Fest done | Demos −40% work; **unlocks Prologue demos** (10) | — | — |

### Technology

| Node | Cost | Prerequisite | Effect | Upkeep | Buy instead |
|---|---|---|---|---|---|
| Networking Stack I (P2P + host migration) | 8 pw N | Network Programmer | Online Multiplayer −30% work; desync bugs −30% | 0.3 pw/month | Framework $5–15k |
| Networking Stack II (dedicated servers + matchmaking) | 12 pw N | Stack I + shipped a multiplayer game | Dedicated servers and matchmaking −40%; server costs −20% | 0.5 pw/month | Backend subscription |
| Rollback Netcode | 10 pw N | Stack I | **Unlocks** the rollback option (best feel for action, fighting, party) | — | — |
| Crossplay Layer | 8 pw N + P | Stack II + 2 console platforms | Crossplay −50% | — | — |
| Optimization Profiler Suite | 4 pw P | A "Poor performance" review | **Reveals** exact performance load; optimization +30% efficiency | — | — |
| Console Porting Layer | 10 pw P | One completed port (your own or a contract) | Port cost −40%; certification failures −50% | 0.3 pw/month per platform | Porting partner |
| Custom Shader Library | 4 pw (Tech Art + P) | Technical Artist | +0.3 visual quality for your 3D styles in future projects (a reusable "look") | — | Shader packs (not exclusive) |
| Physics Middleware Integration | 3 pw P | Used the Physics feature | Physics bugs −40% | — | License $5k |
| **Custom Engine** (capstone) | 60–120 pw P | *Engine Whisperer* or Lead Programmer ≥8; 3 shipped games | No fees or royalties; +10% efficiency on your core genres; new hires onboard 50% slower for 3 months | **2 pw/month** | — |

### Production

| Node | Cost | Prerequisite | Effect | Upkeep |
|---|---|---|---|---|
| Source Control Discipline | 1 pw P | Team ≥2 | Ends merge-chaos events. Needed for Build Automation. | Subscription (08) |
| Documentation Culture | 2 pw spread across the team | Team ≥5 | Bus-factor risk ↓. Onboarding −30%. | 2% of team time |
| Agile Estimation | 1 pw + Producer | Shipped 1 project | Estimate noise −30% | — |
| Milestone Planning | 1 pw (Producer) | A publisher deal | Better milestone definitions in negotiation; higher pass rate | — |
| Automated Testing | 6 pw P | Build server | Regressions −50%; certification failures −30% | 0.3 pw/month |
| Build Automation (CI) | 4 pw P | Build server + source control | Nightly builds: cheaper playtests, Early Access updates −30% effort | 0.2 pw/month |
| Crash Reporting Integration | 1 pw P | Shipped a game | Post-launch bug discovery ×3 | Subscription |
| Outsourcing Management | 2 pw (Producer) | 3 outsourcing contracts | Management hours per outsourced week −40%; vendor reliability **revealed** after 1 contract | — |
| Multi-team Production | 4 pw (Producer) | 2 concurrent projects | Shared-service conflicts −40%; **unlocks** the allocation planner | — |

*The **Code Review** practice is a policy (07), not research. Bugs −20%, feature speed −5%.*

### Marketing

| Node | Cost | Prerequisite | Effect |
|---|---|---|---|
| Social Media Playbook | 1 pw | 50 posts made | Posting time −25%; best-window hints |
| Trailer Craft | 3 pw (Marketing + Art) | 1 trailer made | In-house trailer quality +1.5 |
| Store Page A/B Testing | 2 pw | A store page live | **Reveals** the better capsule and screenshot variants (typically +10–25% conversion from picking winners) |
| **Influencer Database** | 3 pw + $500 | 10 creator campaigns | **Reveals** creator genre preferences and conversion estimates |
| Press Relations | 2 pw | 5 press contacts | Better angle targeting; better-timed review keys |
| Ads Attribution | 3 pw + analytics | $5k+ spent on ads | **Reveals** ad ROI and "Unknown source" wishlists |
| Launch Playbook | 2 pw | 2 launches | Auto-generated checklist tasks; Launch Week decision previews |
| Festival Strategy | 1 pw | 2 festivals | Festival wishlist forecasts; demo-metrics analysis |
| Community → Wishlist Funnel | 2 pw | Chorus at 2k members | Higher conversion from community to wishlists and mailing list |

### Business

| Node | Cost | Prerequisite | Effect |
|---|---|---|---|
| Financial Forecasting | 2 pw (Business / Finance) | Incorporated | Cash forecast bands −50% |
| Legal Review Process | $3k + 1 pw | 1 signed contract | Every future contract reveals ~70% of hidden clauses |
| Market Research Methods | 2 pw | 5 research actions | Market Knowledge decays 50% slower; narrower forecasts |
| Platform Relations | 2 pw + events | 1 console release | Faster dev-kit approvals; featuring chance + |
| Tax Optimization | 1 pw + accountant | A profitable incorporated year | Better tax-relief claims; effective tax −2 points |
| Merchandising | 2 pw | Brand "Recognized" | **Unlocks** merch (print-on-demand, bulk, partner; 11) |
| Licensing Department | 3 pw | A franchise | **Unlocks** licensing deals (comics, board games, TV event chance) |
| Investor Relations | 2 pw | Investors | Board meetings take half the time; valuation +5% |

### Community

| Node | Cost | Prerequisite | Effect |
|---|---|---|---|
| Community Guidelines & Moderation Tools | 1 pw | Chorus at 1k | Toxicity −40%; moderation hours −30% |
| Chorus Integration (bots, roles) | 1 pw P | Chorus at 500 | Activity decay −25%; automatic playtest sign-ups |
| **Playtest Program** | 2 pw | Chorus at 2k | Recurring community playtests: free findings and bug reports (leak risk) |
| Modding SDK | 8 pw P | Shipped Mod Support once | Mod Support −50%; mod-scene chance ↑ |
| Creator Program | 2 pw + budget | Influencer Database | Long-term creator partnerships (codes, early builds); relationship growth ×2 |
| Newsletter Automation | 1 pw | 1,000 subscribers | Mailing-list conversion +20% |

### Publishing (late game)

| Node | Cost | Prerequisite | Effect |
|---|---|---|---|
| **Publishing Label** | 4 pw + a $250k fund | Tier 4 (Tier 3 with *Portfolio Thinker*) | **Unlocks** publishing other studios' games (12, 16) |
| Scouting Network | 2 pw | Label | More and better pitches; more accurate slice evaluations |
| Label Services | Varies | The matching in-house capability (QA, porting, localization) | Offer your capabilities to signed studios: revenue and relationships |
| Portfolio Analytics | 2 pw | 3 external titles | Portfolio risk and recoupment dashboards |

## 3. Reusable tech and tech aging

- Code and tools built by the studio go into the **Tech Library** with a version and **technical debt** (05).
- **Reuse** saves 10–40% on the relevant features in new projects, and brings the debt along.
- **Tech ages:** each engine generation, reusable tech needs an update (pw), or its savings shrink. That's upkeep again.
- Tech can be **productized** and sold on the Asset Bazaar (03), which turns R&D into income.

---

## 4. Studio perks (earned, and double-edged)

The brief's perks, and others, are **earned by patterns of behavior**. Each one carries a shadow.

| Perk | Earned by | Advantage | Shadow |
|---|---|---|---|
| **Recognizable Brand** | 3 Positive releases + Awareness ≥ "Respected" | Store conversion from brand; fans follow you across genres | Expectations +; scandals amplified |
| **Publisher Connections** | 2 publisher deals with every milestone met | Inbound offers; opening terms +5% | Partners expect first-look rights |
| **Veteran Developers** | 3+ employees with 5+ years' tenure | Mentoring +; fewer bugs; better estimates | Higher salaries; resistance to change (morale − on new engines or genres) |
| **Fan Community** | Chorus 10k+ with trust ≥ "Respected" | Guaranteed day-one buyers; free QA volunteers | Entitlement; drama scales with size |
| **Better Recruitment** | Gamedoor ≥4.2 for 2 years | Candidate pool +1 quality tier | Candidates expect the culture to hold: crunch or layoffs cost double |
| **Media Attention** | An award win or a viral hit | Press coverage chance +; ready-made angles | Scrutiny: controversies amplified, "hot take" reviews |
| **Platform Partner** | 2 well-rated console launches | Dev kits, featuring, platform funding offers | Platforms expect timed exclusives |
| **Genre Master** | 3 well-reviewed games in one genre | Genre fans +25%; design +0.3 in that genre | Expectations in the genre rise; repeating yourself costs Innovation |
| **Indie Darling** | 2 awards | Festival acceptance +; creator interest + | "Sold out" backlash if you take VC money or a AAA publisher deal |
| **Lean Machine** | 3 profitable years with ≤10 staff | Overhead efficiency | Rapid hiring causes culture shock (morale −) |

---

## Connections
- **Feeds:** feature costs (05), bug rates (05), information reveals (17), marketing efficiency (09), deals (12), team structure (07), income (03, productized tech).
- **Fed by:** team capacity (07), experiences from every system (the prerequisites), cash (13).

## Visibility
- **V:** node costs, prerequisites, effects (stated precisely), upkeep.
- **E:** the time to complete (the same estimate calibration as projects, 05).
- **H:** nothing important. Research is the one system where the player gets exact information. Its uncertainty is *strategic*: will you actually need it?
