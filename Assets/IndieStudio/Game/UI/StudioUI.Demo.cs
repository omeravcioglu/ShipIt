using System;
using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// The free demo (DemoRules) and the how-to-play guide. The demo opens with a welcome card, keeps some apps closed
    /// (they say what they do in the full game), and ends with a card that sums up your first two weeks: launching
    /// your first game ends it, and so does running out of days. From there you can wishlist the full game, play the
    /// demo again or quit.
    /// </summary>
    public sealed partial class StudioUI
    {
        /// <summary>Apps the demo keeps closed, and what each one does in the full game.</summary>
        static readonly Dictionary<Type, string> FullGameApps = new Dictionary<Type, string>
        {
            { typeof(DashboardPage), "Your week at a glance: the runway chart, your audience, and how your games are selling." },
            { typeof(IdeasPage), "Research new game ideas, and get a sharper read on the ones you have." },
            { typeof(FinancePage), "Bills on the 1st, payouts, monthly reports and your cash forecast." },
            { typeof(SkillsPage), "Your skills and levels, and what to study next." },
            { typeof(ShopPage), "Upgrades for your bedroom studio: a gaming PC, a proper chair, pro art tools." },
        };

        bool _demoEndShown;

        public bool IsDemo => Sim.State.IsDemo;

        /// <summary>An app the demo keeps closed.</summary>
        public bool IsLocked(Page page) => page != null && Sim.State.IsDemo && FullGameApps.ContainsKey(page.GetType());

        /// <summary>What a closed app does in the full game (its dock tooltip).</summary>
        public string FullGameNote(Page page) => FullGameApps.TryGetValue(page.GetType(), out string note) ? note : null;

        void ShowLocked(Page page)
        {
            _hud.CloseMenus();
            Toast(Icons.Lock, "bg-mustard-light", $"{page.Title} comes with the full game", FullGameNote(page));
        }

        /// <summary>"3 days left", "1 day left" (the demo's calendar).</summary>
        public static string DemoDaysLeftText(int day)
        {
            int left = DemoRules.DaysLeft(day);
            return left == 1 ? "1 day left" : $"{left} days left";
        }

        // ------------------------------------------------------------------ the welcome

        /// <summary>The demo's first screen: what it is, how long it takes, and the way into the how-to-play guide.</summary>
        public void ShowDemoIntro()
        {
            _hud.CloseMenus();
            string hello = PlayerName != null ? $"Hi {PlayerName}! " : "";
            var modal = OpenSystemModal(MessageKind.Info, Doug(Mood.Happy), "Welcome to the Ship It! demo",
                $"{hello}You've quit your job to make games. You have $5,000, a tired laptop, a cat, and two weeks.\n" +
                "Your goal: ship your very first game. It takes about 15 to 20 minutes, and I'll show you everything as we go.");
            modal.parent.name = "demo-intro";

            var steps = Ui.Row("demo-steps");
            steps.Add(DemoStep("1", Icons.Briefcase, "act-freelance", "Take a gig", "Your friend Sam has a small job for you. It pays the rent."));
            steps.Add(DemoStep("2", Icons.Plug, "act-develop", "Build your game", "Pick an idea and wire up its features, one session at a time."));
            steps.Add(DemoStep("3", Icons.Rocket, "bg-orange", "Ship it!", "Open a store page, pick a price and launch it to real players."));
            modal.Add(steps);

            AddChoices(modal,
                ("Let's go!", "Doug walks you through your first day.", true, () => CloseSystemModal()),
                ("How to play", "The whole game on one page.", false, ShowHowToPlay));
        }

        static VisualElement DemoStep(string number, string icon, string tileClass, string title, string text)
        {
            var card = Ui.El("demo-step");
            var head = Ui.Row("demo-step__head");
            head.Add(Ui.Text(number, "demo-step__number"));
            head.Add(Ui.Tile(icon, "demo-step__tile " + tileClass, 18f));
            card.Add(head);
            card.Add(Ui.Display(title, "demo-step__title"));
            card.Add(Ui.Text(text, "demo-step__text"));
            return card;
        }

        // ------------------------------------------------------------------ how to play

        /// <summary>The rules on one page (the menu's "How to play", and the demo's welcome).</summary>
        public void ShowHowToPlay()
        {
            if (_minigames.IsOpen || _modalMessage != null || _launch != null) return;
            _hud.CloseMenus();
            if (_systemModalOpen) CloseSystemModal(false); // from the demo's welcome
            var modal = OpenSystemModal(MessageKind.Info, Doug(Mood.Happy), "How to play",
                IsDemo
                    ? "Everything you need for your first two weeks. Open this again any time from the menu (☰, top right)."
                    : "The basics of running your studio. Open this again any time from the menu (☰, top right).");
            modal.parent.name = "how-to-play";
            modal.AddToClassList("modal--howto");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("howto");
            var cards = new[]
            {
                HowToCard(Icons.Sunrise, "bg-yellow", "Your day",
                    "Every day has a morning, an afternoon and an evening. Choose what to do with each one in the panel on the left. " +
                    "Time only moves when you choose, so there's never a rush. When the evening is done, go to sleep."),
                HowToCard(Icons.Plug, "act-develop", "Build your game",
                    "A game is a list of features. Each Build session is Wire It Up: click tiles to turn them until the flow reaches every module. " +
                    "The better you play, the more the session gets done."),
                HowToCard(Icons.Briefcase, "act-freelance", "Gigs pay the rent",
                    "Accept freelance gigs in GigGrid and work on them from your day. Each kind of gig is its own little game: " +
                    "squash bugs, match pixels, fix typos, tune a synth, sort feedback. The client pays a few days after you deliver."),
                HowToCard(Icons.Moon, "act-rest", "Stay fresh",
                    "More than 45 hours of work in a week builds burnout, which slows you down. Rest an evening when you need it. " +
                    "Brew a coffee from the mug for a boost, and pet the cat when you're feeling low."),
                HowToCard(Icons.Rocket, "bg-orange", "Ship it",
                    "When every feature is built, open a store page in the Project app, pick a price and launch. " +
                    "Then watch the first copies sell and the first players react."),
                HowToCard(Icons.Target, "bg-teal", "Your goal",
                    IsDemo
                        ? "The goal at the top right always shows what's next: click it for the steps. In this demo you have two weeks to ship your first game. Doug points the way."
                        : "The goal at the top right always shows your current chapter: click it for the steps. Side goals run alongside, and every number has a \"Why?\" when you hover it."),
            };
            for (int i = 0; i < cards.Length; i += 3)
            {
                var row = Ui.El("howto__row");
                foreach (var card in cards.Skip(i).Take(3)) row.Add(card);
                scroll.Add(row);
            }
            modal.Add(scroll);
            AddChoices(modal, ("Got it", null, true, () => CloseSystemModal()));
        }

        static VisualElement HowToCard(string icon, string tileClass, string title, string text)
        {
            var card = Ui.Row("howto__card");
            card.Add(Ui.Tile(icon, "howto__tile " + tileClass, 22f));
            var words = Ui.El("howto__words");
            words.Add(Ui.Display(title, "howto__title"));
            words.Add(Ui.Text(text, "howto__text"));
            card.Add(words);
            return card;
        }

        // ------------------------------------------------------------------ the end of the demo

        /// <summary>
        /// The demo's last screen: what you made in your two weeks, what the full game adds, and the way on. It stays
        /// until you choose to play again or quit (wishlisting keeps it open).
        /// </summary>
        public void ShowDemoEnd()
        {
            _demoEndShown = true;
            _minigames.Close(false);
            if (!_current.IsHome) Show(_pages[0]); // the room behind the card: the poster, and the trophy once you've shipped
            _hud.CloseMenus();
            var s = Sim.State;
            bool shipped = s.DemoEnd == DemoEnd.Shipped;
            var game = s.ReleasedProjects.OrderByDescending(p => p.ReleaseDay).FirstOrDefault() ?? s.ActiveProject ?? s.Projects.LastOrDefault();
            string thanks = PlayerName != null ? $"Thanks for playing, {PlayerName}! " : "Thanks for playing! ";
            string title = shipped ? "That's the end of the demo!" : "Your two weeks are up!";
            string body = shipped
                ? $"{thanks}{s.StudioName} shipped its very first game, and most people who dream about this never get that far. I hope you liked it!"
                : $"{thanks}That's the end of the demo{(game != null ? $": {game.Title} got to {Fmt.Pct(game.Progress)}" : "")}. I hope you liked it! " +
                  "Play it again and ship your game, or keep going in the full game.";
            var modal = OpenSystemModal(MessageKind.Info, Doug(shipped ? Mood.Ecstatic : Mood.Happy), title, body);
            modal.parent.name = "demo-end";
            modal.AddToClassList("modal--demo-end");

            modal.Add(DemoSummary(s, game, shipped));
            modal.Add(FullGameTeaser(s));

            var choices = new List<(string, string, bool, Action)>
            {
                ("Wishlist Ship It! on Steam", "Hear the moment the full game is out.", true, () => GameEdition.OpenFullGameStorePage(_game.Platform)),
                ("Play the demo again", "A new seed, a new bedroom, $5,000.", false, () =>
                {
                    CloseSystemModal(false);
                    _game.NewGame((ulong)DateTime.Now.Ticks);
                }),
            };
            if (!Application.isEditor)
                choices.Add(("Quit", null, false, () =>
                {
                    _game.SaveNow();
                    Application.Quit();
                }));
            AddChoices(modal, choices.ToArray());
            _coach?.Refresh();
        }

        VisualElement DemoSummary(GameState s, Project game, bool shipped)
        {
            var stats = Ui.Row("demo-end__stats");
            stats.name = "demo-end-stats";
            if (shipped && game?.Result != null)
            {
                stats.Add(DemoStat(game.Title, $"{GameContent.Genre(game.Genre).Name} · ${game.Price:0.00}", true));
                stats.Add(DemoStat(Fmt.Num(Math.Floor(GameSimulation.ExpectedLaunchDayUnits(game))), "copies on day one")); // as the launch show counted them
                stats.Add(DemoStat($"{game.FeaturesDone}", "features built"));
            }
            else
            {
                stats.Add(DemoStat(game?.Title ?? "No game yet", game != null ? $"{Fmt.Pct(game.Progress)} built" : "Start one next time!", true));
                stats.Add(DemoStat(game != null ? $"{game.FeaturesDone}/{game.FeatureCount}" : "0", "features built"));
            }
            stats.Add(DemoStat(s.Freelance.Profile.CompletedGigs.ToString(), s.Freelance.Profile.CompletedGigs == 1 ? "gig delivered" : "gigs delivered"));
            stats.Add(DemoStat(Math.Min(s.Day + 1, DemoRules.Days).ToString(), "days in the studio"));
            return stats;
        }

        static VisualElement DemoStat(string value, string label, bool wide = false)
        {
            var box = Ui.El("demo-end__stat" + (wide ? " demo-end__stat--wide" : ""));
            box.Add(Ui.Display(value, "demo-end__value"));
            box.Add(Ui.Text(label, "demo-end__label"));
            return box;
        }

        static VisualElement FullGameTeaser(GameState s)
        {
            var box = Ui.El("demo-end__more");
            box.Add(Ui.Text($"IN THE FULL GAME, {s.StudioName.ToUpperInvariant()} KEEPS GOING", "demo-end__kicker"));
            var chips = Ui.Row("wrap demo-end__chips");
            foreach (var line in new[]
                     {
                         "Launch week: reviews, critics, hotfixes",
                         "Vapor store pages and wishlists",
                         "Next Fest with a demo of your own",
                         "Small games: a real indie launch",
                         "Marketing, research and new skills",
                         "Upgrades for your bedroom studio",
                         "Four chapters, from a bedroom to full-time indie",
                     })
                chips.Add(Ui.Chip(line, "chip--teal demo-end__chip"));
            box.Add(chips);
            return box;
        }
    }
}
