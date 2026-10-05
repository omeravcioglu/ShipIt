using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>GigGrid freelancing: the survival backbone (Design/03 §2).</summary>
    public sealed partial class GameSimulation
    {
        static readonly ClientType[] ClientTypes = { ClientType.SmallStudio, ClientType.Agency, ClientType.Startup, ClientType.Corporate };

        static readonly Dictionary<Skill, string[]> GigTitles = new Dictionary<Skill, string[]>
        {
            { Skill.Programming, new[] { "Fix bugs in a mobile puzzle game", "Build an inventory UI", "Port a jam game to the web", "Write a save system", "Hook up controller support", "Optimize a laggy level" } },
            { Skill.Design, new[] { "Balance a card game's economy", "Design five platformer levels", "Write a pitch document", "Tune enemy difficulty", "Map out a tutorial" } },
            { Skill.Art, new[] { "Draw twenty item icons", "Animate a character sprite sheet", "Model low-poly props", "Paint a key-art background", "Redesign a menu" } },
            { Skill.Audio, new[] { "Make a UI sound pack", "Compose a two-minute loop", "Record footstep foley", "Mix a trailer" } },
            { Skill.Writing, new[] { "Proofread game text", "Write item descriptions", "Write store-page copy", "Edit a dialogue script" } },
        };

        // ------------------------------------------------------------------ commands

        public bool AcceptGig(int gigId, out string error)
        {
            error = null;
            var gig = State.Freelance.Gigs.FirstOrDefault(g => g.Id == gigId);
            if (gig == null || gig.Status != GigStatus.Offered) { error = "That offer is no longer available."; return false; }
            if (State.Freelance.ActiveCount >= Tuning.MaxActiveGigs) { error = $"You can juggle at most {Tuning.MaxActiveGigs} gigs at once."; return false; }
            gig.Status = GigStatus.Active;
            gig.DeadlineDay = State.Day + gig.DurationDays;
            CheckProgress();
            return true;
        }

        /// <summary>Work sessions a gig still needs at an ordinary pace (4 hours each).</summary>
        public double SessionsLeft(Gig gig) =>
            Math.Max(0, gig.Hours - gig.HoursDone) / Math.Max(0.5, Tuning.BlockHours * Rules.Productivity(State).Value);

        public void DeclineGig(int gigId)
        {
            var gig = State.Freelance.Gigs.FirstOrDefault(g => g.Id == gigId);
            if (gig != null && gig.Status == GigStatus.Offered) gig.Status = GigStatus.Declined;
        }

        public void SetRateSetting(RateSetting setting) => State.Freelance.Profile.RateSetting = setting;

        public Breakdown ExplainGigRate(Skill skill) =>
            Rules.FreelanceRate(State.Founder.Get(skill), State.Freelance.Profile.Rating, State.Freelance.Profile.RateSetting);

        // ------------------------------------------------------------------ work

        void DoFreelance(int day, PlanBlock plan, double performance)
        {
            var gig = plan.Option < 0 ? null
                : (plan.Option > 0 ? State.Freelance.Active.FirstOrDefault(g => g.Id == plan.Option) : null)
                  ?? State.Freelance.Active.OrderBy(g => g.DeadlineDay).FirstOrDefault();
            if (gig == null)
            {
                // No gig to work on: the slot goes into prospecting, which sometimes turns up a new offer.
                State.CurrentWeek.IdleFreelanceBlocks++;
                State.Stats.IdleFreelanceBlocks++;
                if (_report != null) _report.Title = "Looked for clients";
                if (State.RngGigs.Chance(0.5))
                {
                    var found = CreateGig(day, State.RngGigs);
                    found.RiskFlags = string.IsNullOrEmpty(found.RiskFlags) ? "Found by prospecting" : "Found by prospecting · " + found.RiskFlags;
                    State.Freelance.Gigs.Add(found);
                    Note($"New offer in GigGrid: \"{found.Title}\" for {Rules.Money(found.Pay)}.");
                }
                else Note("Nothing today. Offers also arrive every Monday.");
                GainXp(Skill.Business, Tuning.BlockHours * 0.5);
                return;
            }

            SetSubject($"Worked on \"{gig.Title}\"", gig.Title, gig.Discipline, gig.Progress);
            double hours = Tuning.BlockHours * Rules.Productivity(State).Value * performance;
            gig.HoursDone += hours;
            gig.Sessions++;
            gig.PerformanceSum += performance;
            State.CurrentWeek.FreelanceHours += Tuning.BlockHours;
            State.Freelance.Profile.TotalHours += Tuning.BlockHours;
            GainXp(gig.Discipline, Tuning.BlockHours * Rules.Productivity(State).Value);
            if (_report != null) _report.ProgressAfter = Math.Min(1, gig.Progress);
            Note($"{Math.Min(gig.Hours, gig.HoursDone):0.#} of {gig.Hours:0} hours done for {gig.ClientName}.");

            if (!gig.ScopeCreepChecked && gig.Progress >= 0.5)
            {
                gig.ScopeCreepChecked = true;
                if (State.RngGigs.Chance(gig.ScopeCreepChance)) PostScopeCreep(gig);
            }
            if (gig.HoursDone >= gig.Hours) CompleteGig(day, gig);
        }

        void CompleteGig(int day, Gig gig)
        {
            var f = State.Founder;
            var profile = State.Freelance.Profile;
            double raw = 3.2 + (f.Get(gig.Discipline) - gig.RequiredSkill) * 1.2 + State.RngGigs.Gaussian() * 0.7
                         - (day > gig.DeadlineDay ? 1.0 : 0) - Math.Max(0, f.Burnout - 50) * 0.02 + gig.ScoreModifier
                         + Tuning.GigScorePerPerformance * (gig.AveragePerformance - 1);
            int score = (int)Rules.Clamp(Math.Round(raw), 1, 5);
            gig.ClientScore = score;
            gig.Status = GigStatus.Completed;
            profile.AddScore(score);
            profile.CompletedGigs++;

            string stars = $"{score}/5";
            if (State.RngGigs.Chance(gig.NonPayRisk))
            {
                Note($"Delivered ({stars})… but {gig.ClientName} has gone quiet about the invoice.");
                f.Morale = Rules.Clamp(f.Morale - 4, 0, 100);
                Post("client_ghosted", MessageKind.Decision, $"{gig.ClientName} stopped answering",
                    $"You delivered \"{gig.Title}\" ({stars}), but the {Rules.Money(gig.Pay)} invoice is ignored. Morale −4.",
                    gig.Id, 7, "Chase them (costs a Sunday of emails)", "Let it go");
                return;
            }

            Schedule(day + gig.PaymentDelayDays, gig.Pay, TxCategory.Freelance, $"Gig: {gig.Title}");
            profile.TotalEarned += gig.Pay;
            State.CurrentWeek.FreelanceEarned += gig.Pay;
            string when = gig.PaymentDelayDays >= 20 ? $"in {gig.PaymentDelayDays} days (corporate invoicing)" : $"in {gig.PaymentDelayDays} days";
            Note($"Delivered! {gig.ClientName} rated you {stars}. {Rules.Money(gig.Pay)} arrives {when}.");
            Post("gig_done", MessageKind.Info, $"Gig delivered: {gig.Title}",
                $"{gig.ClientName} rated you {stars}. {Rules.Money(gig.Pay)} arrives {when}. Rating now {Rules.Stars(profile.Rating)}.");
        }

        void CheckGigDeadlines(int day)
        {
            foreach (var gig in State.Freelance.Active.ToList())
            {
                if (day > gig.DeadlineDay + 7)
                {
                    gig.Status = GigStatus.Failed;
                    gig.ClientScore = 1;
                    State.Freelance.Profile.AddScore(1);
                    State.Freelance.Profile.FailedGigs++;
                    State.Founder.Morale = Rules.Clamp(State.Founder.Morale - 5, 0, 100);
                    Post("gig_failed", MessageKind.Critical, $"{gig.ClientName} cancelled the gig",
                        $"\"{gig.Title}\" is a week past its deadline. No pay, a 1/5 review, and morale −5. Spend slots on the gigs you accept.");
                }
                else if (day > gig.DeadlineDay && !gig.LateNoticeSent)
                {
                    gig.LateNoticeSent = true;
                    Post("gig_late", MessageKind.Info, $"{gig.ClientName} is waiting",
                        $"\"{gig.Title}\" was due yesterday ({Rules.Pct(gig.Progress)} done). Late delivery lowers your rating; a week late cancels it.");
                }
            }
        }

        void ExpireGigOffers(int day)
        {
            foreach (var gig in State.Freelance.Offers.Where(g => day > g.ExpiresDay).ToList())
                gig.Status = GigStatus.Expired;
            // Keep the list from growing forever: drop old finished offers.
            State.Freelance.Gigs.RemoveAll(g => (g.Status == GigStatus.Expired || g.Status == GigStatus.Declined) && day - g.OfferedDay > 60);
        }

        void GenerateGigOffers(int day)
        {
            var rng = State.RngGigs;
            var profile = State.Freelance.Profile;
            int count = 2
                        + (profile.CompletedGigs >= 2 ? 1 : 0)
                        + (profile.CompletedGigs >= 6 ? 1 : 0)
                        + (profile.Rating >= 4 ? 1 : 0)
                        + (rng.Chance(0.4) ? 1 : 0);
            if (profile.RateSetting == RateSetting.Budget) count++;
            if (profile.RateSetting == RateSetting.Premium) count--;
            for (int i = 0; i < count; i++) State.Freelance.Gigs.Add(CreateGig(day, rng));
        }

        Gig CreateGig(int day, SimRandom rng)
        {
            var f = State.Founder;
            var profile = State.Freelance.Profile;
            var disciplines = SkillInfo.Craft.Where(s => f.Get(s) >= 1.4).ToList();
            Skill discipline = rng.PickWeighted(disciplines, s => f.Get(s) * f.Get(s));
            double skill = f.Get(discipline);
            double required = Math.Round(Rules.Clamp(skill + rng.Gaussian() * 0.7, 1, skill + 1.5), 1);

            bool premium = profile.RateSetting == RateSetting.Premium && profile.Rating >= 4;
            ClientType client = rng.PickWeighted(ClientTypes, c => ClientWeight(c, profile.Rating, premium));
            double maxHours = 20 + 8 * Math.Min(5, profile.CompletedGigs);
            double hours = Math.Round(rng.Range(8, maxHours) / 2) * 2;

            double rate = Rules.FreelanceRate(required, profile.Rating, profile.RateSetting).Value;
            double pay = Math.Max(20, Math.Round(hours * rate * ClientPayFactor(client) * rng.Range(0.9, 1.1) / 5) * 5);
            int duration = (int)Math.Ceiling(hours / 10.0) * 7 + rng.Range(2, 8);
            bool rush = rng.Chance(0.15);
            if (rush)
            {
                duration = Math.Max(4, duration / 2);
                pay = Math.Round(pay * 1.2 / 5) * 5;
            }

            var gig = new Gig
            {
                Id = State.NextId++,
                Client = client,
                ClientName = client == ClientType.Friend ? "A friend" : $"{rng.Pick(GameContent.ClientFirstNames)} {rng.Pick(GameContent.ClientSecondNames)}",
                Title = rng.Pick(GigTitles[discipline]),
                Discipline = discipline,
                RequiredSkill = required,
                Hours = hours,
                Pay = pay,
                OfferedDay = day,
                ExpiresDay = day + 6,
                DurationDays = duration,
                NonPayRisk = ClientNonPayRisk(client) * (profile.CompletedGigs < 3 ? 1.5 : 1.0),
                ScopeCreepChance = ClientScopeCreep(client),
                PaymentDelayDays = ClientPaymentDelay(client),
            };

            var flags = new List<string>();
            if (client == ClientType.Startup) flags.Add("New client");
            if (gig.ScopeCreepChance >= 0.3) flags.Add("Vague requirements");
            if (required > skill + 0.5) flags.Add("Above your level");
            if (rush) flags.Add("Rush");
            if (client == ClientType.Corporate) flags.Add("Pays in 30 days");
            gig.RiskFlags = string.Join(" · ", flags);
            return gig;
        }

        void OfferFriendGig()
        {
            State.Freelance.Gigs.Add(new Gig
            {
                Id = State.NextId++,
                Client = ClientType.Friend,
                ClientName = "Sam (a friend)",
                Title = "Fix a few bugs in a small mobile game",
                Discipline = Skill.Programming,
                RequiredSkill = 1.5,
                Hours = 12,
                Pay = 180,
                OfferedDay = 0,
                ExpiresDay = 13,
                DurationDays = 14,
                NonPayRisk = 0,
                ScopeCreepChance = 0.1,
                PaymentDelayDays = 2,
                RiskFlags = "Friend rate",
            });
        }

        static double ClientWeight(ClientType client, double rating, bool premium)
        {
            switch (client)
            {
                case ClientType.SmallStudio: return 3;
                case ClientType.Agency: return rating >= 3 ? 3 : 1.5;
                case ClientType.Startup: return premium ? 1 : 3;
                case ClientType.Corporate: return rating >= 3.5 ? 2 : 0.5;
                default: return 1;
            }
        }

        static double ClientPayFactor(ClientType client)
        {
            switch (client)
            {
                case ClientType.Friend: return 0.8;
                case ClientType.Agency: return 0.95;
                case ClientType.Startup: return 1.2;
                case ClientType.Corporate: return 1.15;
                default: return 1.0;
            }
        }

        static double ClientNonPayRisk(ClientType client)
        {
            switch (client)
            {
                case ClientType.Startup: return 0.08;
                case ClientType.SmallStudio: return 0.02;
                case ClientType.Agency: return 0.01;
                case ClientType.Corporate: return 0.01;
                default: return 0;
            }
        }

        static double ClientScopeCreep(ClientType client)
        {
            switch (client)
            {
                case ClientType.Startup: return 0.35;
                case ClientType.Corporate: return 0.2;
                case ClientType.Agency: return 0.15;
                case ClientType.SmallStudio: return 0.15;
                default: return 0.1;
            }
        }

        static int ClientPaymentDelay(ClientType client)
        {
            switch (client)
            {
                case ClientType.Friend: return 2;
                case ClientType.SmallStudio: return 5;
                case ClientType.Agency: return 7;
                case ClientType.Startup: return 10;
                default: return 30;
            }
        }
    }
}
