using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Runs one playthrough. Deterministic for a given seed and command sequence. Time only moves when the
    /// player spends a slot of the day (morning, afternoon, evening) and goes to sleep (Design/02 §4):
    /// <see cref="PerformSlot"/> three times, then <see cref="Sleep"/>. Bots use <see cref="AdvanceDay"/>.
    /// </summary>
    public sealed partial class GameSimulation
    {
        /// <param name="edition">The full game, or the free demo (<see cref="DemoRules"/>).</param>
        public GameSimulation(ulong seed, Edition edition = Edition.Full)
        {
            State = new GameState { Seed = seed, Edition = edition };
            State.RngGigs = Stream(seed, "gigs", 1);
            State.RngEvents = Stream(seed, "events", 2);
            State.RngSales = Stream(seed, "sales", 3);
            State.RngWork = Stream(seed, "work", 4);
            State.RngIdeas = Stream(seed, "ideas", 5);
            State.RngMarketing = Stream(seed, "marketing", 6);
            SetupNewGame();
            StartDay();
        }

        GameSimulation(GameState loaded)
        {
            State = loaded;
        }

        /// <summary>Writes the whole game to a stream (<see cref="SaveGame"/>).</summary>
        public void Save(System.IO.Stream stream) => SaveGame.Write(State, stream);

        /// <summary>
        /// Continues a saved game exactly where it was left, mid-day included. Throws
        /// <see cref="System.IO.InvalidDataException"/> for a damaged or unsupported save.
        /// </summary>
        public static GameSimulation Load(System.IO.Stream stream) => new GameSimulation(SaveGame.Read(stream));

        public GameState State { get; }
        public int Day => State.Day;
        public Founder Founder => State.Founder;
        public Finance Finance => State.Finance;

        /// <summary>0 = morning, 1 = afternoon, 2 = evening, 3 = night (every slot of the day is used).</summary>
        public int Slot => State.Slot;
        public bool IsNight => State.Slot >= WeekPlan.BlocksPerDay;
        public bool IsDemo => State.IsDemo;

        /// <summary>Time no longer moves: the studio closed, or the demo is over.</summary>
        bool Stopped => State.IsGameOver || State.DemoOver;

        /// <summary>Raised for every new inbox message.</summary>
        public event Action<Message> MessagePosted;

        /// <summary>Raised when a day ends, with the new day index.</summary>
        public event Action<int> DayAdvanced;

        static SimRandom Stream(ulong seed, string name, ulong id) => new SimRandom(seed ^ StableHash.Fnv1a64(name), id);

        void SetupNewGame()
        {
            var f = State.Founder;
            f.Skills[Skill.Programming] = 2;
            f.Skills[Skill.Design] = 2;
            f.Skills[Skill.Art] = 1.5;
            f.Skills[Skill.Writing] = 1.5;
            f.Skills[Skill.Audio] = 1;

            var fin = State.Finance;
            fin.Cash = Tuning.StartCash;
            fin.SetRecurring("rent", "Rent (tiny apartment)", Tuning.RentApartment, TxCategory.Rent);
            fin.SetRecurring("living", "Food & living", Tuning.FoodAndLiving, TxCategory.Living);
            fin.SetRecurring("utilities", "Utilities & electricity", Tuning.Utilities, TxCategory.Utilities);
            fin.SetRecurring("internet", "Internet", Tuning.Internet, TxCategory.Utilities);
            fin.SetRecurring("phone", "Phone", Tuning.Phone, TxCategory.Utilities);

            State.Plan = PlanTemplates.Standard();
            AddIdea(GenerateIdea("Starter idea", 1));
            AddIdea(GenerateIdea("Starter idea", 1));

            PostWelcome();
            OfferFriendGig();
            GenerateGigOffers(0);
        }

        // ------------------------------------------------------------------ time: a day is three slots

        /// <summary>
        /// Wakes up: money that is due arrives, the 1st of the month brings the bills and the monthly report,
        /// Monday brings new gig offers and the week's events. Runs once per day.
        /// </summary>
        public void StartDay()
        {
            if (Stopped || State.DayStarted) return;
            int day = State.Day;
            if (State.IsDemo && day >= DemoRules.Days)
            {
                State.DemoEnd = DemoEnd.OutOfTime; // the demo's two weeks are up: this morning never starts
                return;
            }
            State.DayStarted = true;
            State.Slot = 0;
            State.Today.Clear();
            State.CoffeeReady = false; // yesterday's coffee went cold
            State.CoffeesToday = 0;

            ProcessScheduledPayments(day);
            FinishLaunchWeeks(day);
            StartFests(day);
            if (day > 0 && GameCalendar.IsFirstOfMonth(day)) OnMonthStart(day);
            if (State.IsGameOver) return;
            if (day > 0 && GameCalendar.IsMonday(day)) OnWeekStart(day);
            if (State.Founder.IsResting(day)) State.Stats.VacationDays++;
            if (State.IsDemo && day == DemoRules.Days - DemoRules.WarningDaysLeft && State.ShippedGames == 0) PostDemoWarning();
        }

        /// <summary>The activity the current slot must be (a rest day, the day job, a jam), or null when it's your choice.</summary>
        public PlanBlock? ForcedActivity() =>
            State.DayStarted && State.Slot < WeekPlan.BlocksPerDay ? ForcedBlock(State.Day, State.Slot) : null;

        /// <summary>
        /// Spends the current slot (4 hours) on an activity and moves on to the next one. <paramref name="performance"/>
        /// is the work multiplier earned in the activity's minigame (<see cref="Rules.MinigameMultiplier"/>); 1 is an
        /// ordinary session. A forced slot (rest day, day job, jam) ignores the choice.
        /// </summary>
        public SlotReport PerformSlot(PlanBlock activity, double performance = 1.0)
        {
            if (Stopped) return null;
            if (!State.DayStarted) StartDay();
            if (Stopped || State.Slot >= WeekPlan.BlocksPerDay) return null;

            int day = State.Day, slot = State.Slot;
            var forced = ForcedBlock(day, slot);
            var block = forced ?? activity;
            double multiplier = forced.HasValue ? 1.0 : Math.Max(0.1, performance);
            if (!forced.HasValue && activity.IsWork && performance >= Rules.MinigameMultiplier(Tuning.ThreeStarScore) - 1e-9) State.Stats.BrilliantSessions++;
            bool afterWork = IsAfterWork(block);
            if (afterWork) multiplier *= Tuning.AfterJobFatigue;
            double flow = ApplyFlow(block, forced.HasValue);
            multiplier *= flow;
            bool coffee = State.CoffeeReady && !forced.HasValue && block.IsWork;
            if (coffee)
            {
                multiplier *= Tuning.CoffeeBoost;
                State.CoffeeReady = false;
            }

            var report = ExecuteBlock(day, slot, block, multiplier);
            if (flow > 1.0001) report.Lines.Add($"Flow: session {State.FlowStreak} in a row, ×{flow:0.00}.");
            if (coffee) report.Lines.Add($"Coffee: ×{Tuning.CoffeeBoost:0.00}.");
            if (afterWork) report.Lines.Add($"Tired after the day job: this session counted ×{Tuning.AfterJobFatigue:0.0}.");
            report.Forced = forced.HasValue;
            State.Today.Add(report);
            State.Slot = slot + 1;
            CheckProgress();
            return report;
        }

        /// <summary>
        /// True when the current slot comes after a day-job shift today: a tired evening gets less done
        /// (<see cref="Tuning.AfterJobFatigue"/>). It's what makes keeping the job slower than its hours suggest.
        /// </summary>
        public bool IsAfterWork(PlanBlock activity) =>
            activity.IsWork && activity.Kind != ActivityKind.DayJob && State.Today.Any(r => r.Activity.Kind == ActivityKind.DayJob);

        /// <summary>"Call it a day": every remaining slot of today becomes rest.</summary>
        public void RestForTheDay()
        {
            while (!Stopped && State.DayStarted && State.Slot < WeekPlan.BlocksPerDay) PerformSlot(PlanBlock.Rest);
        }

        /// <summary>
        /// Ends the day: unused slots are rest, sales come in, deadlines are checked, and on Sunday night the
        /// week's hours turn into burnout and morale. The next day starts with <see cref="StartDay"/>.
        /// </summary>
        public void EndDay()
        {
            if (Stopped) return;
            if (!State.DayStarted) StartDay();
            RestForTheDay();
            if (Stopped) return;

            int day = State.Day;
            ProcessSales(day);
            ProcessFests(day);
            CheckGigDeadlines(day);
            CheckProjectMilestones(day);
            if (GameCalendar.DayOfWeek(day) == 6) FinalizeWeek(day);
            AutoResolveExpiredMessages(day);
            CheckCashWarnings();
            CheckProgress();

            State.Day = day + 1;
            State.DayStarted = false;
            State.Slot = 0;
            DayAdvanced?.Invoke(State.Day);
        }

        /// <summary>Goes to bed: ends today and wakes up tomorrow.</summary>
        public void Sleep()
        {
            EndDay();
            StartDay();
        }

        /// <summary>Plays the rest of the day from the repeating week plan (bots, tests and balancing).</summary>
        public void AdvanceDay()
        {
            if (Stopped) return;
            if (!State.DayStarted) StartDay();
            while (!Stopped && State.Slot < WeekPlan.BlocksPerDay)
                PerformSlot(State.Plan[GameCalendar.DayOfWeek(State.Day), State.Slot]);
            EndDay();
        }

        public void AdvanceDays(int days)
        {
            for (int i = 0; i < days && !Stopped; i++) AdvanceDay();
        }

        /// <summary>Runs until the next Monday.</summary>
        public void AdvanceToNextWeek()
        {
            do AdvanceDay();
            while (!Stopped && !GameCalendar.IsMonday(State.Day));
        }

        // ------------------------------------------------------------------ commands: the week plan (bots) and time off

        public void SetPlanBlock(int dayOfWeek, int block, PlanBlock value) => State.Plan[dayOfWeek, block] = value;

        public void ApplyPlan(WeekPlan plan) => State.Plan.CopyFrom(plan);

        /// <summary>Takes the next <paramref name="days"/> days off, starting today (today's remaining slots become rest).</summary>
        public void TakeVacation(int days)
        {
            State.Founder.VacationUntilDay = Math.Max(State.Founder.VacationUntilDay, State.Day + days - 1);
        }

        public void QuitDayJob()
        {
            if (State.Founder.Job == DayJob.None) return;
            PayDayJobUntil(State.Day);
            State.Founder.Job = DayJob.None;
            State.Founder.JobStartDay = -1;
            Post("job_quit", MessageKind.Info, "You quit the day job", "Your time is your own again. So is the rent.");
        }

        // ------------------------------------------------------------------ commands: shop

        public bool Buy(string itemId, out string error)
        {
            error = null;
            var item = ShopCatalog.Get(itemId);
            if (item == null) { error = "Unknown item."; return false; }
            if (State.Owned.Contains(itemId)) { error = "You already have this."; return false; }
            if (item.Price > 0 && State.Finance.Cash - item.Price < -State.Finance.CreditLimit) { error = "Not enough money or credit."; return false; }

            if (item.Price > 0) State.Finance.Apply(State.Day, -item.Price, item.Category, item.Name);
            if (item.Monthly > 0) State.Finance.SetRecurring(item.Id, item.Name, item.Monthly, item.Category);
            State.Owned.Add(itemId);
            if (itemId == "gaming_pc" || itemId == "cooling_pad") State.Flags.Remove("laptop_risky");
            return true;
        }

        public void CancelSubscription(string itemId)
        {
            var item = ShopCatalog.Get(itemId);
            if (item == null || item.Monthly <= 0 || item.Price > 0) return;
            State.Finance.RemoveRecurring(itemId);
            State.Owned.Remove(itemId);
        }

        // ------------------------------------------------------------------ inbox

        public void MarkRead(int messageId)
        {
            var m = State.Inbox.Find(messageId);
            if (m != null) m.IsRead = true;
        }

        public void ChooseOption(int messageId, int option)
        {
            var m = State.Inbox.Find(messageId);
            if (m == null || !m.NeedsDecision || option < 0 || option >= m.Options.Count) return;
            m.ChosenOption = option;
            m.IsRead = true;
            m.Outcome = ResolveEvent(m, option);
        }

        Message Post(string eventId, MessageKind kind, string title, string body, int contextId = 0, int expiresInDays = -1, params string[] options)
        {
            var m = new Message
            {
                Id = State.NextId++,
                Day = State.Day,
                EventId = eventId,
                Kind = kind,
                Title = title,
                Body = body,
                ContextId = contextId,
                ExpiresDay = expiresInDays > 0 ? State.Day + expiresInDays : -1,
            };
            foreach (var option in options) m.Options.Add(new MessageOption { Label = option });
            State.Inbox.Messages.Add(m);
            State.EventLastDay[eventId] = State.Day;
            MessagePosted?.Invoke(m);
            return m;
        }

        void AutoResolveExpiredMessages(int day)
        {
            foreach (var m in State.Inbox.Messages.Where(x => x.NeedsDecision && x.ExpiresDay >= 0 && x.ExpiresDay <= day).ToList())
            {
                m.ChosenOption = m.DefaultOption;
                m.Outcome = "(No answer — defaulted.) " + ResolveEvent(m, m.DefaultOption);
            }
        }

        bool OnCooldown(string eventId, int days) =>
            State.EventLastDay.TryGetValue(eventId, out int last) && State.Day - last < days;

        /// <summary>Completes side goals and story chapters whose conditions are now met. Runs after every slot and command.</summary>
        public void CheckProgress()
        {
            if (State.IsGameOver) return;
            CheckGoals(State.Day);
            CheckChapter();
            if (State.IsDemo && State.DemoEnd == DemoEnd.None && State.ShippedGames >= 1) State.DemoEnd = DemoEnd.Shipped;
        }

        public ChapterDef CurrentChapter => ChapterCatalog.All[Math.Min(State.Chapter, ChapterCatalog.All.Count - 1)];

        void CheckGoals(int day)
        {
            foreach (var goal in GoalCatalog.All)
            {
                if (State.GoalsCompleted.ContainsKey(goal.Id) || !goal.IsComplete(State)) continue;
                State.GoalsCompleted[goal.Id] = day;
                State.Founder.Morale = Rules.Clamp(State.Founder.Morale + 3, 0, 100);
                Post("goal", MessageKind.Info, "Goal complete: " + goal.Title, "Small wins keep you going. Morale +3.");
            }
        }

        void CheckChapter()
        {
            while (State.Chapter < ChapterCatalog.All.Count - 1)
            {
                var chapter = ChapterCatalog.All[State.Chapter];
                if (!chapter.IsComplete(State)) return;
                State.ChaptersCompleted[chapter.Id] = State.Day;
                State.Chapter++;
                State.ChapterStartDay = State.Day;
                State.Founder.Morale = Rules.Clamp(State.Founder.Morale + 8, 0, 100);
                var next = ChapterCatalog.All[State.Chapter];
                string nextText = next.Steps.Count > 0
                    ? $"\nChapter {next.Number}: {next.Title}. Your goal: {next.Goal.ToLowerInvariant()}."
                    : "\nThat's every chapter so far. Hiring, publishers and a real studio arrive in a future update. Keep growing!";
                Post("chapter_complete", MessageKind.Info, $"Chapter {chapter.Number} complete: {chapter.Title}", chapter.Outro + nextText);
            }
        }

        void GameOver(string reason)
        {
            if (State.IsGameOver) return;
            State.GameOverReason = reason;
            Post("game_over", MessageKind.Critical, "The studio is closed", reason);
        }
    }
}
