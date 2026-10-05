# 08 — Studio Infrastructure

Hardware, software and space. The rule from the README applies with full force: **no flat buffs.** Every item solves a specific problem that appears at a specific scale, enables something new, or trades money for flexibility. The game makes the player **feel the problem first.**

## Decisions
- **Does this project need this machine or tool now,** or would the money be better spent on a contractor or marketing?
- **Which problems am I willing to live with?** Some tools are insurance, and insurance costs money every month.
- **Where do we work?** Home, coworking, office, or remote-first, and in which city.
- **How do we use limited office space?** More desks, or rooms that make each desk better?
- **Lease commitments:** a bigger office signals success and adds fixed costs that don't shrink when sales do.

---

## 1. Hardware

| Item | Cost | What it enables or fixes | Upkeep | Risk and notes |
|---|---|---|---|---|
| **Cheap laptop** (start) | Owned | Fine for 2D. On 3D projects **~20% of hours are lost** to builds and bakes. | — | 3%/month failure chance after year 2. Data loss if you have no backups or source control. |
| **Gaming PC** | $1,800 | 3D build/bake loss drops to ~8%. Better devlog and stream capture. | +$20/month electricity | — |
| **Developer workstation** (per seat) | $3,500 | Unreel and realistic projects become efficient (~3% loss) | +$30/month electricity | Wasted on 2D and writing roles |
| **Seat equipment tiers** (per employee) | Basic $1,200 / Standard $2,500 / Pro $4,500 (workstation, dual monitors, drawing tablet) | Output effect **depends on role × project type**: large for 3D artists, small for writers | Replacement every ~4 years | Senior hires on Basic seats: morale −3 |
| **NAS / cloud backup** | $600 or $10/month | Prevents catastrophic data-loss events | — | The cheapest insurance in the game |
| **Build server** | $4,000 | Enables *Build Automation* research (nightly builds). Fewer "broken build" events. Needed for daily playtest builds with 5+ people. | +$40/month | — |
| **Render server** | $8,000 | High-quality baked lighting for 3D; faster cinematics | +$80/month | Only matters for 3D, realistic and cinematic work |
| **Testing devices** | Mobile lab $2,000; Vapor Deck $500; **console dev kits** (loaned free once approved by a platform program, otherwise $2,500 each) | **Required to QA on that platform.** Without them, platform-specific bugs stay hidden until launch or certification. | — | Dev-kit approval needs a shipped game or a platform contact |
| **Motion capture** | Suit $15,000 / optical studio $60,000 (plus a room) | Realistic animation cost −40–60%; realistic animation quality | Maintenance | Needs an Animator to clean the data |
| **Recording booth gear** | $6,000 (plus a room) | In-house voice and foley: voice cost −30–50%, better dialogue audio | — | Only worth it with voice acting |
| **Streaming setup** | $800 | Better devlog and dev-stream quality | — | Only if you make video content |

## 2. Software

A recurring design rule: **pro tools only help people good enough to use them.** Pro art software speeds up an artist at skill 5+; it does almost nothing for a novice.

