using UnityEngine;

namespace GameCore
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Library")]
        [SerializeField] private AudioLibrary audioLibrary;

        [Header("Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        private const string MasterVolumeKey = "MasterVolume";
        private const string BgmVolumeKey = "BgmVolume";
        private const string SfxVolumeKey = "SfxVolume";

        protected override void Awake()
        {
            base.Awake();

            CreateSourcesIfNeeded();
            LoadVolumeSettings();
            ApplyVolume();
        }

        private void CreateSourcesIfNeeded()
        {
            if (bgmSource == null)
            {
                GameObject bgmObject = new GameObject("BGM Source");
                bgmObject.transform.SetParent(transform);
                bgmSource = bgmObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }

            if (sfxSource == null)
            {
                GameObject sfxObject = new GameObject("SFX Source");
                sfxObject.transform.SetParent(transform);
                sfxSource = sfxObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
            }
        }

        public void PlayBGM(string id, bool restartIfSame = false)
        {
            AudioClip clip = audioLibrary.GetClip(id);

            if (clip == null)
                return;

            if (!restartIfSame && bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        public void PlaySFX(string id)
        {
            AudioClip clip = audioLibrary.GetClip(id);

            if (clip == null)
                return;

            sfxSource.PlayOneShot(clip);
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolume();
            SaveVolumeSettings();
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyVolume();
            SaveVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolume();
            SaveVolumeSettings();
        }

        private void ApplyVolume()
        {
            bgmSource.volume = masterVolume * bgmVolume;
            sfxSource.volume = masterVolume * sfxVolume;
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        }
    }
}