using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// The M1 event library. Events are conditioned on the player's situation, always offer a choice
    /// when they matter, and are rarely purely good or bad (Design/14 §7).
    /// </summary>
    public sealed partial class GameSimulation
    {
        void PostWelcome()
        {
            Post("welcome", MessageKind.Info, "Day one",
                "You quit your job to make games. You have $5,000 in savings, a tired laptop and no audience.\n" +
                "January's rent is paid. On the 1st of every month $1,460 leaves your account for rent, food and bills — " +
                "that's about 3.4 months of runway if nothing comes in.\n" +
                "Every day has a morning, an afternoon and an evening, and nothing happens until you decide how to spend them: " +
                "gigs earn money, Build makes your game, Marketing finds players, rest keeps burnout down. " +
                "Your friend Sam has a small gig for you in GigGrid.");
        }

        void RollWeeklyEvents(int day)
        {
            var rng = State.RngEvents;
            var f = State.Founder;
            var flags = State.Flags;

            if (day >= 35 && day <= 120 && !flags.Contains("overheat_seen") && !State.Owned.Contains("cooling_pad") && rng.Chance(0.25))
            {
                flags.Add("overheat_seen");
                var m = Post("laptop_overheat", MessageKind.Decision, "Your laptop is overheating",
                    "The fan screams during every build and the case is hot enough to fry an egg. A cooling pad costs $120.",
                    0, 7, "Buy a cooling pad ($120)", "Ignore it");
                m.DefaultOption = 1;
            }

            if (!State.Owned.Contains("gaming_pc") && !OnCooldown("laptop_dies", 180))
            {
                double chance = flags.Contains("laptop_risky") ? 0.03 : !State.Owned.Contains("cooling_pad") && day > 365 ? 0.004 : 0;
                if (chance > 0 && rng.Chance(chance)) TriggerLaptopDeath(day);
            }

            if (day >= 32 && !OnCooldown("jam_announcement", 112) && !f.IsResting(day) && (day < 60 || rng.Chance(0.5)))
            {
                string theme = rng.Pick(new[] { "Only One", "It's Alive", "Out of Control", "Small World", "Deeper and Deeper", "Opposites" });
                var m = Post("jam_announcement", MessageKind.Decision, $"Ludum Jam this weekend — theme: \"{theme}\"",
                    "72 hours, one game, thousands of developers. It costs your weekend (and some burnout), but jam games teach fast, " +
                    "bring followers and sometimes become real projects.", 0, 4, "Join (Saturday and Sunday become jam days)", "Skip it");
                m.DefaultOption = 1;
            }

            if (day > 21 && !OnCooldown("course_offer", 60) && rng.Chance(0.06))
            {
                var skill = rng.Pick(new[] { Skill.Programming, Skill.Art, Skill.Design, Skill.Marketing, Skill.Audio, Skill.Writing });
                var m = Post("course_offer", MessageKind.Opportunity, $"Course sale: {skill} masterclass for $60",
                    $"40 hours of structured lessons. Studying {skill} earns XP ×{Tuning.CourseXpMultiplier} instead of ×{Tuning.LearnXpMultiplier} until you finish it.",
                    (int)skill, 7, "Buy it ($60)", "Skip");
                m.DefaultOption = 1;
            }

            if (f.Job == DayJob.None && day > 45 && State.Finance.Cash < 1200 && !OnCooldown("job_offer", 90) && rng.Chance(0.5))
            {
                var m = Post("job_offer", MessageKind.Decision, "A café needs a part-time helper",
                    $"20 hours a week (weekday afternoons), ${Tuning.PartTimeMonthlyPay:#,0}/month after tax. Safe money — and five fewer afternoons for your game every week.",
                    0, 7, "Take the job", "No thanks");
                m.DefaultOption = 1;
            }

            if (f.Job == DayJob.None && day > 180 && !OnCooldown("old_colleague", 365) && rng.Chance(0.02))
            {
                var m = Post("old_colleague", MessageKind.Decision, "Your old team wants you back",
                    $"A full-time job, ${Tuning.FullTimeMonthlyPay:#,0}/month after tax. Weekday mornings and afternoons would belong to them. It would be so easy.",
                    0, 10, "Take it", "No. I'm doing this.");
                m.DefaultOption = 1;
            }

            if (State.Finance.Cash < 500 && flags.Add("parents_loan_offered"))
            {
                var m = Post("parents_loan", MessageKind.Decision, "Your parents want to help",
                    "They heard things are tight. They offer $2,000, no interest. They will ask about it at every holiday.",
                    0, 10, "Accept the $2,000", "Thank them and decline");
                m.DefaultOption = 1;
            }

            if (f.Morale < 45 && !OnCooldown("friend_invite", 28) && rng.Chance(0.15))
            {
                var m = Post("friend_invite", MessageKind.Decision, "Friends invite you out on Friday",
                    "You haven't seen them in weeks. It costs Friday evening.", 0, 4, "Go (morale +8)", "Stay in and work (morale −2)");
                m.DefaultOption = 1;
            }

            if (day >= 60 && !State.Owned.Contains("backups") && flags.Add("backup_offer_seen"))
            {
                var m = Post("backup_offer", MessageKind.Opportunity, "Back up your work?",
                    "A cloud backup service costs $10 a month. Most people only buy it after the first disaster.",
                    0, 10, "Subscribe ($10/month)", "Not now");
                m.DefaultOption = 1;
            }

            var released = State.ReleasedProjects.Where(p => day - p.ReleaseDay > 14).ToList();
            if (released.Count > 0 && !OnCooldown("streamer_found", 90) && rng.Chance(0.03))
            {
                var game = rng.Pick(released);
                int month = (day - game.ReleaseDay) / 30;
                if (month < game.Result.MonthlyUnits.Count) game.Result.MonthlyUnits[month] *= 1.5;
                double followers = rng.LogNormal(80, 0.6);
                State.Audience.Followers += followers;
                Post("streamer_found", MessageKind.Info, $"A streamer played {game.Title}",
                    $"A small streamer (about 30k followers) found {game.Title} on their own and played it for an hour. Sales this month +50%, +{followers:0} followers.");
            }
        }

        void TriggerLaptopDeath(int day)
        {
            var project = State.ActiveProject;
            string loss;
            if (State.Owned.Contains("backups"))
                loss = "Your cloud backup had everything. Not a single file lost.";
            else if (project != null && project.TotalDone > 0)
            {
                double lost = Math.Min(project.TotalDone * 0.2, 1.5);
                double share = lost / project.TotalDone;
                foreach (var w in project.Workstreams)
                {
                    double cut = w.Done * share;
                    w.QualitySum -= w.Quality * cut;
                    w.Done -= cut;
                }
                if (project.Status == ProjectStatus.ReadyToRelease && !project.AllWorkDone) project.Status = ProjectStatus.InDevelopment;
                loss = $"No backups: {lost:0.0} person-weeks of work on {project.Title} are gone. (A $10/month backup would have saved them.)";
            }
            else
                loss = "Luckily there was nothing important on it.";

            var m = Post("laptop_dies", MessageKind.Critical, "Your laptop died",
                $"It won't turn on. {loss}", 0, 7,
                "Buy a refurbished laptop ($700)", "Buy a gaming PC ($1,800)", "Borrow a friend's old laptop (−15% output for 8 weeks)");
            m.DefaultOption = 2;
            State.Flags.Remove("laptop_risky");
        }

        /// <summary>Called at the end of a week: the following <see cref="Tuning.CollapseDays"/> days are forced rest.</summary>
        void TriggerCollapse(int day)
        {
            State.Founder.ForcedRestUntilDay = day + Tuning.CollapseDays;
            State.Stats.Collapses++;
            Post("collapse", MessageKind.Critical, "You collapsed",
                $"Months of long weeks caught up with you (burnout {State.Founder.Burnout:0}). The doctor orders two weeks of rest. " +
                "Gigs and deadlines don't pause while you recover.");
        }

        void TryVacationAdvice()
        {
            if (OnCooldown("vacation_advice", 28)) return;
            var m = Post("vacation_advice", MessageKind.Decision, "You're running on fumes",
                $"Burnout is at {State.Founder.Burnout:0}. Past 75 you risk collapsing for two weeks. A week off recovers about 22 points.",
                0, 3, "Take next week off", "Push on");
            m.DefaultOption = 1;
        }

        void TryCrisisOfFaith()
        {
            if (OnCooldown("crisis_of_faith", 56)) return;
            var m = Post("crisis_of_faith", MessageKind.Decision, "Is this even working?",
                "You lie awake doing the math again. Maybe everyone was right about getting a real job.", 0, 5,
                "Call a friend (morale +10)", "Take a long weekend (3 days off)", "Push through (burnout +5)");
            m.DefaultOption = 0;
        }

        void TryMoneyWarning()
        {
            double runway = State.Finance.RunwayDays();
            if (OnCooldown("money_warning", 45)) return;
            Post("money_warning", runway < 30 ? MessageKind.Critical : MessageKind.Info, "Money is running out",
                $"About {runway:0} days of runway at {Rules.Money(State.Finance.MonthlyRecurringTotal)}/month. " +
                "Money stress adds burnout every week. Options: more gig blocks, a day job, cheaper housing, or releasing what you have.");
        }

        void CheckCashWarnings()
        {
            if (State.Finance.Cash >= 0)
            {
                State.Flags.Remove("in_red");
                return;
            }
            if (!State.Flags.Add("in_red")) return;
            Post("in_red", MessageKind.Critical, "You're in the red",
                $"You're living on the credit card now (24% interest). If the debt passes {Rules.Money(State.Finance.CreditLimit)}, the studio closes.");
        }

        void PostScopeCreep(Gig gig)
        {
            var rng = State.RngGigs;
            double extraHours = Math.Round(gig.Hours * rng.Range(0.2, 0.5) / 2) * 2;
            double extraPay = Math.Round(extraHours * gig.EffectiveHourlyRate * 0.9 / 5) * 5;
            var m = Post("gig_scope_creep", MessageKind.Decision, $"{gig.ClientName} wants \"a few small extras\"",
                $"Halfway through \"{gig.Title}\" the client asks for about {extraHours:0} more hours of work.", gig.Id, 5,
                $"Accept for +{Rules.Money(extraPay)}", "Do it for free to keep them happy", "Push back politely");
            m.DefaultOption = 2;
            m.OptionIds.Add(extraHours.ToString(System.Globalization.CultureInfo.InvariantCulture));
            m.OptionIds.Add(extraPay.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        void TriggerRentIncrease(int day)
        {
            if (GameCalendar.Year(day) < 2) return;
            double pct = State.RngEvents.Range(Tuning.RentIncreaseMin, Tuning.RentIncreaseMax);
            var rent = State.Finance.GetRecurring("rent");
            if (rent == null) return;
            double newRent = Math.Round(rent.MonthlyAmount * (1 + pct));
            if (State.Founder.Housing == Housing.Apartment)
            {
                var m = Post("rent_increase", MessageKind.Decision, $"Your landlord raises the rent by {Rules.Pct(pct)}",
                    $"From {Rules.Money(rent.MonthlyAmount)} to {Rules.Money(newRent)} a month, starting now. Moving costs $500 and some pride.",
                    (int)newRent, 10, $"Pay {Rules.Money(newRent)}", $"Move to a shared flat ({Rules.Money(Tuning.RentSharedFlat)}, noisy weeks)",
                    $"Move back with your parents ({Rules.Money(Tuning.RentParents)}, morale −10)");
                m.DefaultOption = 0;
            }
            else
            {
                State.Finance.SetRecurring("rent", rent.Name, newRent, TxCategory.Rent);
                Post("rent_increase", MessageKind.Info, $"Rent goes up {Rules.Pct(pct)}", $"Your rent is now {Rules.Money(newRent)} a month.");
            }
        }

        // ------------------------------------------------------------------ resolution

        /// <summary>Applies the effects of the chosen option and returns a one-line outcome.</summary>
        string ResolveEvent(Message m, int option)
        {
            var f = State.Founder;
            var fin = State.Finance;
            int day = State.Day;
            switch (m.EventId)
            {
                case "laptop_overheat":
                    if (option == 0 && Buy("cooling_pad", out _)) return "The fan finally sounds normal again.";
                    State.Flags.Add("laptop_risky");
                    return "It still works. For now.";

                case "laptop_dies":
                    if (option == 0)
                    {
                        fin.Apply(day, -700, TxCategory.Hardware, "Refurbished laptop");
                        return "A refurbished laptop. Same specs, fewer scratches.";
                    }
                    if (option == 1 && Buy("gaming_pc", out _)) return "A proper machine. 3D builds are fast now.";
                    State.BorrowedLaptopUntilDay = day + 56;
                    return "Sam lends you an ancient laptop. Everything takes a little longer.";

                case "jam_announcement":
                    if (option != 0) return "Maybe next time.";
                    int saturday = day + (5 - GameCalendar.DayOfWeek(day) + 7) % 7;
                    State.Overrides.Add(new PlanOverride { FromDay = saturday, ToDay = saturday + 1, Kind = ActivityKind.Jam });
                    return "You're in. Saturday and Sunday are jam days.";

                case "course_offer":
                    if (option != 0) return "Skipped.";
                    fin.Apply(day, -60, TxCategory.Education, $"{(Skill)m.ContextId} masterclass");
                    f.CourseSkill = (Skill)m.ContextId;
                    f.CourseHoursLeft = 40;
                    return $"Bought. Spend slots on Learn → {(Skill)m.ContextId} to work through it.";

                case "job_offer":
                    if (option != 0) return "You stay focused on your own work.";
                    f.Job = DayJob.PartTime;
                    f.JobStartDay = day;
                    return "Weekday afternoons now belong to the café. Salary arrives on the 1st.";

                case "old_colleague":
                    if (option != 0)
                    {
                        f.Morale = Rules.Clamp(f.Morale + 3, 0, 100);
                        return "Saying no felt good. Morale +3.";
                    }
                    if (f.Job != DayJob.None) PayDayJobUntil(day);
                    f.Job = DayJob.FullTime;
                    f.JobStartDay = day;
                    f.Morale = Rules.Clamp(f.Morale - 5, 0, 100);
                    return "The safe choice. Your game gets evenings and weekends. Morale −5.";

                case "parents_loan":
                    if (option != 0)
                    {
                        f.Morale = Rules.Clamp(f.Morale + 2, 0, 100);
                        return "You'll manage. Morale +2.";
                    }
                    fin.Apply(day, 2000, TxCategory.Family, "Loan from your parents");
                    f.Morale = Rules.Clamp(f.Morale - 3, 0, 100);
                    return "$2,000 arrives. Morale −3 (pride).";

                case "friend_invite":
                    if (option != 0)
                    {
                        f.Morale = Rules.Clamp(f.Morale - 2, 0, 100);
                        return "Another night in. Morale −2.";
                    }
                    int friday = day + (4 - GameCalendar.DayOfWeek(day) + 7) % 7;
                    State.Overrides.Add(new PlanOverride { FromDay = friday, ToDay = friday, Kind = ActivityKind.Rest, BlockMask = 0b100 });
                    f.Morale = Rules.Clamp(f.Morale + 8, 0, 100);
                    return "A night out. Morale +8.";

                case "backup_offer":
                    return option == 0 && Buy("backups", out _) ? "Your work is backed up every night." : "Not now.";

                case "vacation_advice":
                    if (option != 0) return "You push on.";
                    TakeVacation(7 - GameCalendar.DayOfWeek(day));
                    return "Out of office until Monday.";

                case "crisis_of_faith":
                    if (option == 0)
                    {
                        f.Morale = Rules.Clamp(f.Morale + 10, 0, 100);
                        return "It helped more than you expected. Morale +10.";
                    }
                    if (option == 1)
                    {
                        TakeVacation(3);
                        f.Morale = Rules.Clamp(f.Morale + 6, 0, 100);
                        return "Three days off. Morale +6.";
                    }
                    f.Burnout = Rules.Clamp(f.Burnout + 5, 0, 100);
                    f.Morale = Rules.Clamp(f.Morale + 2, 0, 100);
                    return "Stubbornness is a kind of fuel. Burnout +5.";

                case "gig_scope_creep":
                {
                    var gig = State.Freelance.Gigs.FirstOrDefault(g => g.Id == m.ContextId);
                    if (gig == null || gig.Status != GigStatus.Active) return "The gig is already over.";
                    double extraHours = double.Parse(m.OptionIds[0], System.Globalization.CultureInfo.InvariantCulture);
                    double extraPay = double.Parse(m.OptionIds[1], System.Globalization.CultureInfo.InvariantCulture);
                    if (option == 0)
                    {
                        gig.Hours += extraHours;
                        gig.Pay += extraPay;
                        gig.DeadlineDay += 7;
                        return $"More work, more money: +{extraHours:0}h, +{Rules.Money(extraPay)}, one more week.";
                    }
                    if (option == 1)
                    {
                        gig.Hours += extraHours;
                        gig.ScoreModifier += 1;
                        return "They're thrilled. You're a little poorer.";
                    }
                    if (State.RngGigs.Chance(0.6)) return "They accepted. Scope stays as agreed.";
                    gig.ScoreModifier -= 1;
                    return "They accepted — grudgingly. Expect a lower rating.";
                }

                case "client_ghosted":
                {
                    if (option != 0) return "Lesson learned about new clients.";
                    var gig = State.Freelance.Gigs.FirstOrDefault(g => g.Id == m.ContextId);
                    f.Burnout = Rules.Clamp(f.Burnout + 2, 0, 100);
                    if (gig != null && State.RngGigs.Chance(0.55))
                    {
                        Schedule(day + 21, gig.Pay, TxCategory.Freelance, $"Late payment: {gig.Title}");
                        return "After a week of polite emails they promised to pay within three weeks.";
                    }
                    return "Silence. The money is gone.";
                }

                case "rent_increase":
                    if (option == 0)
                    {
                        fin.SetRecurring("rent", "Rent (apartment)", m.ContextId, TxCategory.Rent);
                        return "You pay the new rent.";
                    }
                    fin.Apply(day, -500, TxCategory.Other, "Moving costs");
                    if (option == 1)
                    {
                        f.Housing = Housing.SharedFlat;
                        fin.SetRecurring("rent", "Rent (shared flat)", Tuning.RentSharedFlat, TxCategory.Rent);
                        return "New flatmates. Cheaper, louder.";
                    }
                    f.Housing = Housing.Parents;
                    fin.SetRecurring("rent", "Contribution to your parents", Tuning.RentParents, TxCategory.Rent);
                    f.Morale = Rules.Clamp(f.Morale - 10, 0, 100);
                    return "Your old bedroom, your old posters. Morale −10.";

                case "post_mortem":
                {
                    if (option < 0 || option >= m.OptionIds.Count) return "";
                    var lesson = LessonCatalog.Get(m.OptionIds[option]);
                    State.Lessons.Add(lesson.Id);
                    return $"Lesson learned: {lesson.Title}. {lesson.Effect}";
                }

                default:
                    return "";
            }
        }
    }
}