| Software | Tiers and cost | Problem it solves | You feel the need when… |
|---|---|---|---|
| Engine licenses | See 05 | — | Revenue thresholds, console export |
| Art / 3D / audio suites | Free / Pro ($30–200 per seat per month) | For skill ≥5: task speed +10% and quality ceiling +0.5 | You have a skilled specialist |
| **Source control** | None / free tier (1 GB, 3 users) / paid ($10–50 per user per month, large files) | Prevents data loss. **Required for 3+ people** (otherwise "merge chaos" events). Enables build automation. | A lost-work or overwrite event |
| **Project management** | $10 per user per month | Coordination overhead −2 points for teams of 5+. Milestone tracking for publisher reporting. | "Who's doing what?" events |
| **Analytics / telemetry** | $100–500/month, scaling with players | **Reveals retention funnels, where players quit, and feature usage** (17) | Early Access or post-launch confusion |
| **Crash reporting** | $50–300/month | Post-launch bug discovery ×3; faster hotfixes | "Crash we can't reproduce" events |
| **Automated testing infrastructure** | CI runners $50–300/month + research setup | Fewer regressions and certification failures | Repeated regressions |
| **Social scheduler** | $30/month | Posting time −30% | Marketing blocks pile up |
| **Mailing list service** | Free up to 1,000 subscribers, then ~$0.01 per subscriber per month | **Costs scale with success.** Owned-audience email campaigns. | "You've hit the free limit" |
| **Press database** | $200/month | Reach press contacts at scale | Launch outreach |
| **CreatorConnect** | Free basic / $300 per campaign (pro) | Key distribution to creators filtered by genre (09) | Influencer outreach |
| **Accounting software** | $30/month | Admin blocks − | After incorporation |
| **Legal templates** | $300 one-off | Catches some hidden contract clauses (12) | Your first contract |
| **Localization platform** | $100/month | Localization cost −15% when shipping 5+ languages | Multi-language launches |
| **GameStatz subscription** | $30/month | Market data: competitor estimates, genre medians (14) | Idea research |

## 3. Problem before solution (how infrastructure teaches itself)

Every major tool is introduced by the **problem** it solves. The tool can always be bought earlier by a player who sees the problem coming, which rewards foresight.

| Problem event | Appears when | Solution it makes desirable |
|---|---|---|
| "Your laptop died. Your last backup was 3 weeks ago." | Old hardware, no backups | NAS or cloud backup, source control |
| "Two people overwrote each other's work." | Team of 2+, no source control | Source control |
| "The build is broken the night before the milestone." | Team of 5+, no automation | Build server + automation |
| "We lost track of who's doing what." | Team of 5+, no PM tool or Producer | Project management, a Producer |
| "Players report a crash you can't reproduce." | After launch | Crash reporting |
| "Half of Early Access players quit after 20 minutes, and we don't know why." | Early Access or post-launch | Analytics |
| "Your 3D bakes take all night." | 3D project on weak hardware | Workstation, render server |
| "Nimbus certification failed: 12 platform violations." | First console submission | Testing devices, porting knowledge |
| "Voice sessions at external studios cost $180/hour." | A voice-heavy project | Recording booth |
| "Your mailing list hit the free limit." | 1,000 subscribers | A paid mailing service |

---

## 4. Offices

| Space | Desks | Room slots | Rent/month (mid-cost city) | Lease | Upsides | Downsides |
|---|---|---|---|---|---|---|
| **Bedroom** (start) | 1 | 0 | Included in rent | — | Free | Isolation (morale −1/month after 6 months without Social blocks). No in-person team. |
| **2-bedroom home office** | 2–3 | 0 | +$600 over the tiny apartment | Monthly | Cheap team room | Work and life blur (founder burnout +) |
| **Coworking / shared workspace** | Up to 8 | 0 (shared meeting rooms bookable) | $250 per desk | Monthly | **Random developer contacts** (networking), flexible | Noise, no custom rooms, a tiny leak risk |
| **Small office** | 8 | 2 | $2,200 + $300 utilities | 1 year | Identity, focus, custom rooms | First real fixed commitment |
| **Medium studio** | 20 | 5 | $7,000 + $900 | 2 years | Recruiting appeal, culture | Heavy fixed cost |
| **Large studio** | 50 | 10 | $20,000 + $2,500 | 3 years | Prestige, multiple teams | Very heavy fixed cost |
| **HQ / campus** | 120 | 20 | $55,000 + $6,000 | 5 years | Hosting events, press, maximum recruiting draw | Dangerous in a downturn |

- **Fit-out:** desks $800 each plus seat equipment; rooms $4k–60k.
- **Breaking a lease** costs 3–6 months of rent, unless the player finds a subtenant (an event chain).
- **The brief's "Office Level 2 = +10% money" is replaced by commitment.** An office is a bet that revenue will keep up with rent for the length of the lease.

