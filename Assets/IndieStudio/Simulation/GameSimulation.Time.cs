using System;
using System.Linq;

namespace IndieStudio.Simulation
{
    public sealed partial class GameSimulation
    {
        const int Morning = 0, Afternoon = 1, Evening = 2;

        /// <summary>The report of the slot being executed; activities add their results to it.</summary>
        SlotReport _report;

        /// <summary>What a slot must be regardless of the player's choice: rest days, overrides (jams, nights out), the day job.</summary>
        public PlanBlock? ForcedBlock(int day, int block)
        {
            var f = State.Founder;
            if (f.IsResting(day)) return PlanBlock.Rest;
            foreach (var o in State.Overrides)
                if (day >= o.FromDay && day <= o.ToDay && (o.BlockMask & (1 << block)) != 0)
                    return new PlanBlock(o.Kind);
            if (IsJobBlock(f.Job, GameCalendar.DayOfWeek(day), block)) return new PlanBlock(ActivityKind.DayJob);
            return null;
        }

        /// <summary>The activity the week plan would run in a slot, after rest, overrides and the day job.</summary>
        public PlanBlock ResolveBlock(int day, int block) => ForcedBlock(day, block) ?? State.Plan[GameCalendar.DayOfWeek(day), block];

        public static bool IsJobBlock(DayJob job, int dayOfWeek, int block)
        {
            if (job == DayJob.None || dayOfWeek >= 5) return false;
            return job == DayJob.PartTime ? block == Afternoon : block != Evening;
        }

        SlotReport ExecuteBlock(int day, int block, PlanBlock plan, double performance)
        {
            var report = new SlotReport { Day = day, Slot = block, Activity = plan, Performance = performance };
            _report = report;
            var log = State.CurrentWeek;
            bool weekend = GameCalendar.IsWeekend(day);
            if (!plan.IsWork)
            {
                log.RestBlocks++;
                if (weekend) log.WeekendRestBlocks++;
                var f = State.Founder;
                report.Title = day <= f.ForcedRestUntilDay ? "Doctor's orders: rest" : day <= f.VacationUntilDay ? "Day off" : "Rested";
                Note("Rest doesn't build burnout. Weeks under 45 hours bring it down.");
                _report = null;
                return report;
            }

            log.WorkBlocks++;
            State.Stats.WorkBlocks++;
            if (weekend) log.WeekendWorkBlocks++;
            switch (plan.Kind)
            {
                case ActivityKind.Develop: DoDevelop(day, plan, performance); break;
                case ActivityKind.Freelance: DoFreelance(day, plan, performance); break;
                case ActivityKind.Marketing: DoMarketing(day, plan); break;
                case ActivityKind.Learn: DoLearn(plan); break;
                case ActivityKind.Research: DoResearch(day); break;
                case ActivityKind.Jam: DoJam(day, block, performance); break;
                case ActivityKind.Hotfix: DoHotfix(day, plan, performance); break;
                case ActivityKind.Community: DoCommunity(day, plan, performance); break;
                case ActivityKind.Demo: DoDemo(day, plan, performance); break;
                case ActivityKind.DayJob:
                    report.Title = State.Founder.Job == DayJob.FullTime ? "A day at the office" : "A shift at the café";
                    Note("Your salary arrives on the 1st of the month.");
                    break;
            }
            if (weekend) Note($"Weekend work: +{Tuning.BurnoutPerWeekendBlock} extra burnout when the week closes.");
            _report = null;
            return report;
        }

        void Note(string line) => _report?.Lines.Add(line);

        void SetSubject(string title, string subject, Skill skill, double progressBefore)
        {
            if (_report == null) return;
            _report.Title = title;
            _report.Subject = subject;
            _report.HasSkill = true;
            _report.Skill = skill;
            _report.ProgressBefore = progressBefore;
        }

        void GainXp(Skill skill, double hours)
        {
            var f = State.Founder;
            int before = f.Level(skill);
            double old = f.Skills[skill];
            f.Skills[skill] = SkillInfo.AddPractice(old, hours);
            int after = f.Level(skill);
            if (f.Skills[skill] - old >= 0.005) Note($"{skill} {old:0.00} → {f.Skills[skill]:0.00}");
            if (after > before)
                Post("level_up", MessageKind.Info, $"{skill} reached level {after}",
                    $"Practice pays off. Your {skill.ToString().ToLowerInvariant()} work is now faster and better. " +
                    $"Level {after + 1} needs {SkillInfo.HoursForLevel(after):0} more hours of practice.");
        }

