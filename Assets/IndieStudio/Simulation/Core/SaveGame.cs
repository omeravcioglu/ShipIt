using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IndieStudio.Simulation
{
    /// <summary>
    /// Save games: the whole <see cref="GameState"/> in a small, versioned binary format. Numbers are stored
    /// bit-exact and the random streams keep their position, so a loaded game continues exactly as if it had
    /// never been saved (SaveTests checks this). Fields are written one by one, without reflection, so the
    /// format is the same on every platform and nothing is saved by accident.
    /// When a field is added to the state: write it at the end of its section, bump <see cref="Version"/>,
    /// and read it only when the save's version has it.
    /// </summary>
    public static class SaveGame
    {
        /// <summary>"ISAV" (Indie Studio save), little-endian.</summary>
        const uint Magic = 0x56415349;
        /// <summary>1: the first save format. 2: studio name, flow, coffee and the cat. 3: counters for achievements. 4: launch week. 5: Next Fest and demos.</summary>
        public const int Version = 5;

        public static void Write(GameState state, Stream stream) => Write(state, stream, Version);

        /// <summary>Writes a specific (older) version: tests use it to check that old saves still load.</summary>
        internal static void Write(GameState state, Stream stream, int version)
        {
            using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                w.Write(Magic);
                w.Write(version);
                WriteState(w, state, version);
                w.Write(Magic); // end marker: a cut-off file is detected, not half-loaded
            }
        }

        /// <summary>Reads a save. Throws <see cref="InvalidDataException"/> for anything that isn't a complete, supported save.</summary>
        public static GameState Read(Stream stream)
        {
            using (var r = new BinaryReader(stream, Encoding.UTF8, true))
            {
                try
                {
                    if (r.ReadUInt32() != Magic) throw new InvalidDataException("This isn't an Indie Studio save.");
                    int version = r.ReadInt32();
                    if (version < 1 || version > Version)
                        throw new InvalidDataException($"Save version {version} isn't supported (this build reads versions 1–{Version}).");
                    var state = ReadState(r, version);
                    if (r.ReadUInt32() != Magic) throw new InvalidDataException("The save is incomplete.");
                    return state;
                }
                catch (EndOfStreamException)
                {
                    throw new InvalidDataException("The save is incomplete.");
                }
            }
        }

        // ------------------------------------------------------------------ the state

        static void WriteState(BinaryWriter w, GameState s, int version)
        {
            w.Write(s.Seed);
            w.Write(s.Day);
            w.Write(s.Slot);
            w.Write(s.DayStarted);
            WriteList(w, s.Today, WriteSlotReport);
            w.Write(s.Chapter);
            w.Write(s.ChapterStartDay);
            WriteStringIntMap(w, s.ChaptersCompleted);
            w.Write(s.TutorialStep);
            w.Write(s.TutorialStepDay);
            w.Write(s.TutorialDone);

            WriteFounder(w, s.Founder);
            WriteFinance(w, s.Finance);
            WriteFreelance(w, s.Freelance);
            w.Write(s.Audience.Followers);
            WriteList(w, s.Inbox.Messages, WriteMessage);
            for (int d = 0; d < WeekPlan.DaysPerWeek; d++)
                for (int b = 0; b < WeekPlan.BlocksPerDay; b++)
                    WritePlanBlock(w, s.Plan[d, b]);
            WriteList(w, s.Overrides, (ww, o) =>
            {
                ww.Write(o.FromDay);
                ww.Write(o.ToDay);
                ww.Write((int)o.Kind);
                ww.Write(o.BlockMask);
            });
            WriteList(w, s.Projects, (ww, p) => WriteProject(ww, p, version));
            WriteList(w, s.Ideas, WriteIdea);

            WriteStrings(w, s.Flags);
            WriteStrings(w, s.Owned);
            WriteStrings(w, s.Lessons);
            WriteStrings(w, s.Knowledge);
            WriteStringIntMap(w, s.EventLastDay);
            WriteStringIntMap(w, s.GoalsCompleted);

            var st = s.Stats;
            w.Write(st.WorkBlocks);
            w.Write(st.IdleFreelanceBlocks);
            w.Write(st.IdleDevelopBlocks);
            w.Write(st.VacationDays);
            w.Write(st.Collapses);
            w.Write(st.AnxiousWeeks);
            WriteWeek(w, s.CurrentWeek);
            w.Write(s.LastWeek != null);
            if (s.LastWeek != null) WriteWeek(w, s.LastWeek);

            w.Write(s.ResearchPoints);
            w.Write(s.JamProgress);
            w.Write(s.NoisyUntilDay);
            w.Write(s.BorrowedLaptopUntilDay);
            w.Write(s.NextId);
            w.Write(s.ShippedGames);
            w.Write(s.LifetimeGameNet);
            WriteString(w, s.GameOverReason);

            foreach (var rng in new[] { s.RngGigs, s.RngEvents, s.RngSales, s.RngWork, s.RngIdeas, s.RngMarketing })
            {
                w.Write(rng.State);
                w.Write(rng.Increment);
            }

            if (version >= 2)
            {
                WriteString(w, s.StudioName);
                WriteString(w, s.FlowKey);
                w.Write(s.FlowStreak);
                w.Write(s.CoffeeReady);
                w.Write(s.CoffeesToday);
                w.Write(s.CatPettedDay);
            }
            if (version >= 3)
            {
                w.Write(st.CatPets);
                w.Write(st.BrilliantSessions);
            }
        }

        static GameState ReadState(BinaryReader r, int version)
        {
            var s = new GameState
            {
                Seed = r.ReadUInt64(),
                Day = r.ReadInt32(),
                Slot = r.ReadInt32(),
                DayStarted = r.ReadBoolean(),
            };
            ReadList(r, s.Today, ReadSlotReport);
            s.Chapter = r.ReadInt32();
            s.ChapterStartDay = r.ReadInt32();
            ReadStringIntMap(r, s.ChaptersCompleted);
            s.TutorialStep = r.ReadInt32();
            s.TutorialStepDay = r.ReadInt32();
            s.TutorialDone = r.ReadBoolean();

            ReadFounder(r, s.Founder);
            ReadFinance(r, s.Finance);
            ReadFreelance(r, s.Freelance);
            s.Audience.Followers = r.ReadDouble();
            ReadList(r, s.Inbox.Messages, ReadMessage);
            for (int d = 0; d < WeekPlan.DaysPerWeek; d++)
                for (int b = 0; b < WeekPlan.BlocksPerDay; b++)
                    s.Plan[d, b] = ReadPlanBlock(r);
            ReadList(r, s.Overrides, rr => new PlanOverride
            {
                FromDay = rr.ReadInt32(),
                ToDay = rr.ReadInt32(),
                Kind = (ActivityKind)rr.ReadInt32(),
                BlockMask = rr.ReadInt32(),
            });
            ReadList(r, s.Projects, rr => ReadProject(rr, version));
            ReadList(r, s.Ideas, ReadIdea);

            ReadStrings(r, s.Flags);
            ReadStrings(r, s.Owned);
            ReadStrings(r, s.Lessons);
            ReadStrings(r, s.Knowledge);
            ReadStringIntMap(r, s.EventLastDay);
            ReadStringIntMap(r, s.GoalsCompleted);

            var st = s.Stats;
            st.WorkBlocks = r.ReadInt32();
            st.IdleFreelanceBlocks = r.ReadInt32();
            st.IdleDevelopBlocks = r.ReadInt32();
            st.VacationDays = r.ReadInt32();
            st.Collapses = r.ReadInt32();
            st.AnxiousWeeks = r.ReadInt32();
            s.CurrentWeek = ReadWeek(r);
            s.LastWeek = r.ReadBoolean() ? ReadWeek(r) : null;

            s.ResearchPoints = r.ReadDouble();
            s.JamProgress = r.ReadDouble();
            s.NoisyUntilDay = r.ReadInt32();
            s.BorrowedLaptopUntilDay = r.ReadInt32();
            s.NextId = r.ReadInt32();
            s.ShippedGames = r.ReadInt32();
            s.LifetimeGameNet = r.ReadDouble();
            s.GameOverReason = ReadString(r);

            s.RngGigs = ReadRandom(r);
            s.RngEvents = ReadRandom(r);
            s.RngSales = ReadRandom(r);
            s.RngWork = ReadRandom(r);
            s.RngIdeas = ReadRandom(r);
            s.RngMarketing = ReadRandom(r);

            if (version >= 2)
            {
                s.StudioName = ReadString(r) ?? s.StudioName;
                s.FlowKey = ReadString(r);
                s.FlowStreak = r.ReadInt32();
                s.CoffeeReady = r.ReadBoolean();
                s.CoffeesToday = r.ReadInt32();
                s.CatPettedDay = r.ReadInt32();
            }
            if (version >= 3)
            {
                st.CatPets = r.ReadInt32();
                st.BrilliantSessions = r.ReadInt32();
            }
            return s;
        }

        static SimRandom ReadRandom(BinaryReader r)
        {
            ulong state = r.ReadUInt64();
            ulong increment = r.ReadUInt64();
            return SimRandom.Restore(state, increment);
        }

        // ------------------------------------------------------------------ founder and week

        static void WriteFounder(BinaryWriter w, Founder f)
        {
            w.Write(f.Skills.Count);
            foreach (var pair in f.Skills)
            {
                w.Write((int)pair.Key);
                w.Write(pair.Value);
            }
            w.Write(f.Burnout);
            w.Write(f.Morale);
            w.Write(f.ForcedRestUntilDay);
            w.Write(f.VacationUntilDay);
            w.Write((int)f.Job);
            w.Write(f.JobStartDay);
            w.Write((int)f.Housing);
            w.Write((int)f.CourseSkill);
            w.Write(f.CourseHoursLeft);
        }

        static void ReadFounder(BinaryReader r, Founder f)
        {
            f.Skills.Clear();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var skill = (Skill)r.ReadInt32();
                f.Skills[skill] = r.ReadDouble();
            }
            f.Burnout = r.ReadDouble();
            f.Morale = r.ReadDouble();
            f.ForcedRestUntilDay = r.ReadInt32();
            f.VacationUntilDay = r.ReadInt32();
            f.Job = (DayJob)r.ReadInt32();
            f.JobStartDay = r.ReadInt32();
            f.Housing = (Housing)r.ReadInt32();
            f.CourseSkill = (Skill)r.ReadInt32();
            f.CourseHoursLeft = r.ReadDouble();
        }

        static void WriteWeek(BinaryWriter w, WeekLog log)
        {
            w.Write(log.WorkBlocks);
            w.Write(log.RestBlocks);
            w.Write(log.WeekendWorkBlocks);
            w.Write(log.JamBlocks);
            w.Write(log.WeekendRestBlocks);
            w.Write(log.ProjectPw);
            w.Write(log.FreelanceHours);
            w.Write(log.FreelanceEarned);
            w.Write(log.IdleFreelanceBlocks);
            w.Write(log.IdleDevelopBlocks);
            w.Write(log.FollowersGained);
            w.Write(log.WishlistsGained);
            w.Write(log.BurnoutChange);
            w.Write(log.MoraleChange);
        }

        static WeekLog ReadWeek(BinaryReader r) => new WeekLog
        {
            WorkBlocks = r.ReadInt32(),
            RestBlocks = r.ReadInt32(),
            WeekendWorkBlocks = r.ReadInt32(),
            JamBlocks = r.ReadInt32(),
            WeekendRestBlocks = r.ReadInt32(),
            ProjectPw = r.ReadDouble(),
            FreelanceHours = r.ReadDouble(),
            FreelanceEarned = r.ReadDouble(),
            IdleFreelanceBlocks = r.ReadInt32(),
            IdleDevelopBlocks = r.ReadInt32(),
            FollowersGained = r.ReadDouble(),
            WishlistsGained = r.ReadDouble(),
            BurnoutChange = r.ReadDouble(),
            MoraleChange = r.ReadDouble(),
        };

        static void WriteSlotReport(BinaryWriter w, SlotReport report)
        {
            w.Write(report.Day);
            w.Write(report.Slot);
            WritePlanBlock(w, report.Activity);
            w.Write(report.Performance);
            w.Write(report.Forced);
            WriteString(w, report.Title);
            WriteString(w, report.Subject);
            w.Write(report.HasSkill);
            w.Write((int)report.Skill);
            w.Write(report.ProgressBefore);
            w.Write(report.ProgressAfter);
            WriteStrings(w, report.Lines);
        }

        static SlotReport ReadSlotReport(BinaryReader r)
        {
            var report = new SlotReport
            {
                Day = r.ReadInt32(),
                Slot = r.ReadInt32(),
                Activity = ReadPlanBlock(r),
                Performance = r.ReadDouble(),
                Forced = r.ReadBoolean(),
                Title = ReadString(r),
                Subject = ReadString(r),
                HasSkill = r.ReadBoolean(),
                Skill = (Skill)r.ReadInt32(),
                ProgressBefore = r.ReadDouble(),
                ProgressAfter = r.ReadDouble(),
            };
            ReadStrings(r, report.Lines);
            return report;
        }

        static void WritePlanBlock(BinaryWriter w, PlanBlock block)
        {
            w.Write((int)block.Kind);
            w.Write(block.HasFocus);
            w.Write((int)block.Focus);
            w.Write(block.Option);
        }

        static PlanBlock ReadPlanBlock(BinaryReader r)
        {
            var kind = (ActivityKind)r.ReadInt32();
            bool hasFocus = r.ReadBoolean();
            var focus = (Skill)r.ReadInt32();
            int option = r.ReadInt32();
            return new PlanBlock(kind, hasFocus, focus, option);
        }

        // ------------------------------------------------------------------ money

        static void WriteFinance(BinaryWriter w, Finance f)
        {
            w.Write(f.Cash);
            w.Write(f.CreditLimit);
            WriteList(w, f.Transactions, (ww, t) =>
            {
                ww.Write(t.Day);
                ww.Write(t.Amount);
                ww.Write((int)t.Category);
                WriteString(ww, t.Description);
            });
            WriteList(w, f.Recurring, (ww, c) =>
            {
                WriteString(ww, c.Id);
                WriteString(ww, c.Name);
                ww.Write(c.MonthlyAmount);
                ww.Write((int)c.Category);
            });
            WriteList(w, f.Scheduled, (ww, p) =>
            {
                ww.Write(p.Day);
                ww.Write(p.Amount);
                ww.Write((int)p.Category);
                WriteString(ww, p.Description);
            });
            WriteList(w, f.Reports, (ww, m) =>
            {
                ww.Write(m.MonthIndex);
                ww.Write(m.Income);
                ww.Write(m.Expenses);
                ww.Write(m.CashAtClose);
                ww.Write(m.ByCategory.Count);
                foreach (var pair in m.ByCategory)
                {
                    ww.Write((int)pair.Key);
                    ww.Write(pair.Value);
                }
            });
            w.Write(f.EverNegative);
        }

        static void ReadFinance(BinaryReader r, Finance f)
        {
            f.Cash = r.ReadDouble();
            f.CreditLimit = r.ReadDouble();
            ReadList(r, f.Transactions, rr => new Transaction
            {
                Day = rr.ReadInt32(),
                Amount = rr.ReadDouble(),
                Category = (TxCategory)rr.ReadInt32(),
                Description = ReadString(rr),
            });
            ReadList(r, f.Recurring, rr => new RecurringCost
            {
                Id = ReadString(rr),
                Name = ReadString(rr),
                MonthlyAmount = rr.ReadDouble(),
                Category = (TxCategory)rr.ReadInt32(),
            });
            ReadList(r, f.Scheduled, rr => new ScheduledPayment
            {
                Day = rr.ReadInt32(),
                Amount = rr.ReadDouble(),
                Category = (TxCategory)rr.ReadInt32(),
                Description = ReadString(rr),
            });
            ReadList(r, f.Reports, rr =>
            {
                var report = new MonthlyReport
                {
                    MonthIndex = rr.ReadInt32(),
                    Income = rr.ReadDouble(),
                    Expenses = rr.ReadDouble(),
                    CashAtClose = rr.ReadDouble(),
                };
                int count = rr.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var category = (TxCategory)rr.ReadInt32();
                    report.ByCategory[category] = rr.ReadDouble();
                }
                return report;
            });
            f.EverNegative = r.ReadBoolean();
        }

        // ------------------------------------------------------------------ gigs and mail

        static void WriteFreelance(BinaryWriter w, FreelanceState f)
        {
            var p = f.Profile;
            w.Write(p.CompletedGigs);
            w.Write(p.FailedGigs);
            w.Write(p.TotalHours);
            w.Write(p.TotalEarned);
            w.Write((int)p.RateSetting);
            WriteList(w, p.RecentScores, (ww, score) => ww.Write(score));
            WriteList(w, f.Gigs, (ww, g) =>
            {
                ww.Write(g.Id);
                WriteString(ww, g.ClientName);
                ww.Write((int)g.Client);
                WriteString(ww, g.Title);
                ww.Write((int)g.Discipline);
                ww.Write(g.RequiredSkill);
                ww.Write(g.Hours);
                ww.Write(g.HoursDone);
                ww.Write(g.Pay);
                ww.Write(g.OfferedDay);
                ww.Write(g.ExpiresDay);
                ww.Write(g.DurationDays);
                ww.Write(g.DeadlineDay);
                ww.Write((int)g.Status);
                WriteString(ww, g.RiskFlags);
                ww.Write(g.ClientScore);
                ww.Write(g.ScoreModifier);
                ww.Write(g.LateNoticeSent);
                ww.Write(g.ScopeCreepChecked);
                ww.Write(g.Sessions);
                ww.Write(g.PerformanceSum);
                ww.Write(g.NonPayRisk);
                ww.Write(g.ScopeCreepChance);
                ww.Write(g.PaymentDelayDays);
            });
        }

        static void ReadFreelance(BinaryReader r, FreelanceState f)
        {
            var p = f.Profile;
            p.CompletedGigs = r.ReadInt32();
            p.FailedGigs = r.ReadInt32();
            p.TotalHours = r.ReadDouble();
            p.TotalEarned = r.ReadDouble();
            p.RateSetting = (RateSetting)r.ReadInt32();
            ReadList(r, p.RecentScores, rr => rr.ReadInt32());
            ReadList(r, f.Gigs, rr => new Gig
            {
                Id = rr.ReadInt32(),
                ClientName = ReadString(rr),
                Client = (ClientType)rr.ReadInt32(),
                Title = ReadString(rr),
                Discipline = (Skill)rr.ReadInt32(),
                RequiredSkill = rr.ReadDouble(),
                Hours = rr.ReadDouble(),
                HoursDone = rr.ReadDouble(),
                Pay = rr.ReadDouble(),
                OfferedDay = rr.ReadInt32(),
                ExpiresDay = rr.ReadInt32(),
                DurationDays = rr.ReadInt32(),
                DeadlineDay = rr.ReadInt32(),
                Status = (GigStatus)rr.ReadInt32(),
                RiskFlags = ReadString(rr),
                ClientScore = rr.ReadInt32(),
                ScoreModifier = rr.ReadInt32(),
                LateNoticeSent = rr.ReadBoolean(),
                ScopeCreepChecked = rr.ReadBoolean(),
                Sessions = rr.ReadInt32(),
                PerformanceSum = rr.ReadDouble(),
                NonPayRisk = rr.ReadDouble(),
                ScopeCreepChance = rr.ReadDouble(),
                PaymentDelayDays = rr.ReadInt32(),
            });
        }

        static void WriteMessage(BinaryWriter w, Message m)
        {
            w.Write(m.Id);
            w.Write(m.Day);
            WriteString(w, m.EventId);
            w.Write(m.ContextId);
            w.Write((int)m.Kind);
            WriteString(w, m.Title);
            WriteString(w, m.Body);
            WriteList(w, m.Options, (ww, o) =>
            {
                WriteString(ww, o.Label);
                WriteString(ww, o.Detail);
            });
            WriteStrings(w, m.OptionIds);
            w.Write(m.DefaultOption);
            w.Write(m.ExpiresDay);
            w.Write(m.ChosenOption);
            w.Write(m.IsRead);
            WriteString(w, m.Outcome);
        }

        static Message ReadMessage(BinaryReader r)
        {
            var m = new Message
            {
                Id = r.ReadInt32(),
                Day = r.ReadInt32(),
                EventId = ReadString(r),
                ContextId = r.ReadInt32(),
                Kind = (MessageKind)r.ReadInt32(),
                Title = ReadString(r),
                Body = ReadString(r),
            };
            ReadList(r, m.Options, rr => new MessageOption { Label = ReadString(rr), Detail = ReadString(rr) });
            ReadStrings(r, m.OptionIds);
            m.DefaultOption = r.ReadInt32();
            m.ExpiresDay = r.ReadInt32();
            m.ChosenOption = r.ReadInt32();
            m.IsRead = r.ReadBoolean();
            m.Outcome = ReadString(r);
            return m;
        }

        // ------------------------------------------------------------------ games and ideas

        static void WriteProject(BinaryWriter w, Project p, int version)
        {
            w.Write(p.Id);
            WriteString(w, p.Title);
            w.Write(p.IdeaId);
            w.Write((int)p.Genre);
            WriteString(w, p.Theme);
            WriteString(w, p.Hook);
            w.Write((int)p.ArtStyle);
            w.Write((int)p.Scope);
            w.Write((int)p.Status);
            w.Write(p.StartDay);
            w.Write(p.CompleteDay);
            w.Write(p.FunPotential);
            w.Write(p.HookStrength);
            w.Write(p.JamProven);
            WriteList(w, p.Workstreams, (ww, s) =>
            {
                ww.Write((int)s.Discipline);
                ww.Write(s.Required);
                ww.Write(s.Done);
                ww.Write(s.QualitySum);
                WriteStrings(ww, s.Features);
            });
            w.Write(p.InitialEstimatePw);
            w.Write(p.PolishPw);
            w.Write(p.BugsCreated);
            w.Write(p.BugsFixed);
            w.Write(p.BoughtAssetPack);
            w.Write((int)p.Platform);
            w.Write(p.StorePageDay);
            w.Write(p.Wishlists);
            w.Write(p.Price);
            w.Write(p.ReleaseDay);
            w.Write(p.Result != null);
            if (p.Result != null)
            {
                var r = p.Result;
                w.Write(r.Pillars.Count);
                foreach (var pair in r.Pillars)
                {
                    w.Write((int)pair.Key);
                    w.Write(pair.Value);
                }
                w.Write(r.Stability);
                w.Write(r.Quality);
                w.Write(r.PositiveShare);
                w.Write(r.ReviewCount);
                w.Write(r.Viral);
                w.Write(r.LaunchWishlists);
                w.Write(r.ForecastLow);
                w.Write(r.ForecastHigh);
                WriteList(w, r.MonthlyUnits, (ww, units) => ww.Write(units));
                if (version >= 4) WriteLaunchWeek(w, r);
            }
            w.Write(p.UnitsSold);
            w.Write(p.GrossRevenue);
            w.Write(p.NetRevenue);
            w.Write(p.NetUnpaid);
            w.Write(p.PostMortemDay);
            if (version >= 5)
            {
                w.Write(p.FestDay);
                w.Write(p.FestDone);
                w.Write(p.DemoProgress);
                w.Write(p.DemoBugs);
                w.Write(p.FestQuality);
                w.Write(p.FestTotal);
                w.Write(p.FestWishlists);
                w.Write(p.DemoPlayers);
            }
        }

        static Project ReadProject(BinaryReader r, int version)
        {
            var p = new Project
            {
                Id = r.ReadInt32(),
                Title = ReadString(r),
                IdeaId = r.ReadInt32(),
                Genre = (GenreId)r.ReadInt32(),
                Theme = ReadString(r),
                Hook = ReadString(r),
                ArtStyle = (ArtStyleId)r.ReadInt32(),
                Scope = (ProjectScope)r.ReadInt32(),
                Status = (ProjectStatus)r.ReadInt32(),
                StartDay = r.ReadInt32(),
                CompleteDay = r.ReadInt32(),
                FunPotential = r.ReadDouble(),
                HookStrength = r.ReadDouble(),
                JamProven = r.ReadBoolean(),
            };
            ReadList(r, p.Workstreams, rr =>
            {
                var stream = new Workstream
                {
                    Discipline = (Skill)rr.ReadInt32(),
                    Required = rr.ReadDouble(),
                    Done = rr.ReadDouble(),
                    QualitySum = rr.ReadDouble(),
                };
                ReadStrings(rr, stream.Features);
                return stream;
            });
            p.InitialEstimatePw = r.ReadDouble();
            p.PolishPw = r.ReadDouble();
            p.BugsCreated = r.ReadDouble();
            p.BugsFixed = r.ReadDouble();
            p.BoughtAssetPack = r.ReadBoolean();
            p.Platform = (StorePlatform)r.ReadInt32();
            p.StorePageDay = r.ReadInt32();
            p.Wishlists = r.ReadDouble();
            p.Price = r.ReadDouble();
            p.ReleaseDay = r.ReadInt32();
            if (r.ReadBoolean())
            {
                var result = new ReleaseResult();
                int pillars = r.ReadInt32();
                for (int i = 0; i < pillars; i++)
                {
                    var pillar = (Pillar)r.ReadInt32();
                    result.Pillars[pillar] = r.ReadDouble();
                }
                result.Stability = r.ReadDouble();
                result.Quality = r.ReadDouble();
                result.PositiveShare = r.ReadDouble();
                result.ReviewCount = r.ReadInt32();
                result.Viral = r.ReadBoolean();
                result.LaunchWishlists = r.ReadDouble();
                result.ForecastLow = r.ReadDouble();
                result.ForecastHigh = r.ReadDouble();
                ReadList(r, result.MonthlyUnits, rr => rr.ReadDouble());
                p.Result = result;
                if (version >= 4) ReadLaunchWeek(r, result);
            }
            p.UnitsSold = r.ReadDouble();
            p.GrossRevenue = r.ReadDouble();
            p.NetRevenue = r.ReadDouble();
            p.NetUnpaid = r.ReadDouble();
            p.PostMortemDay = r.ReadInt32();
            if (version >= 5)
            {
                p.FestDay = r.ReadInt32();
                p.FestDone = r.ReadBoolean();
                p.DemoProgress = r.ReadDouble();
                p.DemoBugs = r.ReadDouble();
                p.FestQuality = r.ReadDouble();
                p.FestTotal = r.ReadDouble();
                p.FestWishlists = r.ReadDouble();
                p.DemoPlayers = r.ReadDouble();
            }
            if (version < 4 && p.Result != null)
            {
                // Before launch week: the reviews were what they were at launch, and stay that way.
                var result = p.Result;
                result.LaunchStability = result.Stability;
                result.LaunchQuality = result.Quality;
                result.Mood = result.LaunchMood = result.PositiveShare;
                result.ReviewVotes = p.UnitsSold * Tuning.ReviewRate;
                result.PositiveVotes = result.ReviewVotes * result.PositiveShare;
            }
            return p;
        }

        static void WriteLaunchWeek(BinaryWriter w, ReleaseResult r)
        {
            w.Write(r.LaunchStability);
            w.Write(r.LaunchQuality);
            w.Write(r.Mood);
            w.Write(r.LaunchMood);
            w.Write(r.Goodwill);
            w.Write(r.SalesMultiplier);
            w.Write(r.ReviewVotes);
            w.Write(r.PositiveVotes);
            w.Write(r.Hotfixes);
            w.Write(r.HotfixedBugs);
            w.Write(r.Regressions);
            w.Write(r.Answers);
            w.Write(r.PushUnits);
            WriteList(w, r.Critics, (ww, c) =>
            {
                WriteString(ww, c.Outlet);
                ww.Write(c.Score);
                ww.Write(c.Day);
                WriteString(ww, c.Quote);
            });
            WriteList(w, r.LaunchDays, (ww, d) =>
            {
                ww.Write(d.Units);
                ww.Write(d.Net);
                ww.Write(d.PositiveShare);
                ww.Write(d.Reviews);
            });
        }

        static void ReadLaunchWeek(BinaryReader r, ReleaseResult result)
        {
            result.LaunchStability = r.ReadDouble();
            result.LaunchQuality = r.ReadDouble();
            result.Mood = r.ReadDouble();
            result.LaunchMood = r.ReadDouble();
            result.Goodwill = r.ReadDouble();
            result.SalesMultiplier = r.ReadDouble();
            result.ReviewVotes = r.ReadDouble();
            result.PositiveVotes = r.ReadDouble();
            result.Hotfixes = r.ReadInt32();
            result.HotfixedBugs = r.ReadDouble();
            result.Regressions = r.ReadInt32();
            result.Answers = r.ReadInt32();
            result.PushUnits = r.ReadDouble();
            ReadList(r, result.Critics, rr => new CriticReview { Outlet = ReadString(rr), Score = rr.ReadInt32(), Day = rr.ReadInt32(), Quote = ReadString(rr) });
            ReadList(r, result.LaunchDays, rr => new LaunchDay { Units = rr.ReadDouble(), Net = rr.ReadDouble(), PositiveShare = rr.ReadDouble(), Reviews = rr.ReadInt32() });
        }

        static void WriteIdea(BinaryWriter w, IdeaCard i)
        {
            w.Write(i.Id);
            WriteString(w, i.Title);
            w.Write((int)i.Genre);
            WriteString(w, i.Theme);
            WriteString(w, i.Hook);
            WriteString(w, i.Source);
            w.Write(i.CreatedDay);
            WriteStrings(w, i.Traits);
            w.Write(i.JamProven);
            w.Write(i.Used);
            w.Write(i.FunPotential);
            w.Write(i.HookStrength);
            w.Write(i.Knowledge);
        }

        static IdeaCard ReadIdea(BinaryReader r)
        {
            var idea = new IdeaCard
            {
                Id = r.ReadInt32(),
                Title = ReadString(r),
                Genre = (GenreId)r.ReadInt32(),
                Theme = ReadString(r),
                Hook = ReadString(r),
                Source = ReadString(r),
                CreatedDay = r.ReadInt32(),
            };
            ReadStrings(r, idea.Traits);
            idea.JamProven = r.ReadBoolean();
            idea.Used = r.ReadBoolean();
            idea.FunPotential = r.ReadDouble();
            idea.HookStrength = r.ReadDouble();
            idea.Knowledge = r.ReadInt32();
            return idea;
        }

        // ------------------------------------------------------------------ helpers

        static void WriteString(BinaryWriter w, string value)
        {
            w.Write(value != null);
            if (value != null) w.Write(value);
        }

        static string ReadString(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;

        static void WriteStrings(BinaryWriter w, ICollection<string> values)
        {
            w.Write(values.Count);
            foreach (var value in values) WriteString(w, value);
        }

        static void ReadStrings(BinaryReader r, ICollection<string> into)
        {
            into.Clear();
            int count = ReadCount(r);
            for (int i = 0; i < count; i++) into.Add(ReadString(r));
        }

        static void WriteStringIntMap(BinaryWriter w, Dictionary<string, int> map)
        {
            w.Write(map.Count);
            foreach (var pair in map)
            {
                WriteString(w, pair.Key);
                w.Write(pair.Value);
            }
        }

        static void ReadStringIntMap(BinaryReader r, Dictionary<string, int> into)
        {
            into.Clear();
            int count = ReadCount(r);
            for (int i = 0; i < count; i++)
            {
                string key = ReadString(r);
                into[key] = r.ReadInt32();
            }
        }

        static void WriteList<T>(BinaryWriter w, List<T> items, Action<BinaryWriter, T> write)
        {
            w.Write(items.Count);
            foreach (var item in items) write(w, item);
        }

        static void ReadList<T>(BinaryReader r, List<T> into, Func<BinaryReader, T> read)
        {
            into.Clear();
            int count = ReadCount(r);
            for (int i = 0; i < count; i++) into.Add(read(r));
        }

        /// <summary>A collection length; a nonsensical one means the file is damaged.</summary>
        static int ReadCount(BinaryReader r)
        {
            int count = r.ReadInt32();
            if (count < 0 || count > 10_000_000) throw new InvalidDataException("The save is damaged.");
            return count;
        }
    }
}
