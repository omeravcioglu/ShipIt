using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>You: skills, your state, and what you've learned (Design/02 §6, Design/17 §3).</summary>
    public sealed class SkillsPage : Page
    {
        public SkillsPage(StudioUI studio) : base(studio) { }

        public override string Title => "You";
        public override string IconPath => Icons.Person;
        public override string AppClass => "app-you";

        public override void Refresh()
        {
            Root.Clear();
            var s = State;
            var f = s.Founder;
            Header("You", "Skills grow by doing: every hour of practice counts. Level L to L+1 takes 70 × L² hours, so nobody masters everything.");
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
            var prodRow = Ui.KeyValue("Output multiplier", "×" + productivity.Value.ToString("0.00"));
            Why(prodRow, () => productivity.Describe("0.00"));
            me.Add(prodRow);
            if (f.IsResting(s.Day)) me.Add(Ui.Pill(Icons.Moon, "Resting until " + GameCalendar.FormatShort(System.Math.Max(f.ForcedRestUntilDay, f.VacationUntilDay)), "pill--ink"));
            top.Add(me);

            var skills = Ui.Card("Skills", "col-8");
            foreach (var skill in SkillInfo.All)
            {
                double value = f.Get(skill);
                int level = f.Level(skill);
                double fraction = value - level;
                double hoursLeft = SkillInfo.HoursForLevel(level) * (1 - fraction);
                var line = Ui.Row("skill-row" + (skill.IsCraft() ? " skill-row--craft" : ""));
                line.Add(Ui.Display(level.ToString(), "level " + Fmt.SkillClass(skill)));
                line.Add(Ui.Text(skill.ToString(), "skill-row__name"));
                line.Add(Ui.Bar(fraction, Fmt.SkillClass(skill), 12));
                line.Add(Ui.Text(level >= 10 ? "max" : $"{hoursLeft:0}h to Lv {level + 1}", "skill-row__next"));
                var sk = skill;
                Why(line, () => SkillHelp(sk, State));
                skills.Add(line);
            }
            if (f.CourseHoursLeft > 0) skills.Add(Ui.Pill(Icons.Graduation, $"Course in progress: {f.CourseSkill}, {f.CourseHoursLeft:0}h left (spend sessions on Learn · {f.CourseSkill}).", "pill--good pill--wrap"));
            top.Add(skills);

            var bottom = Ui.Row("grid-row");
            Root.Add(bottom);
            var lessons = Ui.Card("Lessons learned", "col-6");
            if (s.Lessons.Count == 0) lessons.Add(Ui.Empty("Ship a game: the post-mortem turns results, good or bad, into lessons."));
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
            bottom.Add(lessons);

            var knowledge = Ui.Card("Knowledge", "col-6");
            if (s.Knowledge.Count == 0) knowledge.Add(Ui.Empty("What you discover about the market ends up here."));
            foreach (var fact in s.Knowledge)
            {
                var row = Ui.Row("lesson");
                row.Add(Ui.Tile(Icons.Bulb, "bg-cyan", 18f));
                row.Add(Ui.Text(fact, "grow small"));
                knowledge.Add(row);
            }
            var stats = s.Stats;
            knowledge.Add(Ui.Divider());
            knowledge.Add(Ui.KeyValue("Sessions worked", Fmt.Num(stats.WorkBlocks)));
            knowledge.Add(Ui.KeyValue("Days off · collapses", $"{stats.VacationDays} · {stats.Collapses}"));
            knowledge.Add(Ui.KeyValue("Weeks spent worrying about money", Fmt.Num(stats.AnxiousWeeks)));
            bottom.Add(knowledge);
        }

        static string SkillHelp(Skill skill, GameState state)
        {
            double value = state.Founder.Get(skill);
            if (skill.IsCraft())
                return $"{skill} {value:0.00}: work efficiency ×{Rules.Efficiency(value):0.00} (1.00 at skill 5) and better task quality.\n" +
                       $"Gigs in {skill} pay about {Fmt.Money(Rules.FreelanceRate(value, state.Freelance.Profile.Rating, state.Freelance.Profile.RateSetting).Value)}/h.\n" +
                       "Trained by Build, Gig and Learn sessions.";
            switch (skill)
            {
                case Skill.Marketing: return $"Each Marketing session reaches about {(Rules.MarketingFactor(state) - 1) * 100:+0;-0}% more people than at level 1. Trained by Marketing sessions.";
                case Skill.Production: return $"Makes your estimates more honest (now about {Rules.Pct(Rules.EstimateBias(state))} of the real work). Trained by shipping (post-mortems).";
                case Skill.Business: return "Trained by prospecting for gigs. Business perks (grants, deals) arrive in later milestones.";
                default: return $"{skill} matters once you have a team and partners (later milestones).";
            }
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
