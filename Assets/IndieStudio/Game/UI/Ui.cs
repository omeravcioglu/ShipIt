using System;
using System.Globalization;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>Small factory helpers so pages read like layout, not like plumbing.</summary>
    public static class Ui
    {
        public static VisualElement El(string classes = null)
        {
            var e = new VisualElement();
            AddClasses(e, classes);
            return e;
        }

        public static Label Text(string text, string classes = null)
        {
            var label = new Label(text);
            AddClasses(label, classes);
            return label;
        }

        /// <summary>A label in the chunky display font (titles and big numbers).</summary>
        public static Label Display(string text, string classes = null) => Text(text, "display " + classes);

        /// <summary>
        /// A clickable element without Unity's default button styling, so Styles.uss alone decides how it
        /// looks in every state (hover, pressed, focused, disabled).
        /// </summary>
        public static Button Clickable(Action onClick, string classes)
        {
            var button = new Button(onClick);
            button.RemoveFromClassList(Button.ussClassName);
            AddClasses(button, classes);
            return button;
        }

        public static Button Btn(string text, Action onClick, string classes = "btn")
        {
            var button = Clickable(onClick, classes);
            button.text = text;
            return button;
        }

        /// <summary>A button with a line icon in front of its label (or only the icon when text is empty).</summary>
        public static Button IconBtn(string icon, string text, Action onClick, string classes = "btn", float iconSize = 18f)
        {
            var button = Clickable(onClick, classes);
            button.AddToClassList("btn--icon");
            button.Add(new Icon(icon, iconSize));
            if (!string.IsNullOrEmpty(text)) button.Add(Text(text, "btn__label"));
            return button;
        }

        public static VisualElement Row(string classes = null) => El("row " + classes);

        /// <summary>A sticker panel: cream paper, a thick ink outline and a hard shadow.</summary>
        public static VisualElement Card(string title, string classes = null)
        {
            var card = El("card " + classes);
            if (!string.IsNullOrEmpty(title)) card.Add(Text(title.ToUpperInvariant(), "card__title"));
            return card;
        }

        /// <summary>A rounded tile holding a line icon, like an app icon.</summary>
        public static VisualElement Tile(string icon, string classes = null, float iconSize = 22f)
        {
            var tile = El("tile " + classes);
            tile.Add(new Icon(icon, iconSize));
            return tile;
        }

        /// <summary>A rounded tile with a character in it, cropped to head and shoulders.</summary>
        public static VisualElement Portrait(AvatarLook look, Mood mood, float avatarSize, string classes = null)
        {
            var tile = El("portrait " + classes);
            tile.style.width = avatarSize + 10f;
            tile.style.height = avatarSize + 6f;
            tile.Add(new Avatar(look, mood, avatarSize));
            return tile;
        }

        public static VisualElement Bar(double fraction, string fillClasses = "fill-accent", float height = 14)
        {
            var bar = El("bar");
            bar.style.height = height;
            var fill = El("bar__fill " + fillClasses);
            SetFill(fill, fraction);
            bar.Add(fill);
            return bar;
        }

        public static VisualElement Meter(double fraction, string fillClasses)
        {
            var meter = El("meter");
            var fill = El("bar__fill " + fillClasses);
            SetFill(fill, fraction);
            meter.Add(fill);
            return meter;
        }

        public static void SetFill(VisualElement fill, double fraction) =>
            fill.style.width = Length.Percent((float)(Math.Max(0, Math.Min(1, fraction)) * 100));

        /// <summary>A labelled meter row: "Morale ▮▮▮▮▯ 62".</summary>
        public static VisualElement MeterRow(string label, double value, string fillClasses, string valueText = null)
        {
            var row = El("meter-row");
            row.Add(Text(label, "meter-row__label"));
            row.Add(Bar(value / 100.0, fillClasses, 12));
            row.Add(Text(valueText ?? value.ToString("0", CultureInfo.InvariantCulture), "meter-row__value"));
            return row;
        }

        /// <summary>Five chunky segments, like a battery (the runway meter).</summary>
        public static VisualElement Segments(int filled, int total, string fillClass)
        {
            var row = El("segments");
            for (int i = 0; i < total; i++) row.Add(El("segment" + (i < filled ? " " + fillClass : "")));
            return row;
        }

        public static VisualElement KeyValue(string key, string value, string valueClasses = null)
        {
            var row = El("kv");
            row.Add(Text(key, "kv__key"));
            row.Add(Text(value, "kv__value " + valueClasses));
            return row;
        }

        /// <summary>A receipt line: "Rent ............ $850".</summary>
        public static VisualElement Leader(string key, string value, string valueClasses = null)
        {
            var row = El("leader");
            row.Add(Text(key, "leader__key"));
            row.Add(El("leader__dots"));
            row.Add(Text(value, "leader__value " + valueClasses));
            return row;
        }

        /// <summary>A small caption over a big number.</summary>
        public static VisualElement Stat(string caption, string value, string classes = null)
        {
            var stat = El("stat " + classes);
            stat.Add(Text(caption.ToUpperInvariant(), "stat__caption"));
            stat.Add(Display(value, "stat__value"));
            return stat;
        }

        public static Label Chip(string text, string classes = null) => Text(text, "chip " + classes);

        /// <summary>A chip with a small icon (status, deadlines).</summary>
        public static VisualElement Pill(string icon, string text, string classes = null)
        {
            var pill = El("pill " + classes);
            pill.Add(new Icon(icon, 13f, 2.8f));
            pill.Add(Text(text, "pill__text"));
            return pill;
        }

        public static VisualElement Divider() => El("divider");

        public static Label Empty(string text) => Text(text, "empty");

        public static VisualElement Grow() => El("grow");

        public static void AddClasses(VisualElement element, string classes)
        {
            if (string.IsNullOrEmpty(classes)) return;
            foreach (var c in classes.Split(' '))
                if (c.Length > 0) element.AddToClassList(c);
        }
    }

    /// <summary>Formatting and color-class helpers shared by the pages.</summary>
    public static class Fmt
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static string Money(double amount) => Rules.Money(amount);

        /// <summary>"+$310" or "-$2,450" (plain ASCII, so every font has the glyphs).</summary>
        public static string SignedMoney(double amount) => (amount >= 0 ? "+" : "-") + Rules.Money(Math.Abs(amount));

        public static string Pct(double fraction) => (fraction * 100).ToString("0", Culture) + "%";

        public static string Num(double value, string format = "#,0") => value.ToString(format, Culture);

        public static string Days(double days)
        {
            if (double.IsInfinity(days) || days > 5000) return "∞";
            if (days >= 60) return (days / 30).ToString("0.0", Culture) + " months";
            return days.ToString("0", Culture) + " days";
        }

        public static string Weeks(double weeks)
        {
            if (double.IsInfinity(weeks) || weeks > 999) return "never at this plan";
            if (weeks < 1) return "under a week";
            return weeks.ToString("0", Culture) + (weeks < 1.5 ? " week" : " weeks");
        }

        /// <summary>Runway as five battery segments: one per month, colored by urgency.</summary>
        public static int RunwaySegments(double days) => (int)Math.Min(5, Math.Ceiling(Math.Max(0, days) / 30.0));

        public static string RunwayClass(double days) => days < 30 ? "fill-bad" : days < 60 ? "fill-warn" : "fill-good";

        public static string SkillClass(Skill skill) => "skill-" + skill.ToString().ToLowerInvariant();

        public static string ActivityClass(ActivityKind kind) => "act-" + kind.ToString().ToLowerInvariant();

        public static string KindClass(MessageKind kind) => "kind-" + kind.ToString().ToLowerInvariant();

        public static string KindLabel(MessageKind kind)
        {
            switch (kind)
            {
                case MessageKind.Critical: return "CRITICAL";
                case MessageKind.Decision: return "DECISION";
                case MessageKind.Opportunity: return "OPPORTUNITY";
                default: return "NEWS";
            }
        }

        public static string KindIcon(MessageKind kind)
        {
            switch (kind)
            {
                case MessageKind.Critical: return Icons.Warning;
                case MessageKind.Decision: return Icons.Scale;
                case MessageKind.Opportunity: return Icons.Star;
                default: return Icons.Info;
            }
        }

        public static string ActivityIcon(ActivityKind kind)
        {
            switch (kind)
            {
                case ActivityKind.Develop: return Icons.Code;
                case ActivityKind.Freelance: return Icons.Briefcase;
                case ActivityKind.Marketing: return Icons.Megaphone;
                case ActivityKind.Learn: return Icons.Graduation;
                case ActivityKind.Research: return Icons.Search;
                case ActivityKind.DayJob: return Icons.Coffee;
                case ActivityKind.Jam: return Icons.Bolt;
                case ActivityKind.Hotfix: return Icons.Bug;
                case ActivityKind.Community: return Icons.Chat;
                case ActivityKind.Demo: return Icons.Play;
                default: return Icons.Moon;
            }
        }

        public static string Stars(double rating) => Rules.Stars(rating);

        public static string SkillIcon(Skill skill)
        {
            switch (skill)
            {
                case Skill.Programming: return Icons.Code;
                case Skill.Design: return Icons.Chat;
                case Skill.Art: return Icons.Brush;
                case Skill.Audio: return Icons.Note;
                case Skill.Writing: return Icons.Pen;
                case Skill.Marketing: return Icons.Megaphone;
                case Skill.Business: return Icons.Briefcase;
                case Skill.Production: return Icons.Calendar;
                default: return Icons.Star;
            }
        }

        /// <summary>The part of the day: 0 morning, 1 afternoon, 2 evening, 3 night.</summary>
        public static string SlotName(int slot) => slot >= 3 ? "Night" : GameCalendar.BlockNames[Math.Max(0, slot)];

        public static string SlotIcon(int slot) => slot <= 0 ? Icons.Sunrise : slot == 1 ? Icons.Sun : slot == 2 ? Icons.Sunset : Icons.Moon;

        public static string SlotClass(int slot) => slot <= 0 ? "time-morning" : slot == 1 ? "time-afternoon" : slot == 2 ? "time-evening" : "time-night";

        /// <summary>"Wed", "tomorrow", "today" or "in 9 days" for a deadline.</summary>
        public static string Due(int today, int day)
        {
            int days = day - today;
            if (days < 0) return $"{-days} day{(days == -1 ? "" : "s")} late";
            if (days == 0) return "due today";
            if (days == 1) return "due tomorrow";
            if (days < 7) return "due " + GameCalendar.DayNames[GameCalendar.DayOfWeek(day)];
            return $"due in {days} days";
        }

        /// <summary>"Tue 14 Mar".</summary>
        public static string DayMonth(int day) =>
            $"{GameCalendar.DayNames[GameCalendar.DayOfWeek(day)]} {GameCalendar.DayOfMonth(day)} {GameCalendar.MonthNames[GameCalendar.Month(day) - 1]}";

        public static int WeekOfYear(int day) => (GameCalendar.ToDate(day).DayOfYear - 1) / 7 + 1;
    }
}
