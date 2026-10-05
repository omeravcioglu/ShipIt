using System;
using System.IO;
using IndieStudio.Simulation;
using UnityEngine;

namespace IndieStudio.Game
{
    /// <summary>
    /// Player preferences (not part of a save game). Stored in PlayerPrefs for the real game; tests keep them in
    /// memory so they never touch the player's settings.
    /// </summary>
    public sealed class GameSettings
    {
        const string RelaxedKey = "IndieStudio.RelaxedMinigames";
        readonly bool _persistent;
        bool _relaxed;

        public GameSettings(bool persistent)
        {
            _persistent = persistent;
            _relaxed = persistent && PlayerPrefs.GetInt(RelaxedKey, 0) == 1;
        }

        /// <summary>Minigames without timers, scored on accuracy (Design/18 §9).</summary>
        public bool RelaxedMinigames
        {
            get => _relaxed;
            set
            {
                _relaxed = value;
                if (!_persistent) return;
                PlayerPrefs.SetInt(RelaxedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }

    /// <summary>
    /// The save file on disk, and its copy in the cloud (Steam Cloud) when the platform has one. Writes go to a
    /// temporary file first and the previous save is kept as a backup, so a crash mid-write never loses the
    /// studio. With cloud saves, each save is also uploaded, and opening the game reconciles the two copies
    /// (<see cref="SaveSync"/>): the copy that moved on wins, a real conflict is put to the player, and the
    /// copy that loses is kept beside the save, never deleted.
    /// </summary>
    public sealed class SaveStore
    {
        public const string FileName = "studio.isav";
        readonly ICloudStorage _cloud;

        public SaveStore(string folder, ICloudStorage cloud = null)
        {
            Folder = folder;
            _cloud = cloud;
        }

        public string Folder { get; }
        public string FilePath => Path.Combine(Folder, FileName);
        string BackupPath => FilePath + ".bak";
        string TempPath => FilePath + ".tmp";
        /// <summary>The fingerprint of the version this computer and the cloud last agreed on.</summary>
        string SyncPath => FilePath + ".sync";
        /// <summary>Where the copy that lost a conflict is kept.</summary>
        public string LocalCopyPath => FilePath + ".local-copy";
        public string CloudCopyPath => FilePath + ".cloud-copy";

        public bool CloudEnabled => _cloud != null && _cloud.IsEnabled;
        /// <summary>True when the platform has cloud saves (even if the player turned them off).</summary>
        public bool HasCloud => _cloud != null;
        /// <summary>When a save last reached the cloud this session.</summary>
        public DateTime? LastUploaded { get; private set; }
        /// <summary>Set when the last upload failed (the save is still safe on this computer).</summary>
        public string CloudProblem { get; private set; }

        public bool Exists => File.Exists(FilePath) || File.Exists(BackupPath) || File.Exists(TempPath) || (CloudEnabled && _cloud.Exists(FileName));

        public void Write(GameSimulation sim)
        {
            byte[] data = Serialize(sim);
            WriteLocal(data);
            Upload(data);
        }

        static byte[] Serialize(GameSimulation sim)
        {
            using (var stream = new MemoryStream())
            {
                sim.Save(stream);
                return stream.ToArray();
            }
        }

        void WriteLocal(byte[] data)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(TempPath, data);
            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, BackupPath, true);
                File.Delete(FilePath);
            }
            File.Move(TempPath, FilePath);
        }

        void Upload(byte[] data)
        {
            if (!CloudEnabled || data == null) return;
            if (_cloud.Write(FileName, data))
            {
                RememberSynced(data);
                LastUploaded = DateTime.Now;
                CloudProblem = null;
            }
            else CloudProblem = "Your last save didn't reach the cloud. It's safe on this computer and will be uploaded with the next one.";
        }

