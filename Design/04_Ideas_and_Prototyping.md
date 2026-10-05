# 04 — Ideas & Prototyping

Ideas are cheap, and development is expensive. This system lets players buy **information** cheaply before betting years on a concept, and rewards studios that prototype many ideas and develop few.

## Decisions
- **Where to look for ideas?** Each source costs different things and produces different kinds of ideas.
- **Which ideas to keep, combine or throw away?**
- **How much certainty to buy?** Each prototype question costs weeks.
- **Greenlight, iterate, shelve, pivot or kill?**

---

## 1. Idea cards

Every idea card has:
- **Working title** (generated).
- **Genre** (primary, plus an optional secondary) and **Theme**.
- **Hook:** the one-sentence twist. Examples: *"You are the dungeon."* *"Every death rewinds the whole town."* *"Fishing, but the ocean is hungry."*
- **Suggested scope band** and **source**.
- **Traits:** some visible on the card, others hidden until revealed.
- **Estimates, shown as ranges:** market size, development cost (person-weeks), fun potential, technical difficulty, streamer appeal, marketability, originality.
- **Freshness** (trend-based ideas go stale) and **maturity** (0–3: revisiting an idea adds detail and narrows ranges a little).

**Two hook properties matter everywhere:**
- **Hook strength** (hidden, 1–10): how compelling the twist is.
- **Hook clarity** (estimated): whether a stranger understands the game from one screenshot or GIF. It **multiplies all marketing** (09) and store conversion (10).

### Traits

| Trait | Visibility | Effect |
|---|---|---|
| **Trending** | V | High demand now. Decays with the trend. Saturation risk by launch. |
| **Evergreen** | V | Stable demand, smaller spikes. |
| **Experimental** | V | High Innovation. Uncertain audience (wider ranges). |
| **Niche** | V | Small, loyal audience with high price tolerance and little competition. |
| **Cozy** | V | Low-stress audience, strong communities, kinder reviews. |
| **Hardcore** | V | Small, passionate audience. Harsh on bugs and balance. |
| **Risky Tone** | V | Controversial content. Good press angle, but rating, platform and advertiser risk. |
| **Jam-proven** | V | The core-loop question is already answered. |
| **Personal Passion** | V | Founder or employee morale +, design quality +0.3. Sometimes a smaller market. |
| **Streamer Friendly** | E | +Streamer appeal (confirmed by a Market test). |
| **Highly Marketable** | E | +Hook clarity, +store conversion. |
| **Cheap to Develop** | E | −20–40% work. |
| **Content-Hungry** | E | Needs lots of content, so scope grows. |
| **Systemic** | E | Emergent gameplay and replayability, with balance and bug risk. |
| **Technically Difficult** | E | +Complexity, +bugs (confirmed by a Tech spike). |
| **Unmet Demand** | H | Big demand bonus with low saturation. Found by reading reviews or a Market test. |
| **Saturated** | H→E | Crowded. Needs a strong hook or high quality. |
| **Derivative** | H | Inspired by a specific hit. Easier to market ("an X-like"), but −Originality and clone-accusation risk. |
| **Viral Hook** | H | +Virality (09). Found through a Market test or a lucky prototype discovery. |
| **Franchise Potential** | H | A world and characters that can carry sequels (11). |
| **Late Bloomer** | H | Prototypes badly but becomes fun at full scale. The trap that experience teaches you to recognize. |

## 2. Idea sources

| Action | Cost | Tends to produce | Side benefits |
|---|---|---|---|
| **Research gaming trends** | 2 blocks + $0–300 for reports | Trending ideas, genre demand data | Refreshes Market Knowledge (it decays over ~6 months) |
| **Play games** | 1–3 blocks + the game's price | Ideas "inspired by X" (Derivative risk) | Genre Knowledge +, morale + |
| **Attend conferences** | 5–10 blocks + $800–$4,000 | Highly Marketable ideas, industry insight | Contacts (publishers, press), industry reputation |
| **Talk to developers** | 1–2 blocks | Ideas, plus **gossip** (publisher behavior, tech tips) | Relationships |
| **Analyze Vapor** | 2 blocks (detail needs a GameStatz subscription, $30/month) | Niche ideas, genre revenue medians, saturation | Market Knowledge |
| **Read player reviews** of competitors | 2 blocks | **Unmet Demand** ("players keep asking for X") | Audience Knowledge |
| **Experiment with prototypes** (toy-making) | 2–6 blocks | Experimental and Viral Hook ideas | Craft XP |
| **Game jams** (03) | A weekend or 1–2 weeks | Jam-proven and Experimental ideas | Contacts, followers |
| **Employee pitches** (07) | Free (events) | The employee's Personal Passion ideas | Morale + if used; morale − if they're repeatedly ignored |
| **Community suggestions** (09) | Free | Ideas your audience already wants | Trust + if built |
| **Post-mortems** (17) | Free | Sequel, spin-off and "what we'd do differently" ideas | — |
| **Random inspiration** | Free | Wildcards | — |

The **Idea Notebook** has no size limit. Cards can be tagged and sorted by freshness, estimated cost and estimated market.

## 3. Combining ideas

