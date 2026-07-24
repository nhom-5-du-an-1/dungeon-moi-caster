using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public Toggle toggleMusic;
    public Toggle toggleSound;
    public Slider volumeSlider;

    void Start()
    {
        toggleMusic.isOn = AudioManager.Instance.MusicOn();
        toggleSound.isOn = AudioManager.Instance.SoundOn();
        volumeSlider.value = AudioManager.Instance.GetVolume();

        toggleMusic.onValueChanged.AddListener(AudioManager.Instance.ToggleMusic);
        toggleSound.onValueChanged.AddListener(AudioManager.Instance.ToggleSound);
        volumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolume);
    }
}