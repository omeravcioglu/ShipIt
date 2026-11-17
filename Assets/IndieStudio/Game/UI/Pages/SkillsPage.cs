using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>You: skills, your state, and what you've learned (Design/02 §6, Design/17 §3).</summary>
    public sealed class SkillsPage : Page
    {
        /// <summary>The skills that matter to a solo developer: the five crafts, and marketing.</summary>
        public static readonly Skill[] Shown = { Skill.Programming, Skill.Design, Skill.Art, Skill.Audio, Skill.Writing, Skill.Marketing };

        public SkillsPage(StudioUI studio) : base(studio) { }

        public override string Title => "You";
        public override string IconPath => Icons.Person;
        public override string AppClass => "app-you";

        public override void Refresh()
        {
            Root.Clear();
            var s = State;
            var f = s.Founder;
            Header("You", "Everything you do is practice: every session makes you better at its skill. Higher levels get more done, make better games and earn more from gigs.");
            BarNote = HousingLabel(f.Housing) + (f.Job == DayJob.None ? "" : f.Job == DayJob.PartTime ? " · part-time job" : " · full-time job");

            var top = Ui.Row("grid-row");
            Root.Add(top);

            var me = Ui.Card(null, "col-4 me-card");
            me.Add(Ui.Portrait(AvatarLook.Founder, Avatar.FounderMood(s), 150f, "bg-mustard-light me-card__portrait"));
            me.Add(Ui.Display("You", "me-card__name"));
            me.Add(Ui.Text(s.ShippedGames == 0 ? "Bedroom developer, no games shipped yet" : $"Bedroom developer, {s.ShippedGames} game{(s.ShippedGames == 1 ? "" : "s")} shipped", "muted"));
            me.Add(Ui.MeterRow("Morale", f.Morale, "fill-good"));
            me.Add(Ui.MeterRow("Burnout", f.Burnout, f.Burnout >= Tuning.VacationAdviceBurnout ? "fill-bad" : "fill-warn"));
            var productivity = Rules.Productivity(s);
            var prodRow = Ui.KeyValue("Working at", Fmt.Pct(productivity.Value));
            Why(prodRow, () => "How much each session gets done right now, compared to normal. Good spirits lift it; burnout, low morale and money worries drag it down.\n" + productivity.Describe("0.00"));
            me.Add(prodRow);
            if (f.IsResting(s.Day)) me.Add(Ui.Pill(Icons.Moon, "Resting until " + GameCalendar.FormatShort(System.Math.Max(f.ForcedRestUntilDay, f.VacationUntilDay)), "pill--ink"));
            top.Add(me);

            var skills = Ui.Card("Skills", "col-8");
            foreach (var skill in Shown)
            {
                double value = f.Get(skill);
                int level = f.Level(skill);
                double fraction = value - level;
                double sessionsLeft = System.Math.Max(1, System.Math.Ceiling(SkillInfo.HoursForLevel(level) * (1 - fraction) / Tuning.BlockHours));
                var line = Ui.Row("skill-row skill-row--craft");
                line.Add(Ui.Display(level.ToString(), "level " + Fmt.SkillClass(skill)));
                line.Add(Ui.Text(skill.ToString(), "skill-row__name"));
                line.Add(Ui.Bar(fraction, Fmt.SkillClass(skill), 12));
                line.Add(Ui.Text(level >= 10 ? "mastered" : $"~{sessionsLeft:0} sessions to level {level + 1}", "skill-row__next"));
                var sk = skill;
                Why(line, () => SkillHelp(sk, State));
                skills.Add(line);
            }
            if (f.CourseHoursLeft > 0) skills.Add(Ui.Pill(Icons.Graduation, $"Course in progress: {f.CourseSkill}, {f.CourseHoursLeft:0}h left (spend sessions on Learn · {f.CourseSkill}).", "pill--good pill--wrap"));
            top.Add(skills);

            var bottom = Ui.Row("grid-row");
            Root.Add(bottom);
            var lessons = Ui.Card("Lessons learned", "col-5");
            if (s.Lessons.Count == 0) lessons.Add(Ui.Empty("Ship a game: a month after launch, you look back and pick a lesson to keep."));
            foreach (var id in s.Lessons)
            {
                var lesson = LessonCatalog.Get(id);
                var row = Ui.Row("lesson");
                row.Add(Ui.Tile(Icons.Book, "bg-yellow", 18f));
                var words = Ui.El("grow");
                words.Add(Ui.Text(lesson.Title, "bold"));
                words.Add(Ui.Text(lesson.Effect, "small muted"));
                row.Add(words);
                lessons.Add(row);
            }
            if (s.Knowledge.Count > 0)
            {
                lessons.Add(Ui.Text("WHAT YOU'VE FOUND OUT", "card__title lessons__found"));
                foreach (var fact in s.Knowledge)
                {
                    var row = Ui.Row("lesson");
                    row.Add(Ui.Tile(Icons.Bulb, "bg-cyan", 18f));
                    row.Add(Ui.Text(fact, "grow small"));
                    lessons.Add(row);
                }
            }
            bottom.Add(lessons);
            bottom.Add(StoryCard(s));
        }

        /// <summary>"Your story so far": the studio's milestones, newest first, so you can see how far you've come.</summary>
        static VisualElement StoryCard(GameState s)
        {
            var story = Ui.Card("Your story so far", "col-7 story");
            story.name = "you-story";
            string games = s.ShippedGames == 1 ? "1 game shipped" : $"{s.ShippedGames} games shipped";
            int gigs = s.Freelance.Profile.CompletedGigs;
            story.Add(Ui.Text($"Day {s.Day + 1} of {s.StudioName} · {Fmt.Num(s.Stats.WorkBlocks)} sessions worked · {games} · {gigs} gig{(gigs == 1 ? "" : "s")} delivered", "small muted story__stats"));
            var entries = s.Story.AsEnumerable().Reverse().ToList();
            if (entries.Count == 0) entries.Add(new StoryEntry { Day = 0, Kind = StoryKind.Start, Text = "Quit your job to make games." }); // saves from before the story
            foreach (var entry in entries)
            {
                var row = Ui.Row("story__row");
                row.Add(Ui.Text(GameCalendar.FormatShort(entry.Day), "story__date"));
                var (icon, tile) = StoryIcon(entry.Kind);
                row.Add(Ui.Tile(icon, "story__tile " + tile, 14f));
                row.Add(Ui.Text(entry.Text, "story__text"));
                story.Add(row);
            }
            return story;
        }

        static (string, string) StoryIcon(StoryKind kind)
        {
            switch (kind)
            {
                case StoryKind.Gig: return (Icons.Briefcase, "act-freelance");
                case StoryKind.Project: return (Icons.Gamepad, "bg-teal icon-cream");
                case StoryKind.Feature: return (Icons.Plug, "act-develop");
                case StoryKind.Store: return (Icons.Bag, "bg-blue");
                case StoryKind.Launch: return (Icons.Rocket, "bg-orange");
                case StoryKind.Money: return (Icons.Bank, "bg-mustard-light");
                case StoryKind.Fans: return (Icons.Heart, "bg-pink");
                case StoryKind.Level: return (Icons.Graduation, "act-learn");
                case StoryKind.Chapter: return (Icons.Trophy, "bg-mustard");
                case StoryKind.Job: return (Icons.Coffee, "bg-cream");
                case StoryKind.Jam: return (Icons.Bolt, "bg-yellow");
                case StoryKind.Shop: return (Icons.Bag, "bg-lavender");
                case StoryKind.Setback: return (Icons.Warning, "bg-red-light");
                default: return (Icons.Home, "bg-peach");
            }
        }

        static string SkillHelp(Skill skill, GameState state)
        {
            double value = state.Founder.Get(skill);
            int level = state.Founder.Level(skill);
            if (skill.IsCraft())
                return $"{skill} level {level}: you work at about {Fmt.Pct(Rules.Efficiency(value))} of a seasoned pro's speed, and every level makes your {skill.ToString().ToLowerInvariant()} work faster and better.\n" +
                       $"Gigs in {skill} pay about {Fmt.Money(Rules.FreelanceRate(value, state.Freelance.Profile.Rating, state.Freelance.Profile.RateSetting).Value)} an hour.\n" +
                       "You practice it in Build, Gig and Learn sessions.";
            return $"Marketing level {level}: every post reaches about {(Rules.MarketingFactor(state) - 1) * 100:0}% more people than a beginner's. You practice it by posting about your game.";
        }

        public static string HousingLabel(Housing housing)
        {
            switch (housing)
            {
                case Housing.SharedFlat: return "Shared flat";
                case Housing.Parents: return "Your parents' house";
                default: return "Tiny apartment";
            }
        }
    }
}
