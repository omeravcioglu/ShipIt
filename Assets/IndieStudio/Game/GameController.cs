using System;
using IndieStudio.Game.UI;
using IndieStudio.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieStudio.Game
{
    /// <summary>
    /// Owns the simulation, the UI and the platform (Steam or offline). There is no clock: time moves only when
    /// the player spends a slot of the day (morning, afternoon, evening) or goes to sleep (Design/02 §4). All
    /// rules live in the engine-free IndieStudio.Simulation assembly. The game saves itself shortly after every
    /// change (to the cloud as well, when the platform has one) and unlocks achievements as they're earned.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameController : MonoBehaviour
    {
        /// <summary>Seconds after a change before the autosave writes (changes come in bursts).</summary>
        const float AutosaveDelay = 1f;

        public ulong Seed = 0;
        /// <summary>The full game or the free demo (the real game passes <see cref="GameEdition.Current"/>).</summary>
        public Edition Edition = Edition.Full;
        /// <summary>Where saves live; null keeps everything in memory (tests).</summary>
        public string SaveFolder;
        /// <summary>The platform (Steam or offline). The real game passes <see cref="Platforms.Shared"/>; without one, a forgetful offline platform is used (tests).</summary>
        public IPlatform PlatformOverride;

        StudioUI _ui;
        bool _dirty;
        float _dirtySince;

        public GameSimulation Sim { get; private set; }
        public StudioUI Ui => _ui;
        public GameSettings Settings { get; private set; }
        public SaveStore Saves { get; private set; }
        public IPlatform Platform { get; private set; }
        public AchievementTracker Achievements { get; private set; }
        /// <summary>Set while the player hasn't chosen between two different saves (this computer's and the cloud's).</summary>
        public SaveConflict PendingConflict { get; private set; }
        /// <summary>True when this session's studio came down from the cloud.</summary>
        public bool LoadedFromCloud { get; private set; }
        /// <summary>True when this session continued a saved studio.</summary>
        public bool ResumedFromSave { get; private set; }
        /// <summary>What went wrong while loading, if anything (shown to the player once).</summary>
        public string LoadProblem { get; set; }
        public DateTime? LastSaved { get; private set; }

        /// <summary>Raised after every player action that changed the game.</summary>
        public event Action Changed;

        void Awake()
        {
            // Money and numbers read the same on every machine ("$5,000", "1.5"), whatever the OS locale.
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            Settings = new GameSettings(SaveFolder != null);
            Platform = PlatformOverride ?? new OfflinePlatform(false);
            Achievements = new AchievementTracker(Platform) { Enabled = Edition == Edition.Full };
            if (SaveFolder != null) Saves = new SaveStore(SaveFolder, Platform.Cloud, GameEdition.SaveFileName(Edition));

            GameSimulation loaded = null;
            if (Saves != null && Saves.Exists)
            {
                var opening = Saves.Open();
                loaded = opening.Sim;
                PendingConflict = opening.Conflict;
                LoadedFromCloud = opening.FromCloud;
                if (loaded != null) LoadProblem = opening.Problem;
                else if (opening.Problem != null) LoadProblem = opening.Problem + " A new studio was started.";
                if (loaded != null && loaded.State.Edition != Edition)
                {
                    // A studio from the other edition (the demo's save in the full game, or the other way round): start fresh.
                    loaded = null;
                    PendingConflict = null;
                    LoadProblem = null;
                }
            }
            if (loaded != null)
            {
                Attach(loaded);
                ResumedFromSave = true;
            }
            else NewGame(Seed != 0 ? Seed : (ulong)DateTime.Now.Ticks);
            Achievements.Check(Sim.State, true); // what this studio earned before is sent quietly
        }

        void Start()
        {
            var document = GetComponent<UIDocument>();
            _ui = new StudioUI(this, document.rootVisualElement);
            _ui.Refresh();
            _ui.OnStarted();
        }

        /// <summary>Starts a new studio (replacing any save once it autosaves).</summary>
        public void NewGame(ulong seed)
        {
            Seed = seed;
            Attach(new GameSimulation(seed, Edition));
            ResumedFromSave = false;
            LoadedFromCloud = false;
            Achievements?.Reset();
            _ui?.OnNewGame();
            NotifyChanged();
        }

        /// <summary>
        /// Answers the cloud-save question: keep the studio from the cloud (played on another computer) or the one
        /// on this computer. The other is kept beside the save file.
        /// </summary>
        public void ResolveConflict(bool keepCloud)
        {
            var conflict = PendingConflict;
            if (conflict == null) return;
            PendingConflict = null;
            Saves.Resolve(conflict, keepCloud);
            if (keepCloud)
            {
                Attach(conflict.Cloud);
                LoadedFromCloud = true;
                Achievements.Reset();
                Achievements.Check(Sim.State, true);
                _ui?.OnNewGame();
            }
            NotifyChanged();
        }

        void Attach(GameSimulation sim)
        {
            if (Sim != null) Sim.MessagePosted -= OnMessagePosted;
            Sim = sim;
            Seed = sim.State.Seed;
            Sim.MessagePosted += OnMessagePosted;
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
            _ui?.Refresh();
            Achievements?.Check(Sim?.State, false);
            if (!_dirty) _dirtySince = Time.unscaledTime;
            _dirty = true;
        }

        /// <summary>Writes the save now (normally the autosave does this a moment after each change).</summary>
        public void SaveNow()
        {
            if (PendingConflict != null) return; // nothing is saved until the player has picked a studio
            _dirty = false;
            if (Saves == null || Sim == null) return;
            try
            {
                Saves.Write(Sim);
                LastSaved = DateTime.Now;
            }
            catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Indie Studio couldn't save: " + e.Message);
            }
        }

        // The autosave (and the platform's callbacks) are the only things that look at the frame clock; the game never does.
        void Update()
        {
            Platform?.Tick();
            if (_dirty && Time.unscaledTime - _dirtySince >= AutosaveDelay) SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _dirty) SaveNow();
        }

        void OnApplicationQuit()
        {
            if (_dirty) SaveNow();
        }

        void OnDestroy()
        {
            if (_dirty) SaveNow();
        }

        void OnMessagePosted(Message message) => _ui?.OnMessagePosted(message);
    }
}
