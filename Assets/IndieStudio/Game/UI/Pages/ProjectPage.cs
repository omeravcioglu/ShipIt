using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Start a game, watch it grow (feature by feature, and as a little screenshot of the game itself), open a
    /// store page, launch it, and hear what players say (Design/05, Design/10).
    /// </summary>
    public sealed class ProjectPage : Page
    {
        static readonly double[] TinyPrices = { 0.99, 1.99, 2.99, 3.99, 4.99, 6.99 };
        static readonly double[] SmallPrices = { 4.99, 9.99, 12.99, 14.99, 19.99, 24.99 };

        int _ideaId = -1;
        ProjectScope _scope = ProjectScope.Tiny;
        ArtStyleId _style = ArtStyleId.PixelArt;
        double _price = -1;
        int _priceProjectId = -1;
        string _error;

        public ProjectPage(StudioUI studio) : base(studio) { }

        public override string Title => "Project";
        public override string IconPath => Icons.Gamepad;
        public override string AppClass => "app-project";

        public void SelectIdea(int ideaId)
        {
            _ideaId = ideaId;
            _error = null;
        }

        public override void Refresh()
        {
            Root.Clear();
            var project = State.ActiveProject;
            if (project == null) BuildNewProject();
            else BuildActive(project);
            BuildReleased();
        }

        // ------------------------------------------------------------------ new project

        void BuildNewProject()
        {
            var s = State;
            Header("Start a project", "Pick an idea, a scope and an art style. Every choice changes cost, audience and risk. Your estimates will be optimistic. Everyone's are at first.");
            BarNote = s.ShippedGames == 0 ? "Your first game" : $"Game #{s.ShippedGames + 1}";
            var ideas = s.UnusedIdeas.ToList();
            if (ideas.Count == 0)
            {
                Root.Add(Ui.Empty("Your idea notebook is empty. Spend a session or two on Research, or join a game jam."));
                return;
            }
            if (ideas.All(i => i.Id != _ideaId)) _ideaId = ideas[0].Id;

            Root.Add(Ui.Display("1. Pick an idea", "section-title"));
            var ideaRow = Ui.Row("row--top wrap");
            foreach (var idea in ideas)
            {
                var card = IdeasPage.IdeaCardView(idea, idea.Id == _ideaId);
                var id = idea.Id;
                card.Add(Ui.Btn(idea.Id == _ideaId ? "Selected" : "Choose", () => { _ideaId = id; Refresh(); }, "btn btn--small" + (idea.Id == _ideaId ? " btn--selected" : "")));
                ideaRow.Add(card);
            }
            Root.Add(ideaRow);

            var choices = Ui.Row("grid-row");
            var scopeCard = Ui.Card("2. Scope", "col-4");
            var scopeRow = Ui.Row("wrap");
            scopeRow.Add(ChoiceButton("Tiny", _scope == ProjectScope.Tiny, () => _scope = ProjectScope.Tiny,
                "A handful of features: two or three weeks alongside gigs. Cheap to fail, great to learn from."));
            scopeRow.Add(ChoiceButton("Small", _scope == ProjectScope.Small, () => _scope = ProjectScope.Small,
                "About sixty features: a year of focused work alongside gigs. A real indie game, worth a Vapor page and a proper launch. Too big for evenings after a day job."));
            scopeCard.Add(scopeRow);
            scopeCard.Add(Ui.Text(_scope == ProjectScope.Tiny ? "Tiny: finish something, learn everything." : "Small: a real indie game. A long road alone.", "small muted"));
            choices.Add(scopeCard);

            var styleCard = Ui.Card("3. Art style", "col-8");
            var styleRow = Ui.Row("wrap");
            double art = s.Founder.Get(Skill.Art);
            foreach (var style in GameContent.ArtStyles)
            {
                var st = style;
                string warning = art < st.SkillFloor ? $"\nYour Art skill ({art:0.0}) is below this style's floor ({st.SkillFloor:0}): it will look cheap unless you improve or buy assets." : "";
                string hardware = st.Is3D && !s.Owned.Contains("gaming_pc") ? "\n3D on your laptop: -20% output from slow builds." : "";
                var button = ChoiceButton(st.Name + (art < st.SkillFloor ? " (!)" : ""), _style == st.Id, () => _style = st.Id,
                    $"{st.Note}\nArt cost ×{st.ArtCost:0.0} · looks good from Art {st.SkillFloor:0}{warning}{hardware}");
                if (art < st.SkillFloor) button.AddToClassList("choice-chip--risky");
                styleRow.Add(button);
            }
            styleCard.Add(styleRow);
            choices.Add(styleCard);
            Root.Add(choices);

            var idea0 = s.Ideas.First(i => i.Id == _ideaId);
            var genre = GameContent.Genre(idea0.Genre);
            var artStyle = GameContent.ArtStyle(_style);
            var plan = Sim.PlanProject(idea0, _scope, _style);
            double estimate = plan.TotalRequired * Rules.EstimateBias(s);
            double sessions = Sim.EstimatedSessions(plan);

            var summary = Ui.Card("Your estimate", "estimate");
            var head = Ui.Row("card-head");
            head.Add(Ui.Tile(Icons.Gamepad, "bg-teal icon-cream tile--big", 26f));
            var words = Ui.El("grow");
            words.Add(Ui.Display(idea0.Title, "card-head__title"));
            words.Add(Ui.Text($"{genre.Name} · {idea0.Theme} · {artStyle.Name} · {_scope}", "muted"));
            head.Add(words);
            summary.Add(head);
            summary.Add(Ui.Text($"\"{idea0.Hook}.\"", "quote"));
            var est = Ui.KeyValue("Work", $"about {sessions:0} Build sessions ({estimate:0.0} person-weeks)");
            Why(est, () => "A session is one morning, afternoon or evening of work (4 hours), played as a minigame. Your skills decide how much each one gets done.\n" +
                           "This is your honest guess, but first-time developers usually underestimate.");
            summary.Add(est);
            summary.Add(Ui.KeyValue("Features", plan.FeatureCount.ToString()));
            var features = Ui.Row("wrap");
            foreach (var w in plan.Workstreams.OrderByDescending(w => w.Required))
                foreach (var name in w.Features)
                    features.Add(Ui.Chip(name, "chip--skill " + Fmt.SkillClass(w.Discipline)));
            summary.Add(features);
            if (!string.IsNullOrEmpty(_error)) summary.Add(Ui.Text(_error, "bad"));
            var start = Ui.IconBtn(Icons.Rocket, "Start this project", StartProject, "btn btn--primary btn--big", 20f);
            start.name = "start-project";
            summary.Add(start);
            Root.Add(summary);
        }

        Button ChoiceButton(string text, bool selected, System.Action choose, string help)
        {
            var button = Ui.Btn(text, () => { choose(); Refresh(); }, "choice-chip" + (selected ? " choice-chip--on" : ""));
            Why(button, () => help);
            return button;
        }

        void StartProject()
        {
            string error = null;
            Studio.Do(() => Sim.StartProject(_ideaId, _scope, _style, out error));
            _error = error;
            if (error != null) Refresh();
        }

        // ------------------------------------------------------------------ active project

        void BuildActive(Project project)
        {
            var s = State;
            var genre = GameContent.Genre(project.Genre);
            var style = GameContent.ArtStyle(project.ArtStyle);
            bool ready = project.Status == ProjectStatus.ReadyToRelease;
            Header(project.Title, $"\"{project.Hook}.\"");
            BarNote = $"{genre.Name} · {project.Theme} · {style.Name} · {project.Scope}";

            if (_priceProjectId != project.Id)
            {
                _priceProjectId = project.Id;
                _price = Rules.FairPrice(project.Scope);
            }

            var top = Ui.Row("grid-row");
            Root.Add(top);

            var progress = Ui.Card("Features", "col-7");
            var head = Ui.Row("card-head");
            head.Add(Ui.Chip(ready ? "CONTENT COMPLETE" : "IN DEVELOPMENT", ready ? "chip--good" : "chip--teal"));
            head.Add(Ui.Text($"{project.FeaturesDone} of {project.FeatureCount} features built", "features__count"));
            head.Add(Ui.Grow());
            head.Add(Ui.Display(Fmt.Pct(project.Progress), "card-head__pct"));
            progress.Add(head);
            var bar = Ui.Bar(project.Progress, "fill-stripes", 18);
            bar.Q(className: "bar__fill").style.backgroundImage = new StyleBackground(Textures.Stripes);
            progress.Add(bar);

            bool canPlay = !Sim.IsNight && Sim.ForcedActivity() == null;
            var board = Ui.El("features");
            foreach (var w in project.Workstreams)
            {
                var lane = Ui.Row("features__lane");
                var label = Ui.Row("features__skill");
                label.Add(Ui.Tile(Fmt.SkillIcon(w.Discipline), "features__tile " + Fmt.SkillClass(w.Discipline), 14f));
                label.Add(Ui.Text(w.Discipline.ToString(), "features__skill-name"));
                var ws = w;
                Why(label, () => $"{ws.Discipline}: your skill {s.Founder.Get(ws.Discipline):0.0}, average task quality so far {ws.Quality:0.0}/10.");
                lane.Add(label);
                var chips = Ui.Row("features__chips");
                for (int i = 0; i < w.FeatureCount; i++)
                {
                    double p = w.FeatureProgress(i);
                    bool current = !w.IsDone && i == w.CurrentFeature;
                    var chip = Ui.Row("feature" + (p >= 1 ? " feature--done" : current ? " feature--current" : ""));
                    if (p >= 1) chip.Add(new Icon(Icons.Check, 12f, 3.2f));
                    chip.Add(Ui.Text(w.FeatureName(i), "feature__name"));
                    if (current && p > 0) chip.Add(Ui.Text(Fmt.Pct(p), "feature__pct"));
                    if (current)
                    {
                        var fill = Ui.El("feature__fill " + Fmt.SkillClass(w.Discipline));
                        fill.style.width = Length.Percent((float)(p * 100));
                        chip.Insert(0, fill);
                    }
                    chips.Add(chip);
                }
                lane.Add(chips);
                if (!w.IsDone && !ready)
                {
                    var discipline = w.Discipline;
                    var build = Ui.IconBtn(Icons.Plug, "Build", () => Studio.Play(PlanBlock.Build(discipline)), "btn btn--small features__build", 14f);
                    build.name = "build-" + discipline.ToString().ToLowerInvariant();
                    build.SetEnabled(canPlay);
                    lane.Add(build);
                }
                board.Add(lane);
            }
            progress.Add(board);
            progress.Add(Ui.Divider());
            double bias = Rules.EstimateBias(s);
            if (!ready)
            {
                var est = Ui.KeyValue("Estimated remaining", $"about {Sim.EstimatedSessionsRemaining(project):0} Build sessions");
                Why(est, () => s.ShippedGames == 0
                    ? "Your best guess. First projects usually run 30-40% over. You'll find out in the post-mortem."
                    : $"Adjusted by your track record. Estimates shown are about {Rules.Pct(bias)} of the real remaining work.");
                progress.Add(est);
                if (!project.BoughtAssetPack && project.Stream(Skill.Art)?.Remaining > 0)
                {
                    var pack = Command($"Buy an art asset pack ({Fmt.Money(Sim.AssetPackPrice(project))})", () => Sim.BuyAssetPack(project.Id, out _), "btn btn--small");
                    Why(pack, () => "Covers 40% of the remaining art at quality 6, maybe better than your own work.\nBut players have seen these assets before: Innovation -0.6.");
                    progress.Add(pack);
                }
            }
            else
            {
                progress.Add(Ui.Pill(Icons.Check, $"Every feature is built. Polish: +{Rules.PolishBonus(s, project) * 100:0}%, {project.BugsFixed:0} bugs squashed.", "pill--good pill--wrap"));
                var polish = Ui.IconBtn(Icons.Bug, "Polish: squash bugs (Bug Squash)", () => Studio.Play(PlanBlock.Develop), "btn btn--small", 16f);
                polish.name = "project-polish";
                polish.SetEnabled(canPlay);
                progress.Add(polish);
            }
            top.Add(progress);

            var side = Ui.El("col-5 stack");
            var look = Ui.Card("Your game, so far", "game-card");
            var preview = new GamePreview { name = "project-preview" };
            preview.Show(project);
            look.Add(preview);
            Why(look, () => PreviewHelp(project));
            side.Add(look);

            var quality = Ui.Card("How good is it? (your guess)");
            double price = _price > 0 ? _price : Rules.FairPrice(project.Scope);
            Sim.QualityEstimate(project, price, out double low, out double high);
            quality.Add(Ui.Display($"{Rules.Pct(low)} - {Rules.Pct(high)}", "big-number"));
            var range = Ui.El("range");
            var band = Ui.El("range__band");
            band.style.left = Length.Percent((float)(low * 100));
            band.style.width = Length.Percent((float)(System.Math.Max(0.02, high - low) * 100));
            range.Add(band);
            quality.Add(range);
            quality.Add(Ui.Text("Your guess at the quality players will see. Design and Production skills narrow the range.", "small muted"));
            double bugs = project.BugsRemaining;
            quality.Add(Ui.KeyValue("Bugs (estimate)", bugs < 1 ? "very few" : $"{bugs * 0.7:0}-{bugs * 1.3:0}", bugs > 15 ? "warn" : null));
            quality.Add(Ui.Text("The exact breakdown is revealed in the post-mortem, 30 days after launch.", "small muted"));
            side.Add(quality);
            top.Add(side);

            var bottom = Ui.Row("grid-row");
            Root.Add(bottom);
            bottom.Add(StoreCard(project));
            bottom.Add(LaunchCard(project));
        }

        VisualElement StoreCard(Project project)
        {
            var s = State;
            var store = Ui.Card("Store page", "col-5");
            if (!project.HasStorePage)
            {
                store.Add(Ui.Text("No store page yet. Wishlists only start once people can find your game.", "muted"));
                var scratch = Command("Open a scratch.io page (free)", () => Sim.OpenStorePage(project.Id, StorePlatform.ScratchIo, out _));
                Why(scratch, () => "Free and instant. Tiny audience, no wishlists, weekly payouts. Fine for a first tiny game.");
                store.Add(scratch);
                var vapor = Command($"Open a Vapor page ({Fmt.Money(Tuning.VaporStoreFee)})", () => Sim.OpenStorePage(project.Id, StorePlatform.Vapor, out _), "btn btn--primary");
                vapor.SetEnabled(project.Progress >= 0.25);
                Why(vapor, () => "The big PC store. Marketing turns into wishlists, and wishlists turn into launch sales.\n" +
                                 "Needs 25% progress (you need screenshots) and must be public 14 days before launch. Pays 30 days after each month.");
                store.Add(vapor);
                return store;
            }
            var head = Ui.Row("card-head");
            head.Add(Ui.Tile(project.Platform == StorePlatform.Vapor ? Icons.Star : Icons.Heart, "bg-blue tile--big", 24f));
            var words = Ui.El("grow");
            words.Add(Ui.Display(project.Platform == StorePlatform.Vapor ? "Vapor · Coming Soon" : "scratch.io", "card-head__title"));
            words.Add(Ui.Text($"Public since {GameCalendar.FormatShort(project.StorePageDay)} ({s.Day - project.StorePageDay} days)", "muted small"));
            head.Add(words);
            store.Add(head);
            if (project.Platform == StorePlatform.Vapor)
            {
                var line = Ui.Row("runway-line");
                line.Add(Ui.Display(Fmt.Num(project.Wishlists), "big-number"));
                line.Add(Ui.Text("wishlists", "big-number__unit"));
                Why(line, () => "Wishlists come from Marketing sessions (heavy-tailed: most posts do little, a few take off), from your followers, and from Next Fest.\nRoughly 10-15% of wishlists buy in the first month.");
                store.Add(line);
                store.Add(FestSection(project));
            }
            store.Add(Ui.KeyValue("Followers", Fmt.Num(s.Audience.Followers)));
            return store;
        }

        static string FestName(int festDay) => $"{GameCalendar.MonthNames[GameCalendar.Month(festDay) - 1]} Next Fest";

        /// <summary>Next Fest (Design/10 §4): sign up, the demo's progress, or how it went. Once per game.</summary>
        VisualElement FestSection(Project project)
        {
            var s = State;
            var box = Ui.El("fest");
            box.name = "project-fest";
            box.Add(Ui.Text("NEXT FEST", "card__title fest__title"));
            if (project.FestDone)
            {
                box.Add(Ui.Text($"+{Fmt.Num(project.FestWishlists)} wishlists from Next Fest" +
                                (project.DemoPlayers >= 1 ? $", and {Fmt.Num(project.DemoPlayers)} people played the demo." : "."), "small"));
            }
            else if (Sim.InFest(project))
            {
                box.Add(Ui.Text($"Happening now: day {s.Day - project.FestDay + 1} of {Tuning.FestDays}, +{Fmt.Num(project.FestWishlists)} wishlists so far.", "small"));
            }
            else if (project.FestDay >= 0)
            {
                int days = project.FestDay - s.Day;
                box.Add(Ui.Text($"Signed up for the {FestName(project.FestDay)} ({GameCalendar.FormatShort(project.FestDay)}), in {days} day{(days == 1 ? "" : "s")}.", "small"));
                var demo = Ui.MeterRow("Demo", project.DemoProgress, "fill-good", Fmt.Pct(project.DemoProgress));
                Why(demo, () => project.Progress < Tuning.DemoMinProgress
                    ? $"Build the game to {Fmt.Pct(Tuning.DemoMinProgress)} first: there has to be something to show."
                    : $"Make the demo from your day (Build your game → Make the demo). If the fest started today, the demo would look {Fmt.Pct(Sim.DemoQuality(project))} good.");
                box.Add(demo);
            }
            else
            {
                int next = GameSimulation.UpcomingFest(s.Day);
                box.Add(Ui.Text($"{FestName(next)}: {GameCalendar.FormatShort(next)} to {GameCalendar.FormatShort(next + Tuning.FestDays - 1)}. " +
                                $"Sign up by {GameCalendar.FormatShort(next - Tuning.FestSignUpDays)}.", "small"));
                var signUp = Command("Sign up for Next Fest (free)", () => Sim.SignUpForNextFest(project.Id, out _), "btn btn--small");
                signUp.name = "fest-signup";
                Why(signUp, () => "Once per game. For a week, players try demos and wishlist what they like: a finished, polished demo of a good game " +
                                  "brings hundreds of wishlists, a rough one fewer, and no demo hardly any. The demo takes about a week of Build sessions " +
                                  "that the game itself doesn't get.");
                box.Add(signUp);
            }
            return box;
        }

        VisualElement LaunchCard(Project project)
        {
            var s = State;
            var launch = Ui.Card("Launch", "col-7");
            if (project.Status != ProjectStatus.ReadyToRelease)
            {
                launch.Add(Ui.Text("Build every feature to launch. You can keep polishing after that.", "muted"));
            }
            else
            {
                var prices = Ui.Row("wrap");
                prices.Add(Ui.Text("Price", "templates__label"));
                foreach (var p in project.Scope == ProjectScope.Tiny ? TinyPrices : SmallPrices)
                {
                    var value = p;
                    prices.Add(Ui.Btn("$" + p.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), () => { _price = value; Refresh(); },
                        "choice-chip" + (System.Math.Abs(_price - p) < 0.001 ? " choice-chip--on" : "")));
                }
                launch.Add(prices);
                Sim.ForecastFirstMonth(project, _price, out double lowUnits, out double highUnits);
                double storeCut = project.Platform == StorePlatform.Vapor ? Tuning.VaporPlatformFee : Tuning.ScratchPlatformFee;
                double net = _price * (1 - Tuning.RefundRate) * (1 - storeCut);
                var forecast = Ui.KeyValue("First month (forecast)", $"{lowUnits:0}-{highUnits:0} copies · {Fmt.Money(lowUnits * net)}-{Fmt.Money(highUnits * net)} to you");
                Why(forecast, () => $"After refunds ({Rules.Pct(Tuning.RefundRate)}) and the store's cut ({Rules.Pct(storeCut)}), you keep about {Fmt.Money(net)} per copy.\n" +
                                    "Cheaper games sell more copies; players also judge value (content vs price).");
                launch.Add(forecast);
                int waitDays = project.Platform == StorePlatform.Vapor ? Tuning.VaporMinStorePageDays - (s.Day - project.StorePageDay) : 0;
                if (!project.HasStorePage) launch.Add(Ui.Pill(Icons.Warning, "Open a store page first.", "pill--warn"));
                else if (waitDays > 0) launch.Add(Ui.Pill(Icons.Clock, $"Vapor needs the page public for {waitDays} more days.", "pill--warn"));
                var release = Ui.IconBtn(Icons.Rocket, "Launch " + project.Title, () => Studio.Launch(project, _price), "btn btn--primary btn--big", 20f);
                release.name = "project-launch";
                release.SetEnabled(project.HasStorePage && waitDays <= 0);
                launch.Add(release);
            }
            launch.Add(Ui.Grow());
            var cancel = Command("Shelve this project", () => Sim.CancelProject(project.Id), "btn btn--danger btn--small");
            Why(cancel, () => "Stop working on it for good. Morale -8. Skills stay; the work doesn't.");
            launch.Add(cancel);
            return launch;
        }

        static string PreviewHelp(Project project)
        {
            if (project.Status == ProjectStatus.ReadyToRelease)
                return project.BugsRemaining >= 0.5 ? "Every feature is in. The glitches are bugs still hiding: polish squashes them." : "Every feature is in, and it runs clean.";
            return "Gray boxes become art as you build Art features; levels, enemies and pickups come with Design, movement and the HUD with " +
                   "Programming, music with Audio and dialogue with Writing. Glitches are bugs you haven't squashed yet.";
        }

        // ------------------------------------------------------------------ released games

        void BuildReleased()
        {
            var released = State.ReleasedProjects.OrderByDescending(p => p.ReleaseDay).ToList();
            if (released.Count == 0) return;
            Root.Add(Ui.Display("Released games", "section-title"));
            var row = Ui.Row("row--top wrap");
            foreach (var p in released)
            {
                var r = p.Result;
                var card = Ui.Card(null, "release-card");
                var head = Ui.Row("card-head");
                head.Add(Ui.Tile(Icons.Trophy, "bg-mustard tile--big", 24f));
                var words = Ui.El("grow");
                words.Add(Ui.Display(p.Title, "card-head__title"));
                words.Add(Ui.Text($"{(p.Platform == StorePlatform.Vapor ? "Vapor" : "scratch.io")} · ${p.Price:0.00} · launched {GameCalendar.FormatShort(p.ReleaseDay)}", "muted small"));
                head.Add(words);
                card.Add(head);
                card.Add(Ui.KeyValue("Copies sold", Fmt.Num(p.UnitsSold)));
                card.Add(Ui.KeyValue("Earned (after fees)", Fmt.Money(p.NetRevenue), "good"));
                card.Add(Ui.KeyValue("Reviews", r.ReviewCount < 10 ? $"{r.ReviewCount} (too few for a score)" : $"{r.ReviewLabel} · {Rules.Pct(r.PositiveShare)} of {r.ReviewCount}"));
                if (p.Platform == StorePlatform.Vapor) card.Add(Ui.KeyValue("Launch wishlists", Fmt.Num(r.LaunchWishlists)));
                int since = Sim.DaysSinceLaunch(p);
                int scoreHub = r.ScoreHub(since);
                var critics = r.CriticsBy(since).ToList();
                if (critics.Count > 0)
                {
                    var critic = Ui.KeyValue("Critics", scoreHub >= 0 ? $"ScoreHub {scoreHub} · {ReleaseResult.ScoreHubLabel(scoreHub).ToLowerInvariant()}"
                        : string.Join(" · ", critics.Select(c => $"{c.Outlet} {c.Score}")));
                    Why(critic, () => string.Join("\n", critics.Select(c => $"{c.Outlet}, {c.Score}: \"{c.Quote}\"")));
                    card.Add(critic);
                }
                if (r.Hotfixes > 0 || r.Answers > 0)
                    card.Add(Ui.KeyValue("After launch", $"{r.Hotfixes} hotfix{(r.Hotfixes == 1 ? "" : "es")} · {r.Answers} answer session{(r.Answers == 1 ? "" : "s")}"));
                if (p.PostMortemDay >= 0)
                {
                    var pillars = Ui.Row("wrap");
                    foreach (var x in r.Pillars) pillars.Add(Ui.Chip($"{x.Key} {x.Value:0.0}"));
                    pillars.Add(Ui.Chip($"Stability {r.Stability:0.0}"));
                    card.Add(pillars);
                }
                else
                    card.Add(Ui.Text($"Post-mortem on {GameCalendar.FormatShort(p.ReleaseDay + 30)}.", "muted small"));
                var reviews = ReviewWriter.For(p, 2);
                if (reviews.Count > 0)
                {
                    card.Add(Ui.Text("WHAT PLAYERS SAY", "card__title release-card__says"));
                    foreach (var review in reviews) card.Add(ReviewView.Card(review));
                }
                row.Add(card);
            }
            Root.Add(row);
        }
    }
}
