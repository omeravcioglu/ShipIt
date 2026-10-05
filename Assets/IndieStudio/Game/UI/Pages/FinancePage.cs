using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// The Bank (Design/13): a 90-day cash forecast, runway, the bills that land on the 1st (as a receipt),
    /// money owed to you, the last monthly report and the ledger.
    /// </summary>
    public sealed class FinancePage : Page
    {
        public FinancePage(StudioUI studio) : base(studio) { }

        public override string Title => "Bank";
        public override string IconPath => Icons.Bank;
        public override string AppClass => "app-bank";

        public override void Refresh()
        {
            Root.Clear();
            var fin = State.Finance;
            Header("Finances", "Every dollar matters. Money owed to you isn't money in the bank until it arrives.");
            BarNote = fin.Cash < 0 ? "On the credit card" : null;

            var forecast = CashForecast.Build(State, 90);
            var top = Ui.Row("grid-row grid-row--tall");
            top.Add(ForecastCard(forecast));
            top.Add(RunwayCard(forecast));
            Root.Add(top);

            var middle = Ui.Row("grid-row");
            middle.Add(Receipt());
            middle.Add(ComingIn());
            middle.Add(Months());
            Root.Add(middle);

            Root.Add(Ledger());
        }

        VisualElement ForecastCard(CashForecast forecast)
        {
            var card = Ui.Card("Cash forecast · next 90 days", "col-8");
            var legend = Ui.Row("legend card__corner");
            legend.Add(Legend("legend__line", "What the bank knows"));
            legend.Add(Legend("legend__band", "If gigs keep paying"));
            card.Add(legend);

            forecast.Range(out double min, out double max);
            var chartRow = Ui.Row("chart-row");
            var yAxis = Ui.El("chart-y");
            double step = Step(max - min);
            for (double v = System.Math.Ceiling(min / step) * step; v <= max; v += step)
            {
                var label = Ui.Text(v == 0 ? "$0" : (v < 0 ? "-$" : "$") + Fmt.Num(System.Math.Abs(v) / 1000, "0.#") + "k", "chart-y__label" + (v < 0 ? " bad" : ""));
                label.style.top = Length.Percent((float)((max - v) / (max - min) * 100));
                yAxis.Add(label);
            }
            chartRow.Add(yAxis);
            var plot = Ui.El("chart-plot");
            var chart = new ForecastChart();
            chart.SetData(forecast, min, max);
            plot.Add(chart);
            if (forecast.BrokeIndex > 0)
            {
                var marker = Ui.Pill(Icons.Warning, "Broke on " + GameCalendar.FormatShort(forecast.DayAt(forecast.BrokeIndex)), "pill--danger chart-marker");
                float x = forecast.BrokeIndex / (float)(forecast.Known.Length - 1) * 100;
                marker.style.left = Length.Percent(System.Math.Min(x, 72));
                marker.style.top = Length.Percent((float)((max - 0) / (max - min) * 100));
                plot.Add(marker);
            }
            chartRow.Add(plot);
            card.Add(chartRow);

            var xAxis = Ui.Row("chart-x");
            for (int i = 0; i <= 90; i += 30)
            {
                var label = Ui.Text(i == 0 ? "Today" : GameCalendar.FormatShort(forecast.DayAt(i)),
                    "chart-x__label" + (i == 0 ? "" : i == 90 ? " chart-x__label--end" : " chart-x__label--mid"));
                label.style.left = Length.Percent(i / 90f * 100);
                xAxis.Add(label);
            }
            card.Add(xAxis);
            var note = forecast.GigsPerWeek > 0 || forecast.SalesPerMonth > 0
                ? $"The line counts bills on the 1st and money already owed to you. The band adds gigs at {Fmt.Money(forecast.GigsPerWeek)}/week" +
                  (forecast.SalesPerMonth > 0 ? $" and game sales at {Fmt.Money(forecast.SalesPerMonth)}/month" : "") + ", like lately."
                : "The line counts bills on the 1st and money already owed to you. No recent gig income to project.";
            card.Add(Ui.Text(note, "small muted"));
            return card;
        }

        static double Step(double range) => range > 40000 ? 10000 : range > 16000 ? 5000 : range > 6000 ? 2000 : 1000;

        static VisualElement Legend(string swatchClass, string text)
        {
            var item = Ui.Row("legend__item");
            item.Add(Ui.El(swatchClass));
            item.Add(Ui.Text(text, "legend__text"));
            return item;
        }

        VisualElement RunwayCard(CashForecast forecast)
        {
            var s = State;
            var fin = s.Finance;
            double runway = fin.RunwayDays();
            var card = Ui.Card("Runway", "col-4 " + (runway < 30 ? "card--red" : runway < 60 ? "card--mustard-light" : "card--sage-light"));
            var head = Ui.Row("card-head");
            head.Add(Ui.Portrait(AvatarLook.Founder, Avatar.FounderMood(s), 84f, "bg-paper portrait--big"));
            var words = Ui.El("grow");
            words.Add(Ui.Display(Fmt.Days(runway), "big-number big-number--mid"));
            words.Add(Ui.Text(forecast.BrokeIndex > 0 ? "Money runs out " + GameCalendar.FormatShort(forecast.DayAt(forecast.BrokeIndex)) : "No money trouble in sight", "runway__when"));
            words.Add(Ui.Segments(Fmt.RunwaySegments(runway), 5, Fmt.RunwayClass(runway)));
            head.Add(words);
            card.Add(head);

            var bubble = Ui.El("speech");
            bubble.Add(Ui.Text(RunwayQuip(runway, fin.Cash), "speech__text"));
            card.Add(bubble);

            var stats = Ui.Row("stats");
            stats.Add(Ui.Stat("Cash now", Fmt.Money(fin.Cash), fin.Cash < 0 ? "stat--bad" : null));
            stats.Add(Ui.Stat("Owed to you", Fmt.Money(fin.PendingIncome)));
            stats.Add(Ui.Stat("Credit left", Fmt.Money(System.Math.Max(0, fin.CreditLimit + System.Math.Min(0, fin.Cash)))));
            card.Add(stats);
            card.Add(Ui.KeyValue("Earned from games", Fmt.Money(s.LifetimeGameNet), "good"));
            card.Add(Ui.KeyValue("Earned from gigs", Fmt.Money(s.Freelance.Profile.TotalEarned), "good"));
            return card;
        }

        static string RunwayQuip(double runway, double cash)
        {
            if (cash < 0) return "We're on the credit card. Everything is fine. (It is not fine.)";
            if (runway < 30) return "Less than a month. Time to call in every favor.";
            if (runway < 60) return "Under two months. I can hear the clock ticking.";
            if (runway < 120) return "A few months of runway. Don't get comfortable.";
            return "Plenty of runway. For now.";
        }

        VisualElement Receipt()
        {
            var fin = State.Finance;
            var receipt = Ui.El("receipt col-4");
            var paper = Ui.El("receipt__paper");
            receipt.Add(paper);
            paper.Add(Ui.Display("MONTHLY BILLS", "receipt__title"));
            paper.Add(Ui.Text("Charged every 1st of the month", "receipt__sub"));
            paper.Add(Ui.El("receipt__rule"));
            foreach (var cost in fin.Recurring) paper.Add(Ui.Leader(cost.Name, "-" + Fmt.Money(cost.MonthlyAmount)));
            var job = State.Founder.Job;
            if (job != DayJob.None)
                paper.Add(Ui.Leader(job == DayJob.PartTime ? "Part-time job" : "Full-time job",
                    "+" + Fmt.Money(job == DayJob.PartTime ? Tuning.PartTimeMonthlyPay : Tuning.FullTimeMonthlyPay), "good"));
            paper.Add(Ui.El("receipt__rule receipt__rule--double"));
            var total = Ui.Row("receipt__total");
            total.Add(Ui.Text("TOTAL", "receipt__total-label"));
            total.Add(Ui.Grow());
            total.Add(Ui.Display("-" + Fmt.Money(fin.MonthlyRecurringTotal), "receipt__total-value"));
            paper.Add(total);
            paper.Add(Ui.Text($"That's {Fmt.Money(fin.MonthlyRecurringTotal / 30)} a day just to exist.", "receipt__sub"));
            receipt.Add(new ZigzagEdge());
            return receipt;
        }

        VisualElement ComingIn()
        {
            var fin = State.Finance;
            var card = Ui.Card("Owed to you", "col-4");
            var scheduled = fin.Scheduled.OrderBy(p => p.Day).ToList();
            if (scheduled.Count == 0) card.Add(Ui.Empty("Nothing scheduled. Deliver a gig or sell a game."));
            foreach (var payment in scheduled.Take(6))
            {
                var row = Ui.Row("payment");
                var date = GameCalendar.ToDate(payment.Day);
                var badge = Ui.El("date-badge");
                badge.Add(Ui.Text(GameCalendar.MonthNames[date.Month - 1].ToUpperInvariant(), "date-badge__month"));
                badge.Add(Ui.Display(date.Day.ToString(), "date-badge__day"));
                row.Add(badge);
                row.Add(Ui.Text(payment.Description, "payment__text"));
                row.Add(Ui.Text("+" + Fmt.Money(payment.Amount), "payment__amount good"));
                card.Add(row);
            }
            if (scheduled.Count > 6) card.Add(Ui.Text($"…and {scheduled.Count - 6} more", "small muted"));
            card.Add(Ui.Grow());
            card.Add(Ui.KeyValue("Total owed", Fmt.Money(fin.PendingIncome), "good bold"));
            return card;
        }

        VisualElement Months()
        {
            var fin = State.Finance;
            var report = fin.Reports.LastOrDefault();
            var card = Ui.Card(report != null ? "Report · " + GameCalendar.MonthLabel(report.MonthIndex) : "Monthly report", "col-4");
            if (report == null)
            {
                card.Add(Ui.Empty("Your first monthly report arrives on the 1st."));
                return card;
            }
            foreach (var line in report.ByCategory.OrderByDescending(x => x.Value).Take(7))
                card.Add(Ui.KeyValue(line.Key.ToString(), Fmt.SignedMoney(line.Value), line.Value >= 0 ? "good" : null));
            card.Add(Ui.Divider());
            card.Add(Ui.KeyValue("Net", Fmt.SignedMoney(report.Net), report.Net >= 0 ? "good bold" : "bad bold"));
            if (fin.Reports.Count > 1)
            {
                double max = fin.Reports.Max(r => System.Math.Abs(r.Net));
                var bars = Ui.Row("month-bars");
                foreach (var r in fin.Reports.Skip(System.Math.Max(0, fin.Reports.Count - 8)))
                {
                    var column = Ui.El("month-bar");
                    var fill = Ui.El("month-bar__fill " + (r.Net >= 0 ? "fill-good" : "fill-bad"));
                    fill.style.height = Length.Percent((float)(max > 0 ? System.Math.Max(0.06, System.Math.Abs(r.Net) / max) * 78 : 5));
                    column.Add(fill);
                    column.Add(Ui.Text(GameCalendar.MonthNames[r.MonthIndex % 12].Substring(0, 1), "month-bar__label"));
                    var rr = r;
                    Why(column, () => $"{GameCalendar.MonthLabel(rr.MonthIndex)}: {Fmt.SignedMoney(rr.Net)}");
                    bars.Add(column);
                }
                card.Add(bars);
            }
            return card;
        }

        VisualElement Ledger()
        {
            var fin = State.Finance;
            var card = Ui.Card("Recent transactions");
            if (fin.Transactions.Count == 0)
            {
                card.Add(Ui.Empty("No transactions yet."));
                return card;
            }
            var columns = Ui.Row("ledger");
            var list = fin.Transactions.AsEnumerable().Reverse().Take(24).ToList();
            int half = (list.Count + 1) / 2;
            foreach (var chunk in new[] { list.Take(half), list.Skip(half) })
            {
                var column = Ui.El("ledger__column");
                foreach (var tx in chunk)
                {
                    var row = Ui.Row("ledger__row");
                    row.Add(Ui.Text(GameCalendar.FormatShort(tx.Day), "ledger__date"));
                    row.Add(Ui.Text(tx.Description, "ledger__text"));
                    row.Add(Ui.Text(Fmt.SignedMoney(tx.Amount), "ledger__amount " + (tx.Amount >= 0 ? "good" : "")));
                    column.Add(row);
                }
                columns.Add(column);
            }
            card.Add(columns);
            return card;
        }
    }
}
