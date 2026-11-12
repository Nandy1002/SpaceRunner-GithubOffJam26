using UnityEngine;
using UnityEngine.UI;

namespace SpaceRunner
{
    public sealed class SettingsPage : MonoBehaviour
    {
        [SerializeField] private Slider volume, sensitivity;
        [SerializeField] private Text volumeValue, sensitivityValue;
        public void Initialize(PlayerSettings settings)
        {
            volume.SetValueWithoutNotify(settings.MasterVolume);
            sensitivity.SetValueWithoutNotify(settings.LookSensitivity);
            volumeValue.text = $"{settings.MasterVolume:P0}";
            sensitivityValue.text = $"{settings.LookSensitivity / 0.12f:0.0}x";
            volume.onValueChanged.AddListener(value => { settings.SetVolume(value); volumeValue.text = $"{value:P0}"; });
            sensitivity.onValueChanged.AddListener(value => { settings.SetSensitivity(value); sensitivityValue.text = $"{value / 0.12f:0.0}x"; });
        }
    }
}
