using System.Security.Cryptography;
using System.Text;

namespace IndieStudio.Simulation
{
    public enum SyncDecision
    {
        /// <summary>No save anywhere: start a new studio.</summary>
        NoSave,
        /// <summary>Both copies are the same.</summary>
        UseLocal,
        /// <summary>Only this computer's copy moved on (or the cloud has none): play it and upload it.</summary>
        UseLocalAndUpload,
        /// <summary>Only the cloud copy moved on (played on another computer): download it.</summary>
        UseCloud,
        /// <summary>Both moved on since they last agreed: the player chooses.</summary>
        Conflict,
    }

    /// <summary>
    /// Which copy of the save to trust when the game starts with cloud saves (Steam Cloud): this computer's, the
    /// cloud's, or the player's choice. It compares both against the version they last agreed on, so whichever
    /// side moved on wins, and only a real conflict (both changed, e.g. offline play on one computer and online
    /// play on another) is put to the player. Nothing is ever deleted: the losing copy is kept as a backup.
    /// </summary>
    public static class SaveSync
    {
        public static SyncDecision Decide(string localHash, string cloudHash, string lastSyncedHash)
        {
            if (localHash == null && cloudHash == null) return SyncDecision.NoSave;
            if (cloudHash == null) return SyncDecision.UseLocalAndUpload;
            if (localHash == null) return SyncDecision.UseCloud;
            if (localHash == cloudHash) return SyncDecision.UseLocal;
            if (localHash == lastSyncedHash) return SyncDecision.UseCloud;
            if (cloudHash == lastSyncedHash) return SyncDecision.UseLocalAndUpload;
            return SyncDecision.Conflict;
        }

        /// <summary>A fingerprint of a save's bytes (SHA-256, hex).</summary>
        public static string Hash(byte[] data)
        {
            if (data == null) return null;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var text = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) text.Append(b.ToString("x2"));
                return text.ToString();
            }
        }
    }
}
