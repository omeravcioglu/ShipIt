using System.Linq;
using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>GigGrid: your freelance profile, active gigs and offers (Design/03 §2).</summary>
    public sealed class GigsPage : Page
    {
        public GigsPage(StudioUI studio) : base(studio) { }

        public override string Title => "GigGrid";
        public override string IconPath => Icons.Briefcase;
        public override string AppClass => "app-gigs";

        public override void Refresh()
        {
            Root.Clear();
            var s = State;
            var profile = s.Freelance.Profile;
            Header("GigGrid", "Freelance work pays the rent, and every gig hour is an hour not spent on your game.");
            BarNote = $"{s.Freelance.ActiveCount}/{Tuning.MaxActiveGigs} active";

            var active = s.Freelance.Active.OrderBy(g => g.DeadlineDay).ToList();
            Root.Add(Ui.Display($"Active gigs ({active.Count}/{Tuning.MaxActiveGigs})", "section-title"));
            if (active.Count == 0) Root.Add(Ui.Empty("No active gigs. Accept an offer below."));
            var activeRow = Ui.Row("row--top wrap");
            foreach (var gig in active) activeRow.Add(ActiveGigCard(gig));
            Root.Add(activeRow);

            var offers = s.Freelance.Offers.OrderByDescending(g => g.Client == ClientType.Friend).ThenByDescending(g => g.EffectiveHourlyRate).ToList();
            Root.Add(Ui.Display($"Offers ({offers.Count})", "section-title"));
            if (offers.Count == 0) Root.Add(Ui.Empty("No offers right now. New ones arrive every Monday, or spend a session looking for clients (Gig work)."));
            var offerRow = Ui.Row("row--top wrap");
            foreach (var gig in offers) offerRow.Add(OfferCard(gig));
            Root.Add(offerRow);

            Root.Add(Ui.Display("You on GigGrid", "section-title"));
            var bottom = Ui.Row("grid-row");
            bottom.Add(ProfileCard(profile));
            bottom.Add(WeekCard());
            bottom.Add(HowCard());
            Root.Add(bottom);
        }

        VisualElement ProfileCard(FreelanceProfile profile)
        {
            var s = State;
            var card = Ui.Card("Your profile", "col-5");
            var head = Ui.Row("card-head");
            head.Add(Ui.Portrait(AvatarLook.Founder, Avatar.FounderMood(s), 70f, "bg-lavender portrait--big"));
            var words = Ui.El("grow");
            var ratingLine = Ui.Row("runway-line");
            ratingLine.Add(Ui.Display(Fmt.Stars(profile.Rating), "big-number"));
            ratingLine.Add(Ui.Text(profile.RecentScores.Count > 0 ? $"from {profile.RecentScores.Count} reviews" : "no reviews yet", "big-number__unit"));
            words.Add(ratingLine);
            var stars = Ui.Row("stars");
            for (int i = 0; i < 5; i++)
            {
                var star = new Icon(Icons.Star, 18f, 2.2f);
                star.AddToClassList("star");
                star.EnableInClassList("star--on", profile.Rating >= i + 0.75);
                stars.Add(star);
            }
            words.Add(stars);
            head.Add(words);
            card.Add(head);
            card.Add(Ui.KeyValue("Completed · failed", $"{profile.CompletedGigs} · {profile.FailedGigs}"));
            card.Add(Ui.KeyValue("Earned so far", Fmt.Money(profile.TotalEarned), "good"));
            var best = SkillInfo.Craft.OrderByDescending(s.Founder.Get).First();
            var rate = Sim.ExplainGigRate(best);
            var rateRow = Ui.KeyValue($"Your rate ({best})", $"{Fmt.Money(rate.Value)}/h");
            Why(rateRow, () => rate.Describe("0.00") + "\nA new profile sees fewer, smaller gigs. Ratings bring more offers and better pay.");
            card.Add(rateRow);
            var pricing = Ui.Row("segmented");
            foreach (RateSetting setting in System.Enum.GetValues(typeof(RateSetting)))
            {
                var value = setting;
                var button = Command(setting.ToString(), () => Sim.SetRateSetting(value), "segmented__btn" + (profile.RateSetting == setting ? " segmented__btn--on" : ""));
                Why(button, () => value == RateSetting.Budget ? "-20% pay, one extra offer a week, more risky clients."
                    : value == RateSetting.Premium ? "+25% pay, one fewer offer a week. Better clients once you have a 4/5 rating."
                    : "Standard market rate.");
                pricing.Add(button);
            }
            card.Add(pricing);
            return card;
        }

        VisualElement WeekCard()
        {
            var s = State;
            var card = Ui.Card("Work on your plate", "col-3");
            double accepted = Rules.ActiveGigCapacityHours(s);
            double sessions = s.Freelance.Active.Sum(g => Sim.SessionsLeft(g));
            var line = Ui.Row("runway-line");
            line.Add(Ui.Display(System.Math.Ceiling(sessions).ToString("0"), "big-number"));
            line.Add(Ui.Text(System.Math.Ceiling(sessions) == 1 ? "session left" : "sessions left", "big-number__unit"));
            card.Add(line);
            card.Add(Ui.KeyValue("Accepted work", $"{accepted:0}h"));
            card.Add(Ui.KeyValue("This week on gigs", $"{s.CurrentWeek.FreelanceHours:0}h"));
            card.Add(Ui.Text(accepted <= 0 ? "Nothing accepted. Take an offer, or spend a session looking for clients."
                : "Every session is 4 hours, played as a minigame. Play well and it counts for more.", "small muted"));
            return card;
        }

        static VisualElement HowCard()
        {
            var card = Ui.Card("How gigs work", "col-4");
            card.Add(Step("1", "Accept offers (up to 3 at once)."));
            card.Add(Step("2", "Spend a morning, afternoon or evening on it. Each session is a minigame for the gig's skill."));
            card.Add(Step("3", "Deliver before the deadline. A week late and the client cancels: no pay, 1/5 rating."));
            card.Add(Ui.Text("Careful sessions finish sooner and earn better ratings. Pay arrives a few days after delivery (corporate clients: 30 days). Some new clients never pay.", "small muted"));
            return card;
        }

        static VisualElement Step(string number, string text)
        {
            var row = Ui.Row("step");
            row.Add(Ui.Display(number, "step__number"));
            row.Add(Ui.Text(text, "step__text"));
            return row;
        }

        VisualElement ActiveGigCard(Gig gig)
        {
            var card = Ui.Card(null, "gig-card");
            card.Add(GigHead(gig));
            var bar = Ui.Row("gig-card__progress");
            bar.Add(Ui.Bar(gig.Progress, Fmt.SkillClass(gig.Discipline), 14));
            bar.Add(Ui.Text($"{gig.HoursDone:0}/{gig.Hours:0}h", "gig-card__hours"));
            card.Add(bar);
            int daysLeft = gig.DeadlineDay - State.Day;
            var foot = Ui.Row("card-foot");
            foot.Add(Ui.Pill(Icons.Clock, daysLeft >= 0 ? $"Due {GameCalendar.FormatShort(gig.DeadlineDay)} ({daysLeft}d)" : $"LATE by {-daysLeft} days",
                daysLeft < 0 ? "pill--danger" : daysLeft < 4 ? "pill--warn" : "pill--ink"));
            foot.Add(Ui.Grow());
            foot.Add(Ui.Display(Fmt.Money(gig.Pay), "gig-card__pay"));
            card.Add(foot);
            var work = Ui.IconBtn(Fmt.SkillIcon(gig.Discipline), $"Work on it now ({Activities.GigGameName(gig.Discipline)})",
                () => Studio.Play(PlanBlock.Gig(gig.Id)), "btn btn--primary btn--small gig-card__work", 16f);
            work.name = gig.Client == ClientType.Friend ? "work-friend" : "work-gig-" + gig.Id;
            work.SetEnabled(!Sim.IsNight && Sim.ForcedActivity() == null);
            card.Add(work);
            return card;
        }

        VisualElement OfferCard(Gig gig)
        {
            var s = State;
            var card = Ui.Card(null, "gig-card");
            card.Add(GigHead(gig));
            double yours = s.Founder.Get(gig.Discipline);
            card.Add(Ui.KeyValue(gig.Discipline.ToString(), $"needs {gig.RequiredSkill:0.0} · you {yours:0.0}", gig.RequiredSkill > yours + 0.5 ? "warn" : null));
            card.Add(Ui.KeyValue("Work", $"{gig.Hours:0}h in {gig.DurationDays} days"));
            card.Add(Ui.KeyValue("Rate", $"{Fmt.Money(gig.EffectiveHourlyRate)}/h"));
            if (!string.IsNullOrEmpty(gig.RiskFlags))
            {
                var chips = Ui.Row("wrap");
                foreach (var flag in gig.RiskFlags.Split(new[] { " · " }, System.StringSplitOptions.RemoveEmptyEntries))
                    chips.Add(Ui.Chip(flag, flag == "Rush" || flag == "Above your level" || flag == "Vague requirements" || flag == "New client" ? "chip--warn" : null));
                card.Add(chips);
            }
            var foot = Ui.Row("card-foot");
            foot.Add(Ui.Pill(Icons.Clock, "Expires " + GameCalendar.FormatShort(gig.ExpiresDay), "pill--ink"));
            foot.Add(Ui.Grow());
            foot.Add(Ui.Display(Fmt.Money(gig.Pay), "gig-card__pay"));
            card.Add(foot);
            var buttons = Ui.Row("gig-card__buttons");
            var accept = Command("Accept", () => Sim.AcceptGig(gig.Id, out _), "btn btn--primary");
            if (gig.Client == ClientType.Friend) accept.name = "accept-friend";
            accept.SetEnabled(s.Freelance.ActiveCount < Tuning.MaxActiveGigs);
            buttons.Add(accept);
            buttons.Add(Command("Decline", () => Sim.DeclineGig(gig.Id)));
            card.Add(buttons);
            return card;
        }

        static VisualElement GigHead(Gig gig)
        {
            var head = Ui.Row("card-head");
            head.Add(Ui.Portrait(AvatarLook.Client(gig.Client), Mood.Happy, 46f, "bg-lavender"));
            var words = Ui.El("grow");
            words.Add(Ui.Text(gig.Title, "gig-card__title"));
            words.Add(Ui.Text($"{gig.ClientName} · {ClientLabel(gig.Client)}", "small muted"));
            head.Add(words);
            head.Add(Ui.Chip(gig.Discipline.ToString(), "chip--skill " + Fmt.SkillClass(gig.Discipline)));
            return head;
        }

        static string ClientLabel(ClientType client)
        {
            switch (client)
            {
                case ClientType.SmallStudio: return "small studio";
                case ClientType.Agency: return "agency";
                case ClientType.Startup: return "startup";
                case ClientType.Corporate: return "corporate";
                default: return "friend";
            }
        }
    }
}