- **Merge** two cards into a new idea (for example *Deckbuilder* + *Fishing* + *Cosmic Horror*). A hidden **synergy roll** decides whether the combination is magical (+hook strength) or muddy (−clarity).
- Merged cards start **Experimental**, with wide ranges, until evaluated.
- **Genre hybrids:**
  - The primary genre defines the core audience. The secondary adds features and expectations from both genres.
  - Innovation goes up. Clarity goes down, unless the hook explains the hybrid.

## 4. Evaluating ideas cheaply (before prototyping)

| Check | Cost | Reveals |
|---|---|---|
| Quick market check | 1 block | A narrower market-size range. Accuracy depends on how fresh your Market Knowledge is. |
| Competitor scan | 1 block | Similar released or announced games and their estimated performance. Can reveal *Saturated* or *Derivative*. |
| Gut check | Free | Slightly narrower fun estimate from your Design skill. **Hidden passion bias:** you overrate your Personal Passion ideas by ~10% until a Lesson corrects it. |
| Pitch to a contact | 1 block | Hook clarity |
| Team review | Free (with employees) | Each employee's opinion, colored by their traits and genre tastes |

## 5. Prototypes: buying information

The player chooses which **questions** to answer. Each question is a small work package. Costs below are for a solo founder at skill ~3; teams are faster.

| Question | Work | Money | Reveals | Can discover |
|---|---|---|---|---|
| **Core loop:** "Is it fun?" | 1–3 person-weeks (Design + Programming) | $0–200 | Fun potential narrowed. Width shrinks with Design skill and the *Find the Fun* perk. | **"Found the fun":** an emergent mechanic raises hook strength. Or: it isn't fun, which saves the project. |
| **Tech spike:** "Can we build it?" | 0.5–2 person-weeks (Programming, Networking) | — | Technical difficulty, **named risks** ("physics over the network: very hard") and a narrower work estimate | **"Lucky bug":** a funny glitch that can become a Viral trait |
| **Visual target:** "Does it look good?" | 0.5–2 person-weeks (Art) | $0–1,500 (assets or a commission) | Screenshot appeal, art cost per content unit, style fit | Your first marketing GIFs |
| **Market test:** "Do people care?" | 0.5 person-weeks + marketing blocks (GIF posts, stranger playtests) | $0–500 | Market interest, streamer appeal, hook clarity. +50–500 followers. | **Viral Hook**, if a post takes off. Risk: the idea is now public. |

**Noise matters.** The quality of the information depends on the quality of the prototype. A rough prototype gives wide ranges and occasional **false negatives**: "It felt bad. Was that the idea or the execution?" Every reveal carries a confidence label.

**Example report (the brief's format):**

```
PROTOTYPE REPORT — "Hollow Tide"   Horror × Ocean   Hook: "The sea changes when you look away"
Gameplay Potential ..... 74–86%        (confidence: medium)
Technical Difficulty ... High           named risks: procedural ocean, stalker AI
Market Interest ........ Medium         Chirp GIF: 12k views, 3.1% engagement
Streamer Potential ..... Very High      4 reaction moments in 10 minutes of stranger playtests
Estimated work ......... 180–260 pw     (Medium scope)
Discoveries ............ "Players scream when the lighthouse turns" → Viral Hook
```

### The greenlight meeting

| Option | Result |
|---|---|
| **Greenlight** | Move to pre-production (05). 25–40% of the prototype work carries over. |
| **Iterate** | Answer another question, or re-run one with changes. |
| **Pivot** | Change the hook, genre or theme and re-evaluate. Knowledge carries over. |
| **Shelve** | Back to the notebook. Freshness decays and the team's familiarity fades. |
| **Kill** | Only knowledge (Genre Knowledge, XP) and reusable assets survive. |

## 6. Why prototyping pays (the $100,000 question)

A team of 4 on a Medium game burns about $25k a month. A 3-week, 2-person prototype costs about $6k. If it reveals fun at 45–55% and the idea isn't working, killing it saves 12+ months, roughly $300k.

Prototypes can also mislead:
- A rough prototype can produce a false negative.
- **Late Bloomer** ideas only become fun at scale.

Experience (post-mortems, Design skill, the *Playtest Whisperer* perk) teaches players when to trust a prototype. Early on, prototypes are also how a broke founder explores ideas: jam games and 2-week prototypes cost almost nothing.

## 7. Idea lifecycle and market timing

- Trend-based ideas are strongest when started in a trend's **Rising** phase. Development takes 1–2 years, so by launch the trend may be **Saturated** (14). The Market Report shows each trend's phase and a wide 12–24 month forecast band.
- *Trending* decays with the trend's phase. *Evergreen* doesn't.
- **Rival collisions:** the more generic an idea, the more likely a rival announces something similar during development (14).

---

## Connections
- **Feeds:** the project concept (05), the marketing hook (09), store page quality (10), Knowledge (17).
- **Fed by:** Research actions (02), jams (03), employees (07), community (09), market simulation (14), post-mortems (17).

## Visibility
- **V:** genre, theme, hook, visible traits, source, freshness.
- **E:** every estimate as a range with a confidence label.
- **H:** Unmet Demand, Viral Hook, Derivative, Franchise Potential, Late Bloomer, true fun and market values, and the founder's passion bias.
