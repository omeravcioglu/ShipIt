using System;
using System.Collections.Generic;
using System.Linq;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// The tutorial (Design/02 §10): Old Dev Doug walks you through your first day, one step at a time, and
    /// points at what to click with a pulsing ring. Steps that ask you to do something finish when you've
    /// done it; the rest have a button. The first step names your studio. It can be skipped at any time, and
    /// replayed from the menu. Progress lives in the game state, so a saved game resumes the tutorial where it was.
    /// </summary>
    public sealed class Coach
    {
        sealed class Step
        {
            public string Text;
            public Func<string> Target;
            /// <summary>When set, the step finishes by itself once this is true; otherwise it has a button.</summary>
            public Func<bool> Done;
            public string Button = "Next";
            /// <summary>Something to fill in on this step (the studio's name).</summary>
            public Func<VisualElement> Extra;
            /// <summary>Runs when the step's button is pressed, before moving on.</summary>
            public Action<VisualElement> OnNext;
        }

        readonly StudioUI _studio;
        readonly VisualElement _layer;
        readonly VisualElement _box;
        readonly Label _text;
        readonly Label _counter;
        readonly Button _next;
        readonly VisualElement _extra;
        readonly VisualElement _ring;
        readonly List<Step> _steps;
        bool _pulse;
        string _target;
        string _scrolledTo;
        int _extraStep = -1;
        GameSimulation _extraSim;

        public Coach(StudioUI studio, VisualElement layer)
        {
            _studio = studio;
            _layer = layer;
            _layer.pickingMode = PickingMode.Ignore;

            _ring = Ui.El("coach-ring hidden");
            _ring.pickingMode = PickingMode.Ignore;
            _layer.Add(_ring);

            _box = Ui.El("coach");
            _box.name = "coach";
            _box.pickingMode = PickingMode.Position;
            var head = Ui.Row("coach__head");
            head.Add(Ui.Portrait(AvatarLook.Mentor, Mood.Happy, 62f, "bg-mustard-light coach__face"));
            var words = Ui.El("grow");
            words.Add(Ui.Display("Old Dev Doug", "coach__name"));
            _counter = Ui.Text("", "coach__counter");
            words.Add(_counter);
            head.Add(words);
            _box.Add(head);
            _text = Ui.Text("", "coach__text");
            _box.Add(_text);
            _extra = Ui.El("coach__extra");
            _box.Add(_extra);
            var buttons = Ui.Row("coach__buttons");
            var skip = Ui.Btn("Skip tutorial", Finish, "coach__skip");
            skip.name = "coach-skip";
            buttons.Add(skip);
            buttons.Add(Ui.Grow());
            _next = Ui.Btn("Next", Advance, "btn btn--mustard coach__next");
            _next.name = "coach-next";
            buttons.Add(_next);
            _box.Add(buttons);
            _layer.Add(_box);

            _steps = BuildSteps();
            _layer.schedule.Execute(Track).Every(50);
            _layer.schedule.Execute(() =>
            {
                _pulse = !_pulse;
                _ring.EnableInClassList("coach-ring--pulse", _pulse);
            }).Every(550);
        }

        public bool IsActive => !Finished;
        GameState State => _studio.Sim.State;

        int Index
        {
            get => State.TutorialStep;
            set => State.TutorialStep = value;
        }

        bool Finished => State.TutorialDone;

        List<Step> BuildSteps()
        {
            string name = _studio.PlayerName;
            string hey = name != null ? $"Hey {name}, I'm Doug." : "Hey, I'm Doug.";
            bool FriendTaken() => State.Freelance.Gigs.Any(g => g.Client == ClientType.Friend && g.Status != GigStatus.Offered) || State.Freelance.ActiveCount > 0;
            bool GigWorked() => State.Freelance.Gigs.Any(g => g.Sessions > 0) || State.Freelance.Profile.CompletedGigs > 0;
            return new List<Step>
            {
                new Step
                {
                    Text = hey + " I shipped my first game from a bedroom just like this one. I'll show you how it works. " +
                           "But first: every studio needs a name. What's yours?",
                    Extra = () => new StudioNameField(_studio),
                    OnNext = extra => (extra as StudioNameField)?.Apply(),
                    Button = "That's us!",
                },
                new Step
                {
                    Text = "Up here is your goal: ship your first game. Click it any time to see the steps.",
                    Target = () => "hud-goal",
                    Button = "Got it",
                },
                new Step
                {
                    Text = "And this is your runway. $5,000 lasts about three months, because $1,460 in bills leaves your account on the 1st of every month.",
                    Target = () => "hud-runway",
                    Button = "Gulp",
                },
                new Step
                {
                    Text = "Your day has three parts: morning, afternoon and evening. Time only moves when you decide what to do with them. There's no rush.",
                    Target = () => "today",
                    Button = "Okay",
                },
                new Step
                {
                    Text = "Money first. Your friend Sam has a small gig for you. Open GigGrid.",
                    Target = () => "dock-gigs",
                    Done = () => _studio.Current is GigsPage || FriendTaken(),
                },
                new Step
                {
                    Text = "Accept Sam's gig: $180 for fixing a few bugs in his mobile game.",
                    Target = () => "accept-friend",
                    Done = FriendTaken,
                },
                new Step
                {
                    Text = "Now spend this morning on it: open Gig work in your day and pick Sam's gig. Every work session is a little game. The better you play, the more gets done.",
                    Target = () => _studio.Today.OpenGroup == "gig" ? "gig-friend" : "act-gig",
                    Done = GigWorked,
                },
                new Step
                {
                    Text = "Nicely done! Sam's gig needs a couple more sessions. Now for your own game: open the Project app.",
                    Target = () => "dock-project",
                    Done = () => _studio.Current is ProjectPage || State.Projects.Count > 0,
                },
                new Step
                {
                    Text = "Pick an idea, keep it Tiny, and choose an art style you can pull off. Pixel Art or Minimalist are safe. Then start it.",
                    Target = () => "start-project",
                    Done = () => State.Projects.Count > 0,
                },
                new Step
                {
                    Text = "A game is a list of features. Open Build your game, pick a feature and wire it up.",
                    Target = () => _studio.Today.OpenGroup == "build" ? "build-first" : "act-build",
                    Done = () => State.Projects.Any(p => p.TotalDone > 0),
                },
                new Step
                {
                    Text = "Every session is four hours. Over 45 hours of work in a week, burnout builds up, so rest some evenings and keep weekends light.",
                    Target = () => "today-week",
                    Button = "Will do",
                },
                new Step
                {
                    Text = "Once the evening is used up, go to sleep. Mornings bring mail, payments and new gig offers.",
                    Target = () => _studio.Sim.IsNight ? "today-sleep" : "today",
                    Done = () => State.Day > State.TutorialStepDay,
                },
                new Step
                {
                    Text = "That's the basics. Finish Sam's gig, build every feature, open a store page and launch. I'll be in the corner if you need advice. Good luck!",
                    Button = "Thanks, Doug",
                },
            };
        }

        public void Refresh()
        {
            if (Finished)
            {
                _box.AddToClassList("hidden");
                _ring.AddToClassList("hidden");
                return;
            }
            while (Index < _steps.Count && _steps[Index].Done != null && _steps[Index].Done()) StepTo(Index + 1);
            if (Index >= _steps.Count)
            {
                Finish();
                return;
            }
            var step = _steps[Index];
            _text.text = step.Text;
            _counter.text = $"Tutorial · {Index + 1} of {_steps.Count}";
            _next.text = step.Button;
            _next.EnableInClassList("hidden", step.Done != null);
            if (_extraStep != Index || _extraSim != _studio.Sim)
            {
                _extraStep = Index;
                _extraSim = _studio.Sim;
                _extra.Clear();
                var extra = step.Extra?.Invoke();
                if (extra != null) _extra.Add(extra);
            }
            else if (_extra.childCount > 0) (_extra[0] as StudioNameField)?.Sync();
            _extra.EnableInClassList("hidden", _extra.childCount == 0);
            _target = step.Target?.Invoke();
            bool busy = _studio.Minigames.IsOpen || _studio.HasBlockingModal || (_studio.Hud?.IsMenuOpen ?? false);
            _box.EnableInClassList("hidden", busy);
            Track();
        }

        void StepTo(int index)
        {
            Index = index;
            State.TutorialStepDay = State.Day;
        }

        void Advance()
        {
            if (Index < _steps.Count) _steps[Index].OnNext?.Invoke(_extra.childCount > 0 ? _extra[0] : null);
            StepTo(Index + 1);
            _studio.Game.NotifyChanged(); // the step is part of the save
        }

        /// <summary>Presses the step's button (tests). Steps that wait for an action ignore it.</summary>
        public void Next()
        {
            if (Finished || Index >= _steps.Count || _steps[Index].Done != null) return;
            Advance();
        }

        /// <summary>The element the tutorial is pointing at right now (tests and debugging).</summary>
        public string TargetName => Finished ? null : _target;

        public void Finish()
        {
            State.TutorialDone = true;
            _box.AddToClassList("hidden");
            _ring.AddToClassList("hidden");
            _studio.Game.NotifyChanged();
        }

        /// <summary>Starts the tutorial again from the first step (the menu's "Replay the tutorial").</summary>
        public void Restart()
        {
            State.TutorialDone = false;
            _scrolledTo = null;
            _extraStep = -1;
            StepTo(0);
            _studio.Game.NotifyChanged();
        }

        /// <summary>Keeps the ring on its target (which may move or be rebuilt) and the box out of its way.</summary>
        void Track()
        {
            if (Finished || Index >= _steps.Count) return;
            _target = _steps[Index].Target?.Invoke();
            var target = string.IsNullOrEmpty(_target) || _box.ClassListContains("hidden") ? null : _studio.Root.Q(_target);
            bool visible = target != null && target.resolvedStyle.display != DisplayStyle.None && target.worldBound.width > 1f && IsShown(target);

            // A target below the fold of a scrolling window is scrolled into view once.
            var scroll = visible ? target.GetFirstAncestorOfType<ScrollView>() : null;
            if (scroll != null)
            {
                var view = scroll.contentViewport.worldBound;
                var b = target.worldBound;
                bool inside = b.yMin >= view.yMin - 1f && b.yMax <= view.yMax + 1f;
                if (!inside && _scrolledTo != _target)
                {
                    scroll.ScrollTo(target);
                    _scrolledTo = _target;
                }
                if (b.yMax < view.yMin || b.yMin > view.yMax) visible = false;
            }
            _ring.EnableInClassList("hidden", !visible);
            if (!visible)
            {
                _box.RemoveFromClassList("coach--top");
                return;
            }
            var bounds = target.worldBound;
            var topLeft = _layer.WorldToLocal(bounds.position);
            _ring.style.left = topLeft.x - 8f;
            _ring.style.top = topLeft.y - 8f;
            _ring.style.width = bounds.width + 16f;
            _ring.style.height = bounds.height + 16f;

            // The box sits bottom-right; when that would cover the target, it moves to the top-right.
            var box = _box.worldBound;
            var layer = _layer.worldBound;
            if (box.width > 1f && layer.width > 1f)
            {
                var bottomRight = new Rect(layer.xMax - 20f - box.width, layer.yMax - 116f - box.height, box.width, box.height);
                _box.EnableInClassList("coach--top", bounds.Overlaps(bottomRight));
            }
        }

        static bool IsShown(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || e.ClassListContains("hidden")) return false;
            return true;
        }
    }
}