        void DoLearn(PlanBlock plan)
        {
            var f = State.Founder;
            Skill skill = plan.HasFocus ? plan.Focus : SkillInfo.Craft.OrderBy(s => f.Get(s)).First();
            double hours = Tuning.BlockHours * Rules.Productivity(State).Value;
            double multiplier = Tuning.LearnXpMultiplier;
            bool course = f.CourseHoursLeft > 0 && f.CourseSkill == skill;
            if (course)
            {
                multiplier = Tuning.CourseXpMultiplier;
                f.CourseHoursLeft = Math.Max(0, f.CourseHoursLeft - Tuning.BlockHours);
            }
            if (_report != null)
            {
                _report.Title = course ? $"Worked through the {skill} course" : $"Studied {skill}";
                _report.HasSkill = true;
                _report.Skill = skill;
            }
            GainXp(skill, hours * multiplier);
            if (course) Note(f.CourseHoursLeft > 0 ? $"{f.CourseHoursLeft:0} course hours left." : "Course finished!");
        }

        // ------------------------------------------------------------------ weeks

        void OnWeekStart(int day)
        {
            State.Overrides.RemoveAll(o => o.ToDay < day);
            PayScratchSales(day);
            ExpireGigOffers(day);
            GenerateGigOffers(day);
            RollWeeklyEvents(day);
        }

        /// <summary>Runs at the end of Sunday: the week's hours turn into burnout and morale changes.</summary>
        void FinalizeWeek(int day)
        {
            var f = State.Founder;
            var log = State.CurrentWeek;
            int nextDay = day + 1;

            double hours = log.HoursWorked;
            double delta = hours > Tuning.SustainableWeeklyHours
                ? Tuning.BurnoutPerHourOver * (hours - Tuning.SustainableWeeklyHours)
                : -Tuning.BurnoutRecoveryPerHourUnder * (Tuning.SustainableWeeklyHours - hours);
            delta += Tuning.BurnoutPerWeekendBlock * log.WeekendWorkBlocks;
            delta += Tuning.BurnoutPerJamBlock * log.JamBlocks;
            bool anxious = State.Finance.RunwayDays() < Tuning.MoneyAnxietyRunwayDays;
            if (anxious)
            {
                delta += Tuning.MoneyAnxietyBurnout;
                State.Stats.AnxiousWeeks++;
            }
            if (delta > 0 && State.Owned.Contains("ergonomic_chair")) delta *= 0.9;
            double burnoutBefore = f.Burnout;
            f.Burnout = Rules.Clamp(f.Burnout + delta, 0, 100);
            log.BurnoutChange = f.Burnout - burnoutBefore;

            double morale = f.Morale;
            double restingPoint = f.Housing == Housing.Parents ? 50 : 60;
            morale += (restingPoint - morale) * 0.08;
            if (log.ProjectPw >= 0.25) morale += 1.5;
            if (log.WorkBlocks > 0 && log.WeekendRestBlocks == 0) morale -= 1.5;
            if (anxious) morale -= 2;
            if (f.Burnout > 60) morale -= 2;
            log.MoraleChange = Rules.Clamp(morale, 0, 100) - f.Morale;
            f.Morale = Rules.Clamp(morale, 0, 100);

            if (f.Housing == Housing.SharedFlat && State.RngWork.Chance(0.05))
            {
                State.NoisyUntilDay = day + 7;
                Post("noisy_week", MessageKind.Info, "Party week next door", "Your flatmates are celebrating something. Every day. Output −10% next week.");
            }

            if (!f.IsResting(nextDay))
            {
                if (f.Burnout > Tuning.CollapseBurnout && State.RngWork.Chance(Tuning.CollapseChance)) TriggerCollapse(day);
                else if (f.Burnout >= Tuning.VacationAdviceBurnout) TryVacationAdvice();
            }
            if (f.Morale < 25) TryCrisisOfFaith();
            if (anxious) TryMoneyWarning();

            State.LastWeek = log;
            State.CurrentWeek = new WeekLog();
        }

        // ------------------------------------------------------------------ months

        void OnMonthStart(int day)
        {
            int closedMonth = GameCalendar.MonthIndex(day) - 1;
            CreateMonthlyReport(day, closedMonth);
            ScheduleVaporPayouts(day);
            ChargeRecurring(day);
            PayDayJobUntil(day);
            if (GameCalendar.Month(day) == 1)
            {
                PayAnnualTaxes(day);
                TriggerRentIncrease(day);
            }
            if (State.Finance.Cash < 0)
                State.Finance.Apply(day, State.Finance.Cash * Tuning.MonthlyDebtInterest, TxCategory.Interest, "Credit card interest");
            CheckBankruptcy();
        }

        void ChargeRecurring(int day)
        {
            foreach (var cost in State.Finance.Recurring.ToList())
                State.Finance.Apply(day, -cost.MonthlyAmount, cost.Category, cost.Name);
        }

