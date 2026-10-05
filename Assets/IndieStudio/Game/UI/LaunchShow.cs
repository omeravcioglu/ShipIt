using System;
using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Launch day (Design/10 §7): the moment you press Launch, a little show. Your game goes up on the store,
    /// the counters tick up to the copies day one is expected to sell, and players' first impressions pop in,
    /// talking about features you actually built. It only shows what the simulation decided at release.
    /// </summary>
    public sealed class LaunchShow
    {
        const float CountSeconds = 3.2f;
        const float FirstReviewAt = 1.1f;
        const float ReviewEvery = 0.9f;
        const int ReviewCount = 4;

        readonly Project _project;
        readonly Action _close;
        readonly List<PlayerReview> _reviews;
        readonly double _units;
        readonly double _perCopy;
        readonly Label _copies;
        readonly Label _earned;
        readonly Label _verdict;
        readonly VisualElement[] _reviewColumns;
        IVisualElementScheduledItem _ticker;
        float _time;
        int _shown;

        public LaunchShow(StudioUI studio, Project project, Action close)
        {
            _project = project;
            _close = close;
            _reviews = ReviewWriter.For(project, ReviewCount);
            _units = GameSimulation.ExpectedLaunchDayUnits(project);
            _perCopy = GameSimulation.NetPerLaunchCopy(project);
            var s = studio.Sim.State;
            bool vapor = project.Platform == StorePlatform.Vapor;

            Root = Ui.El("modal-backdrop launch-backdrop");
            Root.name = "launch-show";
            var panel = Ui.El("launch");
            Root.Add(panel);

            var head = Ui.Row("launch__head");
            var words = Ui.El("grow");
            words.Add(Ui.Text($"LAUNCH DAY · {GameCalendar.FormatShort(project.ReleaseDay).ToUpperInvariant()}", "launch__kicker"));
            words.Add(Ui.Display($"{project.Title} is out!", "launch__title"));
            words.Add(Ui.Text($"{s.StudioName} presents · {GameContent.Genre(project.Genre).Name} · ${project.Price:0.00} on {(vapor ? "Vapor" : "scratch.io")}", "launch__sub"));
            head.Add(words);
            head.Add(Ui.Display("OUT NOW", "launch__stamp"));
            panel.Add(head);

            var main = Ui.Row("launch__main");
            var screen = Ui.El("launch__screen");
            var preview = new GamePreview();
            preview.AddToClassList("launch__preview");
            preview.Show(project);
            screen.Add(preview);
            main.Add(screen);

            var stats = Ui.El("launch__stats");
            stats.Add(Counter("COPIES SOLD TODAY", out _copies));
            stats.Add(Counter("YOURS, AFTER FEES", out _earned));
            stats.Add(vapor
                ? Fixed("WISHLISTS AT LAUNCH", Fmt.Num(project.Result.LaunchWishlists))
                : Fixed("FOLLOWERS", Fmt.Num(s.Audience.Followers)));
            stats.Add(Ui.Text("Day one, as the store estimates it. Launch week starts now: hotfixes count most in the first days, players want answers, and posts sell copies.", "launch__note"));
            main.Add(stats);
            panel.Add(main);

            var reviewsHead = Ui.Row("launch__reviews-head");
            reviewsHead.Add(Ui.Text("FIRST IMPRESSIONS", "launch__kicker"));
            reviewsHead.Add(Ui.Grow());
            _verdict = Ui.Text("", "launch__verdict");
            reviewsHead.Add(_verdict);
            panel.Add(reviewsHead);
            var reviewList = Ui.Row("launch__reviews");
            _reviewColumns = new[] { Ui.El("launch__review-col launch__review-col--left"), Ui.El("launch__review-col") };
            foreach (var column in _reviewColumns) reviewList.Add(column);
            panel.Add(reviewList);

            var buttons = Ui.Row("launch__buttons");
            var done = Ui.IconBtn(Icons.ArrowRight, "Back to work", close, "btn btn--primary btn--big", 20f);
            done.name = "launch-done";
            buttons.Add(done);
            panel.Add(buttons);

            Show(0f);
            _ticker = Root.schedule.Execute(Tick).Every(33);
            Root.schedule.Execute(() => Celebrate.Confetti(Root, new Vector2(Root.layout.width / 2f, 150f), 44, project.Id)).StartingIn(80);
        }

        public VisualElement Root { get; }
        /// <summary>Reviews on screen so far.</summary>
        public int ReviewsShown => _shown;

        /// <summary>Jumps to the end of the show (tests, and anyone who'd rather not wait).</summary>
        public void SkipToEnd() => Show(60f);

        /// <summary>"Back to work".</summary>
        public void Close() => _close?.Invoke();

        public void Stop()
        {
            _ticker?.Pause();
            _ticker = null;
        }

        void Tick(TimerState timer)
        {
            _time += Mathf.Min(0.1f, timer.deltaTime / 1000f);
            Show(_time);
        }

        void Show(float time)
        {
            float k = Mathf.Clamp01(time / CountSeconds);
            double units = Math.Floor(_units * (1 - Math.Pow(1 - k, 3)));
            _copies.text = Fmt.Num(units);
            _earned.text = Fmt.Money(units * _perCopy);

            int due = time < FirstReviewAt ? 0 : Math.Min(_reviews.Count, 1 + (int)((time - FirstReviewAt) / ReviewEvery));
            while (_shown < due)
            {
                var card = ReviewView.Card(_reviews[_shown]);
                card.AddToClassList("review--new");
                _reviewColumns[_shown % 2].Add(card);
                _shown++;
                card.schedule.Execute(() => card.RemoveFromClassList("review--new")).StartingIn(30);
            }
            int up = _reviews.Take(_shown).Count(r => r.Positive);
            _verdict.text = _shown == 0 ? "Waiting for players…" : $"{up} of {_shown} recommend it";
            if (k >= 1f && _shown >= _reviews.Count) Stop();
        }

        static VisualElement Counter(string label, out Label value)
        {
            var box = Ui.El("launch__stat");
            box.Add(Ui.Text(label, "launch__stat-label"));
            value = Ui.Display("", "launch__stat-value");
            box.Add(value);
            return box;
        }

        static VisualElement Fixed(string label, string value)
        {
            var box = Ui.El("launch__stat launch__stat--small");
            box.Add(Ui.Text(label, "launch__stat-label"));
            box.Add(Ui.Display(value, "launch__stat-value"));
            return box;
        }
    }

    /// <summary>A player review as a little card: thumbs up or down, what they said, and who said it.</summary>
    public static class ReviewView
    {
        public static VisualElement Card(PlayerReview review)
        {
            var card = Ui.Row("review" + (review.Positive ? " review--up" : " review--down"));
            card.Add(Ui.Tile(review.Positive ? Icons.ThumbUp : Icons.ThumbDown, "review__thumb", 18f));
            var words = Ui.El("review__words");
            words.Add(Ui.Text(review.Text, "review__text"));
            words.Add(Ui.Text($"{review.Author} · {review.Hours:0.0} h on record", "review__author"));
            card.Add(words);
            return card;
        }
    }
}
