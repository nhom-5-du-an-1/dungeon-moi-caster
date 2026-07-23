using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClick;

    private bool musicOn;
    private bool soundOn;
    private float volume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
    }

    private void Start()
    {
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;

        musicSource.volume = volume;
        sfxSource.volume = volume;

        if (musicOn)
        {
            musicSource.Play();
        }
    }

    // ====== Âm thanh nút ======
    public void PlayButton()
    {
        if (soundOn && buttonClick != null)
        {
            sfxSource.PlayOneShot(buttonClick);
        }
    }

    // ====== Bật/Tắt nhạc nền ======
    public void ToggleMusic(bool value)
    {
        musicOn = value;

        if (musicOn)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }

        PlayerPrefs.SetInt("Music", musicOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ====== Bật/Tắt hiệu ứng ======
    public void ToggleSound(bool value)
    {
        soundOn = value;

        PlayerPrefs.SetInt("Sound", soundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ====== Âm lượng tổng ======
    public void SetVolume(float value)
    {
        volume = value;

        musicSource.volume = volume;
        sfxSource.volume = volume;

        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
    }

    // ====== Load cài đặt ======
    private void LoadSettings()
    {
        musicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        soundOn = PlayerPrefs.GetInt("Sound", 1) == 1;
        volume = PlayerPrefs.GetFloat("Volume", 1f);
    }

    // ====== Trả giá trị cho SettingManager ======
    public bool MusicOn()
    {
        return musicOn;
    }

    public bool SoundOn()
    {
        return soundOn;
    }

    public float GetVolume()
    {
        return volume;
    }
}