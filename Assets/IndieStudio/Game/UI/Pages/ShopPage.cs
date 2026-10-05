using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>Hardware, software and your living situation. Every item names the problem it solves (Design/08 §3).</summary>
    public sealed class ShopPage : Page
    {
        public ShopPage(StudioUI studio) : base(studio) { }

        public override string Title => "Shop";
        public override string IconPath => Icons.Bag;
        public override string AppClass => "app-shop";

        public override void Refresh()
        {
            Root.Clear();
            var s = State;
            Header("Upgrades", "Your bedroom, your laptop, your tools. Upgrades solve specific problems: buy them when the problem is real.");
            BarNote = Fmt.Money(s.Finance.Cash) + " to spend";

            var row = Ui.Row("row--top wrap");
            Root.Add(row);
            foreach (var item in ShopCatalog.Items)
            {
                bool owned = s.Owned.Contains(item.Id);
                var card = Ui.Card(null, "shop-card" + (owned ? " card--owned" : ""));
                var head = Ui.Row("card-head");
                head.Add(Ui.Tile(ItemIcon(item.Id), "tile--big " + ItemColor(item.Id), 26f));
                var words = Ui.El("grow");
                words.Add(Ui.Display(item.Name, "card-head__title"));
                string cost = item.Price > 0 && item.Monthly > 0 ? $"{Fmt.Money(item.Price)} + {Fmt.Money(item.Monthly)}/month"
                    : item.Price > 0 ? Fmt.Money(item.Price) : $"{Fmt.Money(item.Monthly)}/month";
                words.Add(Ui.Text(cost, "shop-card__price"));
                head.Add(words);
                card.Add(head);
                card.Add(Ui.Text(item.Description, "shop-card__text"));
                card.Add(Ui.Pill(Icons.Check, "Solves: " + item.Solves, "pill--soft pill--wrap"));
                card.Add(Ui.Grow());
                var id = item.Id;
                if (!owned)
                {
                    var buy = Command("Buy", () => Sim.Buy(id, out _), "btn btn--primary");
                    buy.SetEnabled(item.Price <= 0 || s.Finance.Cash - item.Price >= -s.Finance.CreditLimit);
                    card.Add(buy);
                }
                else if (item.Price <= 0 && item.Monthly > 0)
                {
                    var foot = Ui.Row("card-foot");
                    foot.Add(Ui.Pill(Icons.Check, "Active", "pill--good"));
                    foot.Add(Ui.Grow());
                    foot.Add(Command("Cancel", () => Sim.CancelSubscription(id), "btn btn--small"));
                    card.Add(foot);
                }
                else card.Add(Ui.Pill(Icons.Check, "Owned. It's in your room.", "pill--good"));
                row.Add(card);
            }

            Root.Add(Ui.Display("Life", "section-title"));
            var life = Ui.Card(null, "life-card");
            var f = s.Founder;
            var lifeHead = Ui.Row("card-head");
            lifeHead.Add(Ui.Tile(Icons.Home, "tile--big bg-peach", 26f));
            var lifeWords = Ui.El("grow");
            lifeWords.Add(Ui.Display(SkillsPage.HousingLabel(f.Housing), "card-head__title"));
            lifeWords.Add(Ui.Text($"Rent {Fmt.Money(s.Finance.GetRecurring("rent")?.MonthlyAmount ?? 0)} a month", "muted"));
            lifeHead.Add(lifeWords);
            life.Add(lifeHead);
            if (f.Job != DayJob.None)
            {
                life.Add(Ui.KeyValue("Day job", f.Job == DayJob.PartTime ? $"Part-time, {Fmt.Money(Tuning.PartTimeMonthlyPay)}/month" : $"Full-time, {Fmt.Money(Tuning.FullTimeMonthlyPay)}/month"));
                var quit = Command("Quit the day job", Sim.QuitDayJob, "btn btn--danger btn--small");
                Why(quit, () => "Get your weekdays back. You're paid for the days worked this month.");
                life.Add(quit);
            }
            else life.Add(Ui.Text("No day job. Offers come when money is tight.", "small muted"));
            Root.Add(life);
        }

        static string ItemIcon(string id)
        {
            switch (id)
            {
                case "cooling_pad": return Icons.Snowflake;
                case "backups": return Icons.Cloud;
                case "ergonomic_chair": return Icons.Chair;
                case "gaming_pc": return Icons.Monitor;
                case "pro_art_tools": return Icons.Brush;
                default: return Icons.Bag;
            }
        }

        static string ItemColor(string id)
        {
            switch (id)
            {
                case "cooling_pad": return "bg-cyan";
                case "backups": return "bg-blue";
                case "ergonomic_chair": return "bg-teal icon-cream";
                case "gaming_pc": return "bg-lavender";
                case "pro_art_tools": return "bg-orange";
                default: return "bg-peach";
            }
        }
    }
}
