using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>
    /// Design gigs: sort a client's playtest feedback. Is it a problem to fix, a wish to note, or praise?
    /// Reading players well is most of a designer's job.
    /// </summary>
    public sealed class FeedbackSort : Minigame
    {
        enum Bin { Problem, Wish, Praise }

        static readonly string[] Problems =
        {
            "I fell through the floor in the second level.", "The game crashed when I opened the map.", "I couldn't figure out how to jump.",
            "The boss is impossible. I died 40 times.", "The text is too small to read on my laptop.", "My save disappeared after the update.",
            "I got stuck behind a rock and had to restart.", "The controls feel floaty and slow.", "The castle door never opens for me.",
            "I had no idea where to go after the cave.", "The menu doesn't work with a controller.", "The music cuts out after a few minutes.",
        };

        static readonly string[] Wishes =
        {
            "Can you add a dog I can pet?", "Please add a photo mode!", "It would be cool to have online multiplayer.", "I want more hats for my character.",
            "Add a level editor pls", "Could there be a dark mode?", "More levels please!!", "A speedrun timer would be awesome.",
            "Let me rename my farm animals.", "Can we get a New Game+ mode?", "I'd love a soundtrack I can buy.", "Give the cat a little scarf.",
        };

        static readonly string[] Praise =
        {
            "The art style is gorgeous.", "I laughed out loud at the shopkeeper.", "Best soundtrack I've heard this year.",
            "This made my commute so much better.", "The ending made me cry. Thank you.", "So satisfying when everything clicks.",
            "I played for five hours straight.", "My kids and I play this every evening.", "The puzzles are clever without being mean.",
            "Such a cozy little game.", "The jump feels perfect.", "I'm telling all my friends about this.",
        };

        readonly List<(string text, Bin bin)> _deck = new List<(string, Bin)>();
        int _index;
        int _correct;
        int _wrong;
        Label _quote;
        Label _who;
        Label _feedback;
        VisualElement _card;

        public FeedbackSort(MinigameSetup setup) : base(setup) { }

        int Target => 8 + 2 * Mathf.Clamp(Setup.Tier, 0, 2);

        public override string Name => "Feedback Sort";
        public override string HowTo => (Setup.OwnGame ? "Your players have a lot to say." : "Playtesters had a lot to say.") +
                                          " Sort each comment: a Problem to fix, a Wish to note down, or Praise to enjoy.";
        public override float Duration => 30f + 3f * Setup.Tier + SkillTime;
        public override string Status => $"{_index}/{Target} sorted · {_correct} right";

        public override float Score
        {
            get
            {
                float accuracy = Mathf.Clamp01((_correct - 0.5f * _wrong) / Target);
                return 0.85f * accuracy + (Solved ? 0.15f * TimeBonus : 0f);
            }
        }

        protected override void Build()
        {
            var pool = new List<(string, Bin)>();
            foreach (var t in Problems) pool.Add((t, Bin.Problem));
            foreach (var t in Wishes) pool.Add((t, Bin.Wish));
            foreach (var t in Praise) pool.Add((t, Bin.Praise));
            for (int i = 0; i < Target && pool.Count > 0; i++)
            {
                int k = Rng.Next(pool.Count);
                _deck.Add(pool[k]);
                pool.RemoveAt(k);
            }

            var layout = Ui.El("feedback");
            Root.Add(layout);
            _card = Ui.El("feedback__card");
            _card.Add(new Icon(Icons.Chat, 34f));
            _quote = Ui.Display("", "feedback__quote");
            _card.Add(_quote);
            _who = Ui.Text("", "feedback__who");
            _card.Add(_who);
            layout.Add(_card);
            _feedback = Ui.Text("", "feedback__result");
            layout.Add(_feedback);

            var bins = Ui.Row("feedback__bins");
            bins.Add(BinButton(Bin.Problem, Icons.Warning, "Problem", "fix it", "feedback__bin--problem"));
            bins.Add(BinButton(Bin.Wish, Icons.Bulb, "Wish", "note it", "feedback__bin--wish"));
            bins.Add(BinButton(Bin.Praise, Icons.Heart, "Praise", "enjoy it", "feedback__bin--praise"));
            layout.Add(bins);
            ShowCard();
        }

        VisualElement BinButton(Bin bin, string icon, string title, string detail, string classes)
        {
            var button = Ui.Clickable(() => Sort(bin), "feedback__bin " + classes);
            button.name = "bin-" + title.ToLowerInvariant();
            button.Add(new Icon(icon, 28f));
            var words = Ui.El("feedback__bin-words");
            words.Add(Ui.Display(title, "feedback__bin-title"));
            words.Add(Ui.Text(detail, "feedback__bin-detail"));
            button.Add(words);
            return button;
        }

        void ShowCard()
        {
            if (_index >= _deck.Count)
            {
                _quote.text = "That's everything!";
                _who.text = "";
                return;
            }
            _quote.text = "\"" + _deck[_index].text + "\"";
            _who.text = $"Playtester #{11 + _index * 7 % 89}";
        }

        void Sort(Bin bin)
        {
            if (IsOver || _index >= _deck.Count) return;
            bool right = _deck[_index].bin == bin;
            if (right) _correct++;
            else _wrong++;
            _feedback.text = right ? "Yes!" : $"Hmm, that was {Article(_deck[_index].bin)}.";
            _feedback.EnableInClassList("good", right);
            _feedback.EnableInClassList("bad", !right);
            _index++;
            if (_index >= _deck.Count) Solved = true;
            ShowCard();
        }

        static string Article(Bin bin) => bin == Bin.Problem ? "a problem" : bin == Bin.Wish ? "a wish" : "praise";

        /// <summary>Test helper: sorts the next cards correctly.</summary>
        public void SortForTest(int count)
        {
            for (int i = 0; i < count && _index < _deck.Count; i++) Sort(_deck[_index].bin);
        }
    }
}
