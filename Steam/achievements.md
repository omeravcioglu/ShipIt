# Steam achievements: setup sheet

Enter these in the Steamworks partner site under **App Admin → Stats & Achievements → Achievements**, then
publish (**Publish → Prepare for publishing → Publish to Steam**). The API names must match exactly: the
game unlocks achievements by these names (`Assets/IndieStudio/Simulation/Content/Achievements.cs`, which a
test keeps in sync with this sheet). For every achievement, **Set By** is "Client", and there is no progress
stat.

The icons are in [`AchievementIcons/`](AchievementIcons):
- **64 × 64 JPG:** upload these.
- **`256/` PNG:** the masters.

To redraw them after changing an achievement or its look (`Game/UI/AchievementView.cs`), run the Play-mode
tests with `INDIE_ACHIEVEMENT_ICONS=1` set.

| # | API name | Display name | Description | Hidden | Achieved icon | Unachieved icon |
|---|---|---|---|---|---|---|
| 1 | `ACH_ITS_OFFICIAL` | It's Official | Give your studio a name. | No | `ACH_ITS_OFFICIAL.jpg` | `ACH_ITS_OFFICIAL_locked.jpg` |
| 2 | `ACH_RENT_MONEY` | Rent Money | Deliver your first gig. | No | `ACH_RENT_MONEY.jpg` | `ACH_RENT_MONEY_locked.jpg` |
| 3 | `ACH_IT_COMPILES` | It Compiles! | Finish the first feature of a game. | No | `ACH_IT_COMPILES.jpg` | `ACH_IT_COMPILES_locked.jpg` |
| 4 | `ACH_SHIP_IT` | Ship It! | Release your first game. | No | `ACH_SHIP_IT.jpg` | `ACH_SHIP_IT_locked.jpg` |
| 5 | `ACH_SOMEONE_BOUGHT_IT` | Someone Bought It | Sell the first copy of a game. | No | `ACH_SOMEONE_BOUGHT_IT.jpg` | `ACH_SOMEONE_BOUGHT_IT_locked.jpg` |
| 6 | `ACH_COMING_SOON` | Coming Soon | Open a store page on Vapor. | No | `ACH_COMING_SOON.jpg` | `ACH_COMING_SOON_locked.jpg` |
| 7 | `ACH_A_REAL_INDIE_GAME` | A Real Indie Game | Release a Small game. | No | `ACH_A_REAL_INDIE_GAME.jpg` | `ACH_A_REAL_INDIE_GAME_locked.jpg` |
| 8 | `ACH_WISHLISTED` | Wishlisted | Reach 1,000 wishlists on one game. | No | `ACH_WISHLISTED.jpg` | `ACH_WISHLISTED_locked.jpg` |
| 9 | `ACH_FOUR_DIGITS` | Four Digits | Sell 1,000 copies of one game. | No | `ACH_FOUR_DIGITS.jpg` | `ACH_FOUR_DIGITS_locked.jpg` |
| 10 | `ACH_VERY_POSITIVE` | Very Positive | Release a game that players rate Very Positive. | No | `ACH_VERY_POSITIVE.jpg` | `ACH_VERY_POSITIVE_locked.jpg` |
| 11 | `ACH_FULL_TIME_INDIE` | Full-Time Indie | Live off your games: finish chapter 4. | No | `ACH_FULL_TIME_INDIE.jpg` | `ACH_FULL_TIME_INDIE_locked.jpg` |
| 12 | `ACH_BRILLIANT` | Brilliant! | Earn three stars in a work session. | No | `ACH_BRILLIANT.jpg` | `ACH_BRILLIANT_locked.jpg` |
| 13 | `ACH_PERFECTIONIST` | Perfectionist | Earn three stars in 50 work sessions. | No | `ACH_PERFECTIONIST.jpg` | `ACH_PERFECTIONIST_locked.jpg` |
| 14 | `ACH_IN_THE_ZONE` | In the Zone | Reach full flow: four sessions in a row on the same work. | No | `ACH_IN_THE_ZONE.jpg` | `ACH_IN_THE_ZONE_locked.jpg` |
| 15 | `ACH_EXPERT` | Expert | Reach level 5 in a craft skill. | No | `ACH_EXPERT.jpg` | `ACH_EXPERT_locked.jpg` |
| 16 | `ACH_FIVE_STARS` | Five Stars | Get a perfect 5/5 from a client. | No | `ACH_FIVE_STARS.jpg` | `ACH_FIVE_STARS_locked.jpg` |
| 17 | `ACH_72_HOURS` | 72 Hours | Finish a game jam. | No | `ACH_72_HOURS.jpg` | `ACH_72_HOURS_locked.jpg` |
| 18 | `ACH_DOUBLE_SHOT` | Double Shot | Drink two coffees in one day. | No | `ACH_DOUBLE_SHOT.jpg` | `ACH_DOUBLE_SHOT_locked.jpg` |
| 19 | `ACH_CAT_PERSON` | Cat Person | Pet the cat on 10 different days. | No | `ACH_CAT_PERSON.jpg` | `ACH_CAT_PERSON_locked.jpg` |
| 20 | `ACH_SELF_CARE` | Self-Care | Spend seven days on vacation. | No | `ACH_SELF_CARE.jpg` | `ACH_SELF_CARE_locked.jpg` |
| 21 | `ACH_BACK_TO_INDIE` | Back to Indie | Quit a day job to make games again. | No | `ACH_BACK_TO_INDIE.jpg` | `ACH_BACK_TO_INDIE_locked.jpg` |
| 22 | `ACH_BACK_FROM_THE_BRINK` | Back from the Brink | Climb back to $5,000 after going into debt. | No | `ACH_BACK_FROM_THE_BRINK.jpg` | `ACH_BACK_FROM_THE_BRINK_locked.jpg` |
| 23 | `ACH_THE_HARD_WAY` | The Hard Way | Collapse from burnout. | Yes | `ACH_THE_HARD_WAY.jpg` | `ACH_THE_HARD_WAY_locked.jpg` |
| 24 | `ACH_KILL_YOUR_DARLINGS` | Kill Your Darlings | Shelve a project. | Yes | `ACH_KILL_YOUR_DARLINGS.jpg` | `ACH_KILL_YOUR_DARLINGS_locked.jpg` |

**How the game unlocks them:** it checks the list after every change.
- **States** (a shipped game, a skill level) are sent as soon as they become true.
- **Moments** (two coffees today, full flow right now) are sent when they happen.

When a saved studio loads, what it earned earlier is sent again quietly, which is how achievements earned
offline reach Steam later. Steam shows its own pop-up, and the game shows a toast in its own style.
