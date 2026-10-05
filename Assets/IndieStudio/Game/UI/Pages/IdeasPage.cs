using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>The Idea Notebook (Design/04). Values are shown as ranges that narrow with research.</summary>
    public sealed class IdeasPage : Page
    {
        public IdeasPage(StudioUI studio) : base(studio) { }

        public override string Title => "Ideas";
        public override string IconPath => Icons.Bulb;
        public override string AppClass => "app-ideas";

        public override void Refresh()
        {
            Root.Clear();
            Header("Idea Notebook", "Ideas are cheap; development is expensive. Research sessions bring new ideas and sharpen your read on the ones you have. Game jams produce jam-proven ideas.");
            var unused = State.UnusedIdeas.ToList();
            BarNote = unused.Count == 1 ? "1 idea" : $"{unused.Count} ideas";
            var row = Ui.Row("row--top wrap");
            foreach (var idea in unused)
            {
                var card = IdeaCardView(idea, false);
                int id = idea.Id;
                var develop = Ui.IconBtn(Icons.Rocket, "Develop this", () =>
                {
                    Studio.Page<ProjectPage>().SelectIdea(id);
                    Studio.Show<ProjectPage>();
                }, "btn btn--primary btn--small", 16f);
                develop.SetEnabled(State.ActiveProject == null);
                if (State.ActiveProject != null) Why(develop, () => "Finish or shelve your current project first.");
                card.Add(develop);
                row.Add(card);
            }
            Root.Add(row);
            if (unused.Count == 0) Root.Add(Ui.Empty("No unused ideas. Spend a session or two on Research."));

            var used = State.Ideas.Where(i => i.Used).ToList();
            if (used.Count > 0)
            {
                Root.Add(Ui.Display("Already developed", "section-title"));
                var chips = Ui.Row("wrap");
                foreach (var idea in used) chips.Add(Ui.Chip($"{idea.Title} · {GameContent.Genre(idea.Genre).Name}"));
                Root.Add(chips);
            }
        }

        /// <summary>An idea as an index card: genre band, title, hook, traits and the fun range.</summary>
        public static VisualElement IdeaCardView(IdeaCard idea, bool selected)
        {
            var genre = GameContent.Genre(idea.Genre);
            var card = Ui.Card(null, "idea-card" + (selected ? " card--selected" : ""));
            var band = Ui.Row("idea-card__band genre-" + idea.Genre.ToString().ToLowerInvariant());
            band.Add(Ui.Text(genre.Name.ToUpperInvariant(), "idea-card__genre"));
            band.Add(Ui.Grow());
            if (idea.JamProven) band.Add(Ui.Chip("JAM-PROVEN", "chip--ink"));
            card.Add(band);
            card.Add(Ui.Display(idea.Title, "idea-card__title"));
            card.Add(Ui.Text(idea.Theme, "muted small"));
            card.Add(Ui.Text($"\"{idea.Hook}.\"", "quote"));
            if (idea.Traits.Count > 0)
            {
                var chips = Ui.Row("wrap");
                foreach (var trait in idea.Traits) chips.Add(Ui.Chip(trait));
                card.Add(chips);
            }
            GameSimulation.FunEstimate(idea, out double low, out double high);
            var fun = Ui.Row("idea-card__fun");
            fun.Add(Ui.Text("Fun potential", "kv__key"));
            fun.Add(Ui.Text(idea.Knowledge == 0 ? "?" : $"{Rules.Pct(low)} - {Rules.Pct(high)}", "kv__value" + (idea.Knowledge >= 2 ? " accent" : "")));
            card.Add(fun);
            var range = Ui.El("range range--small");
            if (idea.Knowledge > 0)
            {
                var bandFill = Ui.El("range__band");
                bandFill.style.left = Length.Percent((float)(low * 100));
                bandFill.style.width = Length.Percent((float)(System.Math.Max(0.02, high - low) * 100));
                range.Add(bandFill);
            }
            else range.AddToClassList("range--unknown");
            card.Add(range);
            var read = Ui.Row("idea-card__read");
            read.Add(Ui.Text("Your read", "kv__key"));
            var dots = Ui.Row("dots");
            for (int i = 0; i < 3; i++) dots.Add(Ui.El("dot" + (i < idea.Knowledge ? " dot--on" : "")));
            read.Add(dots);
            string[] confidence = { "unknown", "rough", "researched", "proven" };
            read.Add(Ui.Text(confidence[System.Math.Min(3, idea.Knowledge)], "small muted"));
            card.Add(read);
            card.Add(Ui.KeyValue("Audience", MarketLabel(genre.Market)));
            card.Add(Ui.Text($"{idea.Source} · {GameCalendar.FormatShort(idea.CreatedDay)}", "small muted"));
            return card;
        }

        static string MarketLabel(double market) => market >= 4 ? "big" : market >= 3 ? "medium" : "niche";
    }
}
