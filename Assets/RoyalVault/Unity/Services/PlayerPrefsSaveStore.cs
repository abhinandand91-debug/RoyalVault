using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>Saves a session as JSON in PlayerPrefs, one key per machine.</summary>
    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        private readonly string key;

        public PlayerPrefsSaveStore(string machineName)
        {
            key = "royalvault.save." + machineName.ToLowerInvariant().Replace(' ', '_');
        }

        public SaveData Load()
        {
            string json = PlayerPrefs.GetString(key, null);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
        }

        public void Save(SaveData data)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        /// <summary>Forgets the saved session (start over with the machine's start balance).</summary>
        public void Clear()
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}
