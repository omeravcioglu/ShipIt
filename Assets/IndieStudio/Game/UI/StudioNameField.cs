using IndieStudio.Simulation;
using UnityEngine.UIElements;

namespace IndieStudio.Game.UI
{
    /// <summary>A text box for the studio's name, with a dice button that suggests one (the tutorial and the menu).</summary>
    public sealed class StudioNameField : VisualElement
    {
        readonly StudioUI _studio;
        readonly TextField _field;
        string _shown;
        int _attempt;

        public StudioNameField(StudioUI studio)
        {
            _studio = studio;
            AddToClassList("studio-name");
            _shown = studio.Sim.State.StudioName;
            _field = new TextField { maxLength = GameSimulation.StudioNameMaxLength, value = _shown };
            _field.name = "studio-name";
            _field.AddToClassList("studio-name__input");
            Add(_field);
            var dice = Ui.IconBtn(Icons.Dice, "", Reroll, "icon-btn studio-name__dice", 22f);
            dice.name = "studio-name-dice";
            studio.Tooltip.Attach(dice, () => "Suggest a name");
            Add(dice);
        }

        public string Value
        {
            get => _field.value;
            set => _field.value = value;
        }

        /// <summary>Gives the studio the name in the box, if the player changed it (blank keeps the old one).</summary>
        public void Apply()
        {
            if (_field.value != _shown) _studio.Sim.NameStudio(_field.value);
        }

        /// <summary>Picks up a name given elsewhere (the sign, the menu) while the box is untouched.</summary>
        public void Sync()
        {
            string name = _studio.Sim.State.StudioName;
            if (_field.value != _shown || name == _shown) return;
            _shown = name;
            _field.SetValueWithoutNotify(name);
        }

        void Reroll() => _field.value = _studio.Sim.SuggestStudioName(_attempt++);
    }
}
