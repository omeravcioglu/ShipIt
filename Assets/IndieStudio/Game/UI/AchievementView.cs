using System.Collections.Generic;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>How achievements look: a badge per achievement (its icon on its color, gray while locked) and a list row.</summary>
    public static class AchievementView
    {
        static readonly Dictionary<string, (string icon, string color)> Looks = new Dictionary<string, (string, string)>
        {
            { "ACH_ITS_OFFICIAL", (Icons.Pen, "bg-peach") },
            { "ACH_RENT_MONEY", (Icons.Briefcase, "bg-yellow") },
            { "ACH_IT_COMPILES", (Icons.Code, "bg-blue") },
            { "ACH_SHIP_IT", (Icons.Rocket, "bg-orange") },
            { "ACH_SOMEONE_BOUGHT_IT", (Icons.Bag, "bg-green") },
            { "ACH_COMING_SOON", (Icons.Calendar, "bg-cyan") },
            { "ACH_A_REAL_INDIE_GAME", (Icons.Gamepad, "bg-teal-soft") },
            { "ACH_WISHLISTED", (Icons.Bell, "bg-lavender") },
            { "ACH_FOUR_DIGITS", (Icons.Chart, "bg-sage-light") },
            { "ACH_VERY_POSITIVE", (Icons.ThumbUp, "bg-pink") },
            { "ACH_FULL_TIME_INDIE", (Icons.Trophy, "bg-mustard") },
            { "ACH_BRILLIANT", (Icons.Star, "bg-yellow") },
            { "ACH_PERFECTIONIST", (Icons.Target, "bg-lavender") },
            { "ACH_IN_THE_ZONE", (Icons.Flame, "bg-orange") },
            { "ACH_EXPERT", (Icons.Graduation, "bg-green") },
            { "ACH_FIVE_STARS", (Icons.Chat, "bg-yellow") },
            { "ACH_72_HOURS", (Icons.Bolt, "bg-blue") },
            { "ACH_DOUBLE_SHOT", (Icons.Coffee, "bg-peach") },
            { "ACH_CAT_PERSON", (Icons.Heart, "bg-pink") },
            { "ACH_SELF_CARE", (Icons.Sun, "bg-yellow") },
            { "ACH_BACK_TO_INDIE", (Icons.Home, "bg-teal-soft") },
            { "ACH_BACK_FROM_THE_BRINK", (Icons.ArrowUp, "bg-green") },
            { "ACH_THE_HARD_WAY", (Icons.Bed, "bg-lavender") },
            { "ACH_KILL_YOUR_DARLINGS", (Icons.Close, "bg-red-light") },
        };

        public static (string icon, string color) Look(string id) => Looks.TryGetValue(id, out var look) ? look : (Icons.Trophy, "bg-mustard");

        /// <summary>The achievement's badge: its icon on its color, gray while locked (a padlock while it's still a secret).</summary>
        public static VisualElement Badge(AchievementDef achievement, bool unlocked, float iconSize = 22f, string classes = "")
        {
            var (icon, color) = Look(achievement.Id);
            bool secret = achievement.Hidden && !unlocked;
            return Ui.Tile(secret ? Icons.Lock : icon, "ach-badge " + (unlocked ? color : "ach-badge--locked") + " " + classes, iconSize);
        }

        /// <summary>
        /// The achievement's icon for Steam (Steam/AchievementIcons): a square 64 × 64 design, its icon in an ink frame
        /// on its color, or in gray for the "unachieved" version (a padlock for hidden ones).
        /// </summary>
        public static VisualElement Art(AchievementDef achievement, bool unlocked)
        {
            var (icon, color) = Look(achievement.Id);
            var art = Ui.El("ach-art " + (unlocked ? color : "ach-art--locked"));
            var frame = Ui.El("ach-art__frame");
            frame.Add(new Icon(achievement.Hidden && !unlocked ? Icons.Lock : icon, 34f, 2.8f));
            art.Add(frame);
            return art;
        }

        /// <summary>One achievement in the list: badge, name, what to do, and progress for counters.</summary>
        public static VisualElement Row(AchievementDef achievement, bool unlocked, GameState state)
        {
            bool secret = achievement.Hidden && !unlocked;
            var row = Ui.Row("ach-row" + (unlocked ? " ach-row--unlocked" : ""));
            row.Add(Badge(achievement, unlocked));
            var words = Ui.El("ach-row__words");
            words.Add(Ui.Text(secret ? "Hidden achievement" : achievement.Title, "ach-row__title"));
            words.Add(Ui.Text(secret ? "Keep playing to find out." : achievement.Description, "ach-row__text"));
            if (!unlocked && achievement.Progress != null && state != null)
            {
                double progress = System.Math.Min(achievement.Target, achievement.Progress(state));
                var line = Ui.Row("ach-row__progress");
                var bar = Ui.Bar(progress / achievement.Target, "fill-good", 8);
                bar.AddToClassList("ach-row__bar");
                line.Add(bar);
                line.Add(Ui.Text($"{Fmt.Num(System.Math.Floor(progress))}/{Fmt.Num(achievement.Target)}", "ach-row__count"));
                words.Add(line);
            }
            row.Add(words);
            if (unlocked) row.Add(new Icon(Icons.Check, 18f, 3f) { name = "ach-check" });
            return row;
        }
    }
}
