using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>Writing gigs: proofread a client's game text. Click every misspelled word, and only those.</summary>
    public sealed class TypoHunt : Minigame
    {
        static readonly string[] Texts =
        {
            "Welcome to Mossy Hollow, a sleepy village where every villager hides a secret. Grow strange vegetables, befriend the grumpy blacksmith and uncover the mystery of the glowing well before the harvest festival begins.",
            "The Ancient Lantern is a legendary artifact forged by forgotten wizards. Equip it to reveal hidden platforms and frighten lesser ghosts. Warning: the flame consumes courage every second it remains lit.",
            "Captain, the engines are overheating and the navigation computer keeps singing sea shanties. Should we restart the reactor, or keep drifting toward the suspiciously friendly asteroid?",
            "Build the coziest bakery in the kingdom! Knead dough, decorate cakes and keep your hungry customers smiling. Upgrade your ovens, hire talented apprentices and discover over forty delicious recipes.",
            "Patch notes: fixed a problem where the hero could walk through closed doors. Improved controller support, reduced loading times and removed the invisible wall near the waterfall. Thank you for your patience!",
            "You wake up in a quiet lighthouse with no memory of yesterday. The radio crackles with a familiar voice, and the tide is rising faster than it should. Every choice you make changes the ending.",
            "Every floor of the dungeon is generated from scratch. Collect powerful relics, combine magical scrolls and decide whether to fight the guardian now or explore one more dangerous room.",
            "Our tiny studio spent three years making this game. Please leave a review if you enjoyed it, because every honest comment helps independent developers keep creating strange and wonderful worlds.",
        };

        sealed class Word
        {
            public string Shown;
            public string Correct;
            public bool IsTypo;
            public bool Found;
            public Label View;
        }

        readonly List<Word> _words = new List<Word>();
        int _typos;
        int _found;
        int _wrong;

        public TypoHunt(MinigameSetup setup) : base(setup) { }

        public override string Name => "Typo Hunt";
        public override string HowTo => "Proofread the client's text. Click every misspelled word to fix it. Clicking a correct word costs you.";
        public override float Duration => 28f + 4f * Setup.Tier + SkillTime;
        public override string Status => $"{_found}/{_typos} typos fixed" + (_wrong > 0 ? $" · {_wrong} wrong" : "");

        public override float Score
        {
            get
            {
                if (_typos == 0) return 0f;
                float accuracy = Mathf.Clamp01((_found - 0.5f * _wrong) / _typos);
                return 0.85f * accuracy + (Solved ? 0.15f * TimeBonus : 0f);
            }
        }

        protected override void Build()
        {
            string text = Texts[Rng.Next(Texts.Length)];
            foreach (string token in text.Split(' '))
                _words.Add(new Word { Shown = token, Correct = token });

            var candidates = Enumerable.Range(0, _words.Count).Where(i => Letters(_words[i].Correct).Length >= 5).ToList();
            _typos = Mathf.Min(candidates.Count, 4 + Mathf.Clamp(Setup.Tier, 0, 2));
            for (int n = 0; n < _typos; n++)
            {
                int pick = Rng.Next(candidates.Count);
                var word = _words[candidates[pick]];
                candidates.RemoveAt(pick);
                word.Shown = Corrupt(word.Correct);
                word.IsTypo = true;
            }

            var page = Ui.El("typo");
            Root.Add(page);
            page.Add(Ui.Text("CLIENT_TEXT_FINAL_v3.docx", "typo__file"));
            var paragraph = Ui.El("typo__text");
            foreach (var word in _words)
            {
                var label = Ui.Text(word.Shown, "typo__word");
                var w = word;
                label.RegisterCallback<PointerDownEvent>(e => Click(w));
                word.View = label;
                paragraph.Add(label);
            }
            page.Add(paragraph);
        }

        static string Letters(string token) => new string(token.Where(char.IsLetter).ToArray());

        string Corrupt(string token)
        {
            // Keep punctuation where it was; only mangle the letters in the middle of the word.
            int start = 0, end = token.Length;
            while (start < end && !char.IsLetter(token[start])) start++;
            while (end > start && !char.IsLetter(token[end - 1])) end--;
            string core = token.Substring(start, end - start);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                string bad = Mangle(core);
                if (bad != core) return token.Substring(0, start) + bad + token.Substring(end);
            }
            return token.Substring(0, start) + core + core[core.Length - 1] + token.Substring(end);
        }

        string Mangle(string word)
        {
            var chars = word.ToCharArray();
            int i = 1 + Rng.Next(Mathf.Max(1, word.Length - 2));
            switch (Rng.Next(4))
            {
                case 0: // swap two neighbors
                    if (i + 1 < chars.Length)
                    {
                        (chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
                        return new string(chars);
                    }
                    return word;
                case 1: // doubled letter
                    return word.Substring(0, i) + word[i] + word.Substring(i);
                case 2: // missing letter
                    return word.Remove(i, 1);
                default: // wrong vowel
                    const string vowels = "aeiou";
                    for (int k = 1; k < chars.Length; k++)
                    {
                        int at = (i + k) % chars.Length;
                        if (at == 0 || vowels.IndexOf(chars[at]) < 0) continue;
                        chars[at] = vowels[(vowels.IndexOf(chars[at]) + 1 + Rng.Next(4)) % vowels.Length];
                        return new string(chars);
                    }
                    return word;
            }
        }

        void Click(Word word)
        {
            if (IsOver || word.Found) return;
            if (word.IsTypo)
            {
                word.Found = true;
                _found++;
                word.View.text = word.Correct;
                word.View.AddToClassList("typo__word--fixed");
                if (_found == _typos) Solved = true;
            }
            else
            {
                _wrong++;
                word.View.AddToClassList("typo__word--wrong");
                word.View.schedule.Execute(() => word.View.RemoveFromClassList("typo__word--wrong")).StartingIn(450);
            }
        }

        /// <summary>Test helper: fixes a number of the typos.</summary>
        public void FixForTest(int count)
        {
            foreach (var word in _words.Where(w => w.IsTypo && !w.Found).Take(count).ToList()) Click(word);
        }
    }
}
