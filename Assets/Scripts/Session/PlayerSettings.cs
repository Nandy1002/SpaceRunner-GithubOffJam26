using UnityEngine;

namespace SpaceRunner
{
    /// <summary>The menu writes settings; gameplay reads them without depending on UI.</summary>
    public sealed class PlayerSettings : MonoBehaviour
    {
        private const string VolumeKey = "SpaceRunner.Audio.MasterVolume";
        private const string SensitivityKey = "SpaceRunner.Input.LookSensitivity";
        public float MasterVolume { get; private set; }
        public float LookSensitivity { get; private set; }

        private void Awake()
        {
            SetVolume(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
            SetSensitivity(PlayerPrefs.GetFloat(SensitivityKey, 0.12f));
        }

        public void SetVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
        }

        public void SetSensitivity(float value) => LookSensitivity = Mathf.Clamp(value, 0.03f, 0.4f);

        public void Save()
        {
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(SensitivityKey, LookSensitivity);
            PlayerPrefs.Save();
        }
    }
}