        void CreateMonthlyReport(int day, int monthIndex)
        {
            int from = GameCalendar.FirstDayOfMonthIndex(monthIndex);
            var report = new MonthlyReport { MonthIndex = monthIndex, CashAtClose = State.Finance.Cash };
            foreach (var tx in State.Finance.Transactions.Where(t => t.Day >= from && t.Day < day))
            {
                if (tx.Amount >= 0) report.Income += tx.Amount;
                else report.Expenses += tx.Amount;
                report.ByCategory.TryGetValue(tx.Category, out double sum);
                report.ByCategory[tx.Category] = sum + tx.Amount;
            }
            State.Finance.Reports.Add(report);

            double runwayMonths = State.Finance.RunwayDays() / 30.0;
            Post("monthly_report", MessageKind.Info, $"Monthly report — {GameCalendar.MonthLabel(monthIndex)}",
                $"Income {Rules.Money(report.Income)} · Expenses {Rules.Money(-report.Expenses)} · Net {Rules.Money(report.Net)}\n" +
                $"Cash {Rules.Money(State.Finance.Cash)} · Owed to you {Rules.Money(State.Finance.PendingIncome)} · " +
                $"Runway {runwayMonths:0.0} months at {Rules.Money(State.Finance.MonthlyRecurringTotal)}/month.");
        }

        void PayDayJobUntil(int day)
        {
            var f = State.Founder;
            if (f.Job == DayJob.None || f.JobStartDay < 0) return;
            int monthStart = GameCalendar.FirstDayOfMonthIndex(GameCalendar.MonthIndex(day - 1));
            int from = Math.Max(monthStart, f.JobStartDay);
            int days = day - from;
            if (days <= 0) return;
            double monthly = f.Job == DayJob.PartTime ? Tuning.PartTimeMonthlyPay : Tuning.FullTimeMonthlyPay;
            double pay = monthly * days / (double)GameCalendar.DaysInMonthIndex(GameCalendar.MonthIndex(day - 1));
            State.Finance.Apply(day, pay, TxCategory.DayJob, f.Job == DayJob.PartTime ? "Part-time job salary" : "Full-time job salary");
            f.JobStartDay = day;
        }

        void PayAnnualTaxes(int day)
        {
            int yearStart = GameCalendar.FirstDayOfMonthIndex(GameCalendar.MonthIndex(day) - 12);
            double income = 0, deductible = 0;
            foreach (var tx in State.Finance.Transactions.Where(t => t.Day >= yearStart && t.Day < day))
            {
                switch (tx.Category)
                {
                    case TxCategory.Freelance:
                    case TxCategory.GameSales:
                    case TxCategory.DayJob:
                        income += tx.Amount;
                        break;
                    case TxCategory.Hardware:
                    case TxCategory.Software:
                    case TxCategory.Assets:
                    case TxCategory.Marketing:
                    case TxCategory.StoreFees:
                    case TxCategory.Education:
                        deductible -= tx.Amount;
                        break;
                }
            }
            double taxable = income - deductible - Tuning.TaxAllowance;
            if (taxable <= 0)
            {
                Post("taxes", MessageKind.Info, "Taxes filed", $"Income last year: {Rules.Money(income)}. Below the tax-free allowance after expenses — nothing to pay.");
                return;
            }
            double tax = taxable * Tuning.TaxRate;
            State.Finance.Apply(day, -tax, TxCategory.Taxes, "Income tax (last year)");
            Post("taxes", MessageKind.Info, "Taxes paid",
                $"Income {Rules.Money(income)} − deductible costs {Rules.Money(deductible)} − allowance {Rules.Money(Tuning.TaxAllowance)} → {Rules.Money(tax)} tax at {Rules.Pct(Tuning.TaxRate)}.");
        }

        void CheckBankruptcy()
        {
            var fin = State.Finance;
            if (fin.Cash >= -fin.CreditLimit) return;
            GameOver($"On {GameCalendar.Format(State.Day)} your debt passed the credit limit and the bank closed your account. " +
                     $"You shipped {State.ShippedGames} game(s) and earned {Rules.Money(State.LifetimeGameNet)} from them. " +
                     "Every studio story has a first chapter — start a new one.");
        }

        void ProcessScheduledPayments(int day)
        {
            var due = State.Finance.Scheduled.Where(p => p.Day <= day).ToList();
            foreach (var payment in due)
            {
                State.Finance.Apply(day, payment.Amount, payment.Category, payment.Description);
                State.Finance.Scheduled.Remove(payment);
            }
        }

        void Schedule(int day, double amount, TxCategory category, string description)
        {
            if (amount <= 0) return;
            State.Finance.Scheduled.Add(new ScheduledPayment { Day = day, Amount = Math.Round(amount, 2), Category = category, Description = description });
        }
    }
}
