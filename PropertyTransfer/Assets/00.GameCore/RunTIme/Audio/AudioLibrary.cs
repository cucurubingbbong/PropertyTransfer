using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [CreateAssetMenu(menuName = "GameCore/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioEntry> clips = new();

        private Dictionary<string, AudioClip> clipMap;

        public AudioClip GetClip(string id)
        {
            if (clipMap == null)
                BuildMap();

            if (clipMap.TryGetValue(id, out AudioClip clip))
                return clip;

            Debug.LogWarning($"[AudioLibrary] 찾을 수 없는 오디오 ID: {id}");
            return null;
        }

        private void BuildMap()
        {
            clipMap = new Dictionary<string, AudioClip>();

            foreach (AudioEntry entry in clips)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || entry.Clip == null)
                    continue;

                if (clipMap.ContainsKey(entry.Id))
                {
                    Debug.LogWarning($"[AudioLibrary] 중복된 오디오 ID: {entry.Id}");
                    continue;
                }

                clipMap.Add(entry.Id, entry.Clip);
            }
        }
    }

    [Serializable]
    public class AudioEntry
    {
        public string Id;
        public AudioClip Clip;
    }
}