        void RememberSynced(byte[] data)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(SyncPath, SaveSync.Hash(data));
            }
            catch (IOException)
            {
            }
        }

        string LastSynced()
        {
            try
            {
                return File.Exists(SyncPath) ? File.ReadAllText(SyncPath).Trim() : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// Opens the studio to play: this computer's save (recovering from a damaged one) reconciled with the one
        /// in the cloud. <see cref="SaveOpening.Sim"/> is null when there is no save at all.
        /// </summary>
        public SaveOpening Open()
        {
            var opening = new SaveOpening();
            var local = ReadLocal(out byte[] localBytes, out string problem);
            opening.Problem = problem;

            GameSimulation cloud = null;
            byte[] cloudBytes = null;
            if (CloudEnabled && _cloud.Exists(FileName))
            {
                cloudBytes = _cloud.Read(FileName);
                cloud = cloudBytes != null ? TryParse(cloudBytes, out _) : null;
                if (cloud == null)
                {
                    cloudBytes = null;
                    opening.Problem = Join(opening.Problem, local != null
                        ? "The save in the cloud couldn't be read, so this computer's save replaces it."
                        : "The save in the cloud couldn't be read.");
                }
            }

            switch (SaveSync.Decide(SaveSync.Hash(localBytes), SaveSync.Hash(cloudBytes), LastSynced()))
            {
                case SyncDecision.UseLocal:
                    opening.Sim = local;
                    RememberSynced(localBytes);
                    break;
                case SyncDecision.UseLocalAndUpload:
                    opening.Sim = local;
                    Upload(localBytes);
                    break;
                case SyncDecision.UseCloud:
                    WriteLocal(cloudBytes);
                    RememberSynced(cloudBytes);
                    opening.Sim = cloud;
                    opening.FromCloud = true;
                    break;
                case SyncDecision.Conflict:
                    opening.Sim = local; // playable behind the question; nothing is saved until it's answered
                    opening.Conflict = new SaveConflict
                    {
                        Local = local, Cloud = cloud, LocalBytes = localBytes, CloudBytes = cloudBytes,
                        LocalSaved = File.Exists(FilePath) ? File.GetLastWriteTime(FilePath) : (DateTime?)null,
                        CloudSaved = _cloud.Timestamp(FileName),
                    };
                    break;
            }
            return opening;
        }

        /// <summary>Settles a conflict: the chosen copy becomes the save everywhere, and the other is kept beside it.</summary>
        public void Resolve(SaveConflict conflict, bool keepCloud)
        {
            Directory.CreateDirectory(Folder);
            if (keepCloud)
            {
                File.WriteAllBytes(LocalCopyPath, conflict.LocalBytes);
                WriteLocal(conflict.CloudBytes);
                RememberSynced(conflict.CloudBytes);
            }
            else
            {
                File.WriteAllBytes(CloudCopyPath, conflict.CloudBytes);
                Upload(conflict.LocalBytes);
            }
        }

        /// <summary>
        /// Loads this computer's save. A damaged save is moved aside and the one before it is loaded instead;
        /// <paramref name="problem"/> says what happened (null when all went well).
        /// </summary>
        GameSimulation ReadLocal(out byte[] bytes, out string problem)
        {
            problem = null;
            var sim = TryLoad(FilePath, out bytes, out string error);
            if (sim != null) return sim;
            if (error != null)
            {
                problem = $"Your last save couldn't be read. {error}";
                TryMoveAside(FilePath);
            }

            // A write interrupted between replacing the save and moving the new one in leaves it in the temp file.
            sim = TryLoad(TempPath, out bytes, out _) ?? TryLoad(BackupPath, out bytes, out error);
            if (sim != null)
            {
                if (problem != null) problem += " The save before it was loaded instead.";
                return sim;
            }
            if (error != null && problem == null) problem = $"Your save couldn't be read. {error}";
            bytes = null;
            return null;
        }

        static GameSimulation TryLoad(string path, out byte[] bytes, out string error)
        {
            bytes = null;
            error = null;
            if (!File.Exists(path)) return null;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                error = e.Message;
                return null;
            }
            var sim = TryParse(bytes, out error);
            if (sim == null) bytes = null;
            return sim;
        }

        static GameSimulation TryParse(byte[] bytes, out string error)
        {
            error = null;
            try
            {
                using (var stream = new MemoryStream(bytes)) return GameSimulation.Load(stream);
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException)
            {
                error = e.Message;
                return null;
            }
        }

        static string Join(string first, string second) => string.IsNullOrEmpty(first) ? second : first + " " + second;

        public void Delete()
        {
            foreach (var path in new[] { FilePath, BackupPath, TempPath, SyncPath })
                if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Keeps a damaged save for inspection instead of overwriting it with the next autosave.</summary>
        static void TryMoveAside(string path)
        {
            try
            {
                string broken = path + ".broken";
                if (File.Exists(broken)) File.Delete(broken);
                File.Move(path, broken);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>What opening the save found.</summary>
    public sealed class SaveOpening
    {
        /// <summary>The studio to play, or null for a new one.</summary>
        public GameSimulation Sim;
        /// <summary>Something the player should hear about (a damaged save that was replaced), or null.</summary>
        public string Problem;
        /// <summary>The save came down from the cloud (it was played on another computer).</summary>
        public bool FromCloud;
        /// <summary>Set when both copies changed since they last agreed: the player picks one.</summary>
        public SaveConflict Conflict;
    }

    /// <summary>Two different studios: the one on this computer and the one in the cloud.</summary>
    public sealed class SaveConflict
    {
        public GameSimulation Local;
        public GameSimulation Cloud;
        public byte[] LocalBytes;
        public byte[] CloudBytes;
        public DateTime? LocalSaved;
        public DateTime? CloudSaved;
    }
}
