using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingPanel;

    public string gameScene = "Game";

    public Animator settingAnimator;
    private void Start()
    {
        settingPanel.SetActive(false);
    }

    public void Play()
    {
        AudioManager.Instance.PlayButton();

        // Nếu muốn New Game
        PlayerPrefs.DeleteKey("HasSave");

        SceneManager.LoadScene(gameScene);
    }

    public void Continue()
    {
        AudioManager.Instance.PlayButton();

        if (PlayerPrefs.GetInt("HasSave", 0) == 1)
        {
            SceneManager.LoadScene(gameScene);
        }
        else
        {
            Debug.Log("No Save");
        }
    }

    public void Quit()
    {
        AudioManager.Instance.PlayButton();

        Application.Quit();
    }

    public void OpenSetting()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        settingPanel.SetActive(true);

        settingAnimator.ResetTrigger("Close");
        settingAnimator.SetTrigger("Open");
    }


    public void CloseSetting()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        settingPanel.SetActive(false);
        settingAnimator.ResetTrigger("Open");
        settingAnimator.SetTrigger("Close");
    }
    public void HidePanel()
    {
        settingPanel.SetActive(false);
    }
}