### Rooms (each room takes space that could hold about 4 desks)

| Room | Cost | Effect |
|---|---|---|
| **Better desks & chairs** (office upgrade) | $400 per desk | Burnout accumulation −10% for occupants (the fix for the "back pain" event) |
| **Meeting room** | $6,000 | Coordination overhead −2 points for teams of 6+. In-house playtests and pitch meetings. Contract client meetings. |
| **Kitchen / lounge** | $8,000 (+$15 per person per month for snacks) | Morale +2 steady. More friendships form (07). |
| **Relaxation area** | $5,000 | Faster burnout recovery |
| **Focus pods** | $4,000 for 2 | **Offsets open-office noise** (more than 12 open desks = −3% output) and helps Loner traits |
| **Recording booth** | $6,000 + gear | In-house voice and foley (see hardware) |
| **Mocap area** | $15,000 + gear | See hardware |
| **Server room** | $10,000 | Hosts build and render servers without noise or heat at desks |
| **Testing lab** | $12,000 | QA efficiency +20%. Device library. Certification prep. |
| **Content studio** | $7,000 | Devlog and stream quality +. Creator visits. |
| **Showcase room** | $10,000 | Pitch success + with publishers, investors and press |
| **Training room / library** | $5,000 | Learning XP +10% |
| **Game room** | $4,000 | Morale +. "Play games" research is cheaper. |

**The space puzzle:** a Small office has 8 desks and 2 room slots. A 7th hire needs a desk, and a Meeting room would cut the chaos events you're seeing. Do you give up a room slot, move, or go hybrid?

## 5. Studio location

The location is chosen when you first take an office, or when you declare the studio remote-first. It can be changed later by relocating.

| City tier | Rent | Salaries | Talent pool | Events | Incentives |
|---|---|---|---|---|---|
| **Hub city** (tech capital) | ×1.6 | ×1.3 | Large | Conferences nearby (lower travel cost, more networking) | Varies |
| **Mid city** | ×1.0 | ×1.0 | Medium | Some | Regional grants |
| **Low-cost city** | ×0.6 | ×0.75 | Small (remote hiring needed) | Few | Often generous: grants, **tax relief up to 25%** (03) |
| **Remote-first** (no main city) | $0 office (tools + retreats at $1.5–3k per person per year) | ×0.6–1.0 by region | **Global** | Travel for events | Depends on the legal entity's location |

**Relocating** costs $5–50k. Each employee decides whether to move based on loyalty, morale and personal traits. Those who won't move go remote (if the policy allows) or quit. That's a deliberately painful, story-rich decision.

## 6. Remote, hybrid or office

| Mode | Upsides | Downsides |
|---|---|---|
| **On-site** | Chemistry +. Mentoring 30% faster. Faster onboarding. Fewer communication errors. | Office cost. Local talent only. Commute morale for some traits. |
| **Hybrid** | A balance of both | Pays for office *and* tools. Risk of a "two-tier" culture. |
| **Remote-first** | Global talent, lower cost. Loner and Night Owl traits thrive. | Coordination overhead +2–5 points (time zones). Slower mentoring. Chemistry forms slowly (retreats help). Slight leak and security risk. |

This is a real strategic axis. A remote-first studio can reach Tier 4 with no office, as long as it invests in producers, documentation and retreats.

---

## Connections
- **Feeds:** output multipliers by project type (05), bug discovery (05), coordination overhead (05, 07), morale and burnout (07), hiring pool (07), fixed costs (13), analytics information (17), platform certification (05, 10).
- **Fed by:** cash (13), team size (07), art style and engine (05), platform choices (05), research (15).

## Visibility
- **V:** costs, lease terms, desks and slots, what each item does. Tool descriptions state the *problem* they solve.
- **E:** hardware failure risk (Low/Medium/High), output gain for the current project (shown before buying, calculated from the project type).
- **H:** exact failure rolls; the noise threshold (players discover "open office noise" when it appears as an event).
