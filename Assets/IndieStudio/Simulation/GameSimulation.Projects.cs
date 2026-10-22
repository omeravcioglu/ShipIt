using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    /// <summary>Your own games: development, store page, marketing, launch, sales and post-mortems.</summary>
    public sealed partial class GameSimulation
    {
        static readonly double FirstMonthWeightTotal = Enumerable.Range(0, 30).Sum(d => Math.Exp(-d / 7.0));

        // ------------------------------------------------------------------ commands

        public Project StartProject(int ideaId, ProjectScope scope, ArtStyleId style, out string error)
        {
            error = null;
            if (State.ActiveProject != null) { error = "Finish, release or cancel your current project first."; return null; }
            if (State.IsDemo && scope != ProjectScope.Tiny) { error = "The demo is about your first Tiny game. Small games come with the full game."; return null; }
            var idea = State.Ideas.FirstOrDefault(i => i.Id == ideaId);
            if (idea == null || idea.Used) { error = "That idea is not available."; return null; }

            var project = PlanProject(idea, scope, style);
            project.Id = State.NextId++;
            project.InitialEstimatePw = project.TotalRequired * Rules.EstimateBias(State);
            idea.Used = true;
            State.Projects.Add(project);

            var genre = GameContent.Genre(idea.Genre);
            Post("project_started", MessageKind.Info, $"New project: {project.Title}",
                $"{genre.Name} · {project.Theme} · {GameContent.ArtStyle(style).Name} · {scope}. \"{project.Hook}.\"\n" +
                $"{project.FeatureCount} features to build. Your estimate: about {EstimatedSessions(project):0} Build sessions. " +
                "Spend a morning, afternoon or evening on Build to wire up the next feature.");
            CheckProgress();
            return project;
        }

        /// <summary>The project an idea would become, with its workstreams and named features (not added to the game).</summary>
        public Project PlanProject(IdeaCard idea, ProjectScope scope, ArtStyleId style)
        {
            var genre = GameContent.Genre(idea.Genre);
            var art = GameContent.ArtStyle(style);
            double basePw = scope == ProjectScope.Tiny ? Tuning.TinyBasePw : Tuning.SmallBasePw;
            if (scope == ProjectScope.Tiny && State.Lessons.Contains("ship_small")) basePw *= 0.9;
            if (scope == ProjectScope.Tiny && State.IsDemo) basePw *= DemoRules.TinyWorkScale;
            if (idea.JamProven) basePw *= 0.9;

            var project = new Project
            {
                Title = idea.Title,
                IdeaId = idea.Id,
                Genre = idea.Genre,
                Theme = idea.Theme,
                Hook = idea.Hook,
                ArtStyle = style,
                Scope = scope,
                StartDay = State.Day,
                FunPotential = idea.FunPotential,
                HookStrength = idea.HookStrength,
                JamProven = idea.JamProven,
            };
            foreach (var share in genre.WorkShare)
            {
                double required = basePw * share.Value * (share.Key == Skill.Art ? art.ArtCost : 1.0);
                var stream = new Workstream { Discipline = share.Key, Required = required };
                stream.Features.AddRange(FeatureCatalog.Pick(idea.Title, idea.Genre, share.Key, FeatureCatalog.CountFor(scope, required)));
                project.Workstreams.Add(stream);
            }
            return project;
        }

        public bool BuyAssetPack(int projectId, out string error)
        {
            error = null;
            var project = State.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || project.Status != ProjectStatus.InDevelopment) { error = "Only for projects in development."; return false; }
            if (project.BoughtAssetPack) { error = "You already bought a pack for this game."; return false; }
            var art = project.Stream(Skill.Art);
            if (art == null || art.Remaining <= 0) { error = "There is no art work left to replace."; return false; }
            double price = AssetPackPrice(project);
            if (State.Finance.Cash - price < -State.Finance.CreditLimit) { error = "Not enough money or credit."; return false; }

            State.Finance.Apply(State.Day, -price, TxCategory.Assets, $"Asset pack for {project.Title}");
            double covered = art.Remaining * 0.4;
            art.Done += covered;
            art.QualitySum += covered * 6.0; // bought art can be better than your own…
            project.BoughtAssetPack = true;  // …but players have seen it before (Innovation −0.6)
            return true;
        }

        public double AssetPackPrice(Project project) => project.Scope == ProjectScope.Tiny ? 80 : 1200;

        public bool OpenStorePage(int projectId, StorePlatform platform, out string error)
        {
            error = null;
            var project = State.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || project.IsReleased || project.Status == ProjectStatus.Cancelled) { error = "No such project."; return false; }
            if (project.HasStorePage) { error = "This game already has a store page."; return false; }
            if (platform == StorePlatform.None) { error = "Pick a store."; return false; }
            if (platform == StorePlatform.Vapor && State.IsDemo) { error = "Vapor pages and wishlists come with the full game. In the demo, launch on scratch.io: it's free and instant."; return false; }

            if (platform == StorePlatform.Vapor)
            {
                if (project.Progress < 0.25) { error = "Show something first: Vapor pages need screenshots, so wait until the game is 25% done."; return false; }
                if (State.Finance.Cash - Tuning.VaporStoreFee < -State.Finance.CreditLimit) { error = "Not enough money or credit for the $100 fee."; return false; }
                State.Finance.Apply(State.Day, -Tuning.VaporStoreFee, TxCategory.StoreFees, $"Vapor Direct fee: {project.Title}");
                double carried = State.Audience.Followers * Tuning.FollowersWhoWishlistOnPageOpen;
                project.Wishlists += carried;
                Post("store_page", MessageKind.Info, $"{project.Title} is on Vapor",
                    $"The \"Coming Soon\" page is live. {carried:0} of your followers wishlisted it right away. " +
                    "Marketing now adds wishlists. Vapor needs the page public for 14 days before launch.");
            }
            else
            {
                Post("store_page", MessageKind.Info, $"{project.Title} has a scratch.io page",
                    "Free, instant, and almost nobody browses it. Good for a first release; no wishlists here.");
            }
            project.Platform = platform;
            project.StorePageDay = State.Day;
            CheckProgress();
            return true;
        }

        public bool Release(int projectId, double price, out string error)
        {
            error = null;
            var project = State.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || project.Status != ProjectStatus.ReadyToRelease) { error = "Finish development first."; return false; }
            if (!project.HasStorePage) { error = "Open a store page first (scratch.io is free)."; return false; }
            int daysLive = State.Day - project.StorePageDay;
            if (project.Platform == StorePlatform.Vapor && daysLive < Tuning.VaporMinStorePageDays)
            {
                error = $"Vapor needs the page public for {Tuning.VaporMinStorePageDays} days ({Tuning.VaporMinStorePageDays - daysLive} to go).";
                return false;
            }

            price = ClampPrice(project.Scope, price);
            var result = Rules.EvaluateQuality(State, project, price);
            result.Quality = Rules.Clamp(result.Quality + State.RngSales.Gaussian() * 0.03, 0.05, 0.98);
            result.PositiveShare = Rules.PositiveShare(result.Quality, price, project.Scope);
            ForecastFirstMonth(project, price, out result.ForecastLow, out result.ForecastHigh);
            PlanSales(project, result, price);

            project.Result = result;
            project.Price = price;
            project.ReleaseDay = State.Day;
            project.Status = ProjectStatus.Released;
            bool leftFest = LeaveFestOnRelease(project);
            StartLaunch(project);
            State.ShippedGames++;
            State.Founder.Morale = Rules.Clamp(State.Founder.Morale + 10, 0, 100);

            string store = project.Platform == StorePlatform.Vapor ? $"Vapor with {project.Wishlists:0} wishlists" : "scratch.io";
            Post("launch", MessageKind.Info, $"{project.Title} is out!",
                $"Released on {store} at ${price:0.00}. Launch-week discount: {(project.Platform == StorePlatform.Vapor ? "10%" : "none")}.\n" +
                $"Your forecast for the first month: {result.ForecastLow:0}–{result.ForecastHigh:0} copies. Morale +10.\n" +
                (State.IsDemo ? "Your first game is out in the world."
                    : "Launch week starts now: hotfixes count most in the first days, players want answers, and posts sell copies.") +
                (leftFest ? "\nIt was signed up for Next Fest, which is for unreleased games, so it left the fest." : ""));
            CheckProgress();
            return true;
        }

        public void CancelProject(int projectId)
        {
            var project = State.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || project.IsReleased || project.Status == ProjectStatus.Cancelled) return;
            project.Status = ProjectStatus.Cancelled;
            State.Founder.Morale = Rules.Clamp(State.Founder.Morale - 8, 0, 100);
            Post("project_cancelled", MessageKind.Info, $"{project.Title} is shelved",
                "Letting go hurts (morale −8), but the skills stay with you.");
        }

        public static double ClampPrice(ProjectScope scope, double price) =>
            scope == ProjectScope.Tiny ? Rules.Clamp(price, 0.99, 7.99) : Rules.Clamp(price, 4.99, 24.99);

        // ------------------------------------------------------------------ queries for the UI

        public double EstimatedRemainingPw(Project project) => Rules.RemainingWorkEstimate(State, project);

        /// <summary>
        /// Build sessions the whole project needs, as the player estimates it (optimistic, Design/05 §6), at
        /// today's skills and an ordinary session per slot.
        /// </summary>
        public double EstimatedSessions(Project project) =>
            project.Workstreams.Sum(w => w.Required * Rules.EstimateBias(State) / Math.Max(1e-6, Rules.DevelopOutput(State, project, w.Discipline).Value));

        /// <summary>Build sessions left, as the player estimates it.</summary>
        public double EstimatedSessionsRemaining(Project project) =>
            project.Workstreams.Sum(w => w.Remaining * Rules.EstimateBias(State) / Math.Max(1e-6, Rules.DevelopOutput(State, project, w.Discipline).Value));

        /// <summary>Build sessions left for one discipline's next feature, as the player estimates it.</summary>
        public double EstimatedSessionsForFeature(Project project, Workstream stream)
        {
            if (stream.IsDone) return 0;
            double left = (1 - stream.FeatureProgress(stream.CurrentFeature)) * stream.FeatureSize;
            return left * Rules.EstimateBias(State) / Math.Max(1e-6, Rules.DevelopOutput(State, project, stream.Discipline).Value);
        }

        /// <summary>The discipline with the most work left: what "Build" works on when you don't pick.</summary>
        public static Workstream Bottleneck(Project project) =>
            project.Workstreams.Where(w => w.Remaining > 0).OrderByDescending(w => w.Remaining).FirstOrDefault();

        /// <summary>The quality the player believes the game has: a range, not the truth (Design/17).</summary>
        public void QualityEstimate(Project project, double price, out double low, out double high)
        {
            double quality = Rules.EvaluateQuality(State, project, price).Quality;
            double judgment = (State.Founder.Get(Skill.Design) + State.Founder.Get(Skill.Production)) / 2;
            double spread = Rules.Clamp(0.16 - 0.02 * judgment, 0.04, 0.15);
            low = Rules.Clamp(quality - spread, 0.02, 0.98);
            high = Rules.Clamp(quality + spread, 0.02, 0.98);
        }

        public void ForecastFirstMonth(Project project, double price, out double low, out double high)
        {
            QualityEstimate(project, price, out double qLow, out double qHigh);
            double priceFactor = Math.Pow(Rules.FairPrice(project.Scope) / ClampPrice(project.Scope, price), 0.8);
            double market = GameContent.Genre(project.Genre).Market;
            double followers = State.Audience.Followers;
            if (project.Platform == StorePlatform.Vapor)
            {
                low = (project.Wishlists * Tuning.WcrBase * (0.5 + qLow) * 0.7 + market * 6 * qLow * 0.6 + followers * Tuning.FollowerBuyRate * qLow) * priceFactor;
                high = (project.Wishlists * Tuning.WcrBase * (0.5 + qHigh) * 1.42 * (1 + Tuning.OrganicShare * qHigh) + market * 6 * qHigh * 1.6
                        + followers * Tuning.FollowerBuyRate * qHigh) * priceFactor;
            }
            else
            {
                low = (20 * 0.5 * (0.5 + qLow) * market / 3 + followers * Tuning.FollowerBuyRate * 0.6 * qLow) * priceFactor;
                high = (20 * 2.0 * (0.5 + qHigh) * market / 3 + followers * Tuning.FollowerBuyRate * 0.6 * qHigh) * priceFactor;
            }
        }

        public Project MarketingTarget() =>
            State.ActiveProject ?? State.ReleasedProjects.OrderByDescending(p => p.ReleaseDay).FirstOrDefault();

        /// <summary>Copies launch day itself is expected to sell: the first day of the first month's sales curve.</summary>
        public static double ExpectedLaunchDayUnits(Project project) =>
            project.Result == null || project.Result.MonthlyUnits.Count == 0 ? 0 : project.Result.MonthlyUnits[0] / FirstMonthWeightTotal;

        /// <summary>What you keep from one copy sold in launch week, after the launch discount, refunds and the store's cut.</summary>
        public static double NetPerLaunchCopy(Project project)
        {
            bool vapor = project.Platform == StorePlatform.Vapor;
            double gross = project.Price * (vapor ? 1 - Tuning.LaunchDiscount : 1);
            return gross * (1 - Tuning.RefundRate) * (1 - (vapor ? Tuning.VaporPlatformFee : Tuning.ScratchPlatformFee));
        }

        // ------------------------------------------------------------------ work

        void DoDevelop(int day, PlanBlock plan, double performance)
        {
            var project = State.ActiveProject;
            var log = State.CurrentWeek;
            double productivity = Rules.Productivity(State).Value;
            if (project == null)
            {
                log.IdleDevelopBlocks++;
                State.Stats.IdleDevelopBlocks++;
                if (_report != null) _report.Title = "Tinkered without a project";
                Note("Practice only. Start a project in the Project app to build a game.");
                GainXp(plan.HasFocus ? plan.Focus : Skill.Programming, Tuning.BlockHours * productivity * 0.5);
                return;
            }
            if (project.Status == ProjectStatus.ReadyToRelease)
            {
                Polish(project, productivity, performance);
                return;
            }

            double hoursLeft = Tuning.BlockHours;
            Workstream first = null;
            int firstFeature = 0;
            while (hoursLeft > 1e-6)
            {
                var stream = PickStream(project, plan);
                if (stream == null) break;
                int doneBefore = stream.FeaturesDone;
                if (first == null)
                {
                    first = stream;
                    firstFeature = stream.CurrentFeature;
                    SetSubject($"Built {stream.FeatureName(firstFeature)}", stream.FeatureName(firstFeature), stream.Discipline, stream.FeatureProgress(firstFeature));
                }

                double pwPerHour = Rules.DevelopOutput(State, project, stream.Discipline).Value * performance / Tuning.BlockHours;
                double pw = pwPerHour * hoursLeft;
                double hoursUsed = hoursLeft;
                if (pw > stream.Remaining)
                {
                    pw = stream.Remaining;
                    hoursUsed = pw / pwPerHour;
                }

                double skill = State.Founder.Get(stream.Discipline);
                double bonus = stream.Discipline == Skill.Art && State.Owned.Contains("pro_art_tools") && skill >= 4 ? 0.3 : 0;
                bonus += Tuning.MinigameQualityBonus * (performance - 1);
                double quality = Rules.TaskQuality(skill, Rules.TaskDifficulty(State, project, stream.Discipline), bonus);
                stream.Done += pw;
                stream.QualitySum += pw * quality;

                double bugsPerPw = (stream.Discipline == Skill.Programming ? Tuning.BugsPerPwProgramming : Tuning.BugsPerPwOther)
                                   * Math.Max(0.4, 1.5 - 0.1 * skill) * (1 + 0.01 * Math.Max(0, State.Founder.Burnout - 30));
                project.BugsCreated += pw * bugsPerPw;

                for (int i = doneBefore; i < stream.FeaturesDone; i++) Note($"{SlotReport.FinishedPrefix}{stream.FeatureName(i)} ({stream.Discipline})");
                GainXp(stream.Discipline, hoursUsed * productivity);
                log.ProjectPw += pw;
                hoursLeft -= hoursUsed;
            }

            if (first != null && _report != null)
            {
                _report.ProgressAfter = first.FeatureProgress(firstFeature);
                if (_report.ProgressAfter < 1) _report.Title = $"Worked on {first.FeatureName(firstFeature)}";
                Note($"{project.Title}: {project.FeaturesDone}/{project.FeatureCount} features, {Rules.Pct(project.Progress)} done");
            }

            if (project.AllWorkDone && project.Status == ProjectStatus.InDevelopment)
            {
                project.Status = ProjectStatus.ReadyToRelease;
                project.CompleteDay = day;
                Post("project_complete", MessageKind.Info, $"{project.Title}: content complete",
                    "Every feature is built. Build sessions now polish the game: squash bugs and add a little quality until you launch it." +
                    (project.HasStorePage ? "" : " Open a store page before you can launch."));
            }
        }

        static Workstream PickStream(Project project, PlanBlock plan)
        {
            if (plan.HasFocus)
            {
                var focused = project.Stream(plan.Focus);
                if (focused != null && focused.Remaining > 0) return focused;
            }
            return Bottleneck(project);
        }

        void Polish(Project project, double productivity, double performance)
        {
            var f = State.Founder;
            double skill = Math.Max(f.Get(Skill.Programming), f.Get(Skill.Design));
            double pw = Tuning.BlockHours / Tuning.PersonWeekHours * Rules.Efficiency(skill) * productivity * performance;
            double bugsBefore = project.BugsRemaining;
            double polishBefore = Rules.PolishBonus(State, project);
            project.PolishPw += pw;
            project.BugsFixed = Math.Min(project.BugsCreated, project.BugsFixed + pw * Tuning.BugsFixedPerPolishPw);
            if (_report != null)
            {
                SetSubject($"Polished {project.Title}", "Polish", Skill.Programming, polishBefore);
                _report.ProgressAfter = Rules.PolishBonus(State, project);
            }
            Note($"Squashed {bugsBefore - project.BugsRemaining:0} bugs ({project.BugsRemaining:0} left).");
            GainXp(Skill.Programming, Tuning.BlockHours * productivity * 0.5);
            GainXp(Skill.Design, Tuning.BlockHours * productivity * 0.5);
        }

        void DoMarketing(int day, PlanBlock plan)
        {
            var target = MarketingTarget();
            var post = (MarketingPost)plan.Option;
            double factor = Rules.MarketingFactor(State);
            double appeal = 0.7;
            if (target != null)
            {
                double quality = Rules.EvaluateQuality(State, target, target.Price > 0 ? target.Price : Rules.FairPrice(target.Scope)).Quality;
                appeal = Rules.StorePageFactor(State, target, quality);
            }

            // Same average reach for every kind of post; they differ in spread and in what they grow.
            double median = 1, sigma = Tuning.FollowerSigma, followerShare = 1, wishlistShare = 1;
            if (post == MarketingPost.Devlog) { median = 1.45; sigma = 0.5; followerShare = 1.4; wishlistShare = 0.6; }
            else if (post == MarketingPost.Meme) { median = 0.55; sigma = 1.5; }

            double luck = State.RngMarketing.LogNormal(median, sigma);
            double followers = Tuning.FollowersPerMarketingBlock * factor * appeal * luck * followerShare;
            var launch = LaunchWeekGame;
            if (launch != null) followers *= Tuning.LaunchMomentum;
            State.Audience.Followers += followers;
            State.CurrentWeek.FollowersGained += followers;
            if (_report != null)
            {
                _report.Title = post == MarketingPost.Devlog ? "Wrote a devlog" : post == MarketingPost.Meme ? "Posted a meme" : "Posted screenshots";
                _report.HasSkill = true;
                _report.Skill = Skill.Marketing;
            }
            Note($"+{followers:0.#} followers ({State.Audience.Followers:#,0} total)");
            if (launch != null)
            {
                double sold = LaunchPush(launch, followers, luck, day);
                Note($"Launch momentum: twice the reach, and {sold:0.#} copies of {launch.Title} sold right away.");
            }

            if (target != null && target.Platform == StorePlatform.Vapor && target.HasStorePage && !target.IsReleased)
            {
                double wishlists = Tuning.WishlistsPerMarketingBlock * factor * appeal * wishlistShare
                                   * State.RngMarketing.LogNormal(1, Tuning.WishlistSigma) * (luck > 3 ? Math.Sqrt(luck) : 1);
                if (InFest(target))
                {
                    wishlists *= Tuning.FestMarketingBoost;
                    Note("Next Fest week: players are browsing, so posts bring more wishlists.");
                }
                target.Wishlists += wishlists;
                State.CurrentWeek.WishlistsGained += wishlists;
                Note($"+{wishlists:0} wishlists for {target.Title} ({target.Wishlists:#,0} total)");
            }
            else if (target == null || !target.IsReleased)
                Note("No Vapor page yet, so no wishlists. Followers wishlist when the page opens.");
            GainXp(Skill.Marketing, Tuning.BlockHours);

            if (luck >= 6 && !OnCooldown("post_took_off", 21))
            {
                double views = followers * State.RngMarketing.Range(60, 120);
                string what = post == MarketingPost.Devlog ? "your devlog" : target != null ? $"a GIF of {target.Title}" : "a silly dev meme";
                Post("post_took_off", MessageKind.Info, "A post took off!",
                    $"{char.ToUpper(what[0]) + what.Substring(1)} got about {views:#,0} views. +{followers:0} followers. Luck matters — but only if you keep posting.");
            }
        }

        // ------------------------------------------------------------------ sales

        void PlanSales(Project project, ReleaseResult result, double price)
        {
            var rng = State.RngSales;
            var genre = GameContent.Genre(project.Genre);
            double q = result.Quality;
            double priceFactor = Math.Pow(Rules.FairPrice(project.Scope) / price, 0.8);
            double followers = State.Audience.Followers;

            double month1;
            if (project.Platform == StorePlatform.Vapor)
            {
                double wcr = Tuning.WcrBase * (0.5 + q) * rng.LogNormal(1, Tuning.WcrSigma);
                month1 = project.Wishlists * wcr;
                month1 += Tuning.OrganicShare * month1 * q;
                month1 += genre.Market * 6 * q * rng.LogNormal(1, 0.5);
                month1 += followers * Tuning.FollowerBuyRate * q;
            }
            else
            {
                month1 = rng.LogNormal(20, 0.7) * (0.5 + q) * genre.Market / 3 + followers * Tuning.FollowerBuyRate * 0.6 * q;
            }
            month1 *= priceFactor;

            double year1 = month1 * (Tuning.Year1Base + Tuning.Year1PerQuality * q);
            double viralChance = Tuning.ViralChance + 0.01 * (genre.StreamerAppeal - 3);
            if (q > Tuning.ViralQualityGate && rng.Chance(viralChance))
            {
                double boost = rng.LogNormal(Tuning.ViralMultiplierMedian, 0.6);
                result.Viral = true;
                month1 *= boost;
                year1 *= boost;
            }

            result.LaunchWishlists = project.Wishlists;
            result.MonthlyUnits.Add(month1);
            double rest = Math.Max(0, year1 - month1);
            double totalWeight = Enumerable.Range(0, 11).Sum(i => Math.Pow(Tuning.MonthlyDecay, i));
            for (int i = 0; i < 11; i++) result.MonthlyUnits.Add(rest * Math.Pow(Tuning.MonthlyDecay, i) / totalWeight);
            for (int i = 0; i < 12; i++) result.MonthlyUnits.Add(year1 * Tuning.TailSharePerMonth);
        }

        void ProcessSales(int day)
        {
            foreach (var project in State.ReleasedProjects)
            {
                var r = project.Result;
                int t = day - project.ReleaseDay;
                if (r == null || t < 0) continue;

                int month = t / 30;
                double monthUnits = month < r.MonthlyUnits.Count ? r.MonthlyUnits[month] : r.MonthlyUnits[r.MonthlyUnits.Count - 1] * 0.5;
                double weight = month == 0 ? Math.Exp(-(t % 30) / 7.0) / FirstMonthWeightTotal : 1.0 / 30;
                double units = monthUnits * weight * State.RngSales.LogNormal(1, 0.15) * r.SalesMultiplier;
                SellCopies(project, units, day);
            }
        }

        void ScheduleVaporPayouts(int day)
        {
            foreach (var project in State.ReleasedProjects.Where(p => p.Platform == StorePlatform.Vapor && p.NetUnpaid > 0.5))
            {
                string month = GameCalendar.MonthLabel(GameCalendar.MonthIndex(day) - 1);
                Schedule(day + Tuning.VaporPayoutDelayDays - 1, project.NetUnpaid, TxCategory.GameSales, $"Vapor payout: {project.Title} ({month})");
                project.NetUnpaid = 0;
            }
        }

        void PayScratchSales(int day)
        {
            foreach (var project in State.ReleasedProjects.Where(p => p.Platform == StorePlatform.ScratchIo && p.NetUnpaid > 0.5))
            {
                Schedule(day + 2, project.NetUnpaid, TxCategory.GameSales, $"scratch.io payout: {project.Title}");
                project.NetUnpaid = 0;
            }
        }

        // ------------------------------------------------------------------ post-mortem

        void CheckProjectMilestones(int day)
        {
            foreach (var project in State.ReleasedProjects.Where(p => p.PostMortemDay < 0 && day - p.ReleaseDay >= 30).ToList())
            {
                project.PostMortemDay = day;
                PostPostMortem(project);
            }
        }

        static string LaunchWeekLine(ReleaseResult r)
        {
            if (r.Hotfixes == 0 && r.Answers == 0) return "";
            string moved = Math.Abs(r.PositiveShare - r.LaunchMood) >= 0.005
                ? $"reviews went from {Rules.Pct(r.LaunchMood)} at launch to {Rules.Pct(r.PositiveShare)}"
                : "reviews held where they started";
            return $"After launch: {r.Hotfixes} hotfix{(r.Hotfixes == 1 ? "" : "es")} and {r.Answers} answer session{(r.Answers == 1 ? "" : "s")}; {moved}.\n";
        }

        void PostPostMortem(Project project)
        {
            var r = project.Result;
            var genre = GameContent.Genre(project.Genre);
            var style = GameContent.ArtStyle(project.ArtStyle);
            double overrun = project.InitialEstimatePw > 0 ? project.TotalRequired / project.InitialEstimatePw - 1 : 0;
            double fit = GameContent.StyleFit(project.Genre, project.ArtStyle);
            string fitLabel = fit >= 0.75 ? "a great" : fit > 0 ? "a good" : fit == 0 ? "a neutral" : "a poor";
            string knowledge = $"{genre.Name} × {style.Name}: {fitLabel.Substring(2)} fit";
            if (!State.Knowledge.Contains(knowledge)) State.Knowledge.Add(knowledge);
            GainXp(Skill.Production, 20);

            var weighted = genre.Weights.Where(w => w.Value > 0).Select(w => new { Pillar = w.Key, Score = r.Pillars[w.Key], w.Value }).ToList();
            var weakest = weighted.OrderBy(x => (x.Score - 10) * x.Value).First();
            var strongest = weighted.OrderByDescending(x => x.Score * x.Value).First();

            string pillars = string.Join(" · ", r.Pillars.Select(p => $"{p.Key} {p.Value:0.0}")) + $" · Stability {r.Stability:0.0}";
            string reviews = r.ReviewCount < 10
                ? $"only {r.ReviewCount} review{(r.ReviewCount == 1 ? "" : "s")} so far — too few for a score"
                : $"{r.ReviewLabel} ({Rules.Pct(r.PositiveShare)} of {r.ReviewCount})";
            string body =
                $"First 30 days: {project.UnitsSold:#,0} copies, {Rules.Money(project.NetRevenue)} earned after fees and refunds. Reviews: {reviews}.\n" +
                $"Your forecast was {r.ForecastLow:0}–{r.ForecastHigh:0} copies." + (r.Viral ? " It went viral." : "") + "\n" +
                LaunchWeekLine(r) +
                $"Estimate: you planned {project.InitialEstimatePw:0.0} person-weeks; it took {project.TotalRequired:0.0} ({overrun:+0%;-0%;0%}).\n" +
                $"What players saw (now revealed): {pillars}.\n" +
                $"Strongest: {strongest.Pillar}. Biggest drag: {weakest.Pillar}. {genre.Name} × {style.Name} turned out to be {fitLabel} fit.\n" +
                "Pick a lesson to carry into every future project:";

            var candidates = new List<string>();
            if (overrun > 0.2) candidates.Add("buffer_estimates");
            if (project.Platform == StorePlatform.Vapor ? r.LaunchWishlists < 500 : State.Audience.Followers < 200) candidates.Add("market_early");
            if (r.Stability < 6.5 || r.PositiveShare < 0.7) candidates.Add("polish_pays");
            if (State.Founder.Get(Skill.Art) < style.SkillFloor) candidates.Add("know_your_limits");
            if (project.Scope == ProjectScope.Tiny) candidates.Add("ship_small");
            candidates.Add("hook_first");
            foreach (var lesson in LessonCatalog.All) candidates.Add(lesson.Id);

            var options = candidates.Distinct().Where(id => !State.Lessons.Contains(id)).Take(3).ToList();
            if (options.Count == 0)
            {
                Post("post_mortem", MessageKind.Info, $"Post-mortem: {project.Title}", body.Replace("\nPick a lesson to carry into every future project:", ""), project.Id);
                return;
            }

            var message = Post("post_mortem", MessageKind.Decision, $"Post-mortem: {project.Title}", body, project.Id, 14,
                options.Select(id => LessonCatalog.Get(id).Title).ToArray());
            for (int i = 0; i < options.Count; i++) message.Options[i].Detail = LessonCatalog.Get(options[i]).Effect;
            message.OptionIds.AddRange(options);
        }
    }
}
