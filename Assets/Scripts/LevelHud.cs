using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelHud : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text settingsTitleText;
    [SerializeField] private TMP_Text soundButtonText;
    [SerializeField] private float trackLength = 220f;
    [SerializeField] private string levelLabel = "LEVEL 1";

    private Transform playerTransform;
    private bool settingsOpen;
    private bool shopOpen;

    private void Start()
    {
        PlayerCrowdManager crowd = FindFirstObjectByType<PlayerCrowdManager>();
        if (crowd != null)
        {
            playerTransform = crowd.transform;
        }

        if (levelText != null)
        {
            levelText.text = levelLabel;
        }

        if (settingsTitleText != null)
        {
            settingsTitleText.text = "Settings";
        }

        RefreshSoundLabel();
        SetSettingsOpen(false);
        SetShopOpen(false);
        UpdateHud();
    }

    private void Update()
    {
        UpdateHud();
    }

    public void ToggleSettings()
    {
        SetShopOpen(false);
        SetSettingsOpen(!settingsOpen);
    }

    public void CloseSettings()
    {
        SetSettingsOpen(false);
    }

    public void OpenShop()
    {
        SetSettingsOpen(false);
        SetShopOpen(true);
    }

    public void CloseShop()
    {
        SetShopOpen(false);
    }

    public void ToggleSound()
    {
        AudioListener.volume = AudioListener.volume > 0.01f ? 0f : 1f;
        RefreshSoundLabel();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void SetSettingsOpen(bool open)
    {
        settingsOpen = open;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(open);
        }

        RefreshPauseState();
    }

    private void SetShopOpen(bool open)
    {
        shopOpen = open;

        if (shopPanel != null)
        {
            shopPanel.SetActive(open);
        }

        RefreshPauseState();
    }

    private void RefreshPauseState()
    {
        Time.timeScale = settingsOpen || shopOpen ? 0f : 1f;
    }

    private void RefreshSoundLabel()
    {
        if (soundButtonText != null)
        {
            soundButtonText.text = AudioListener.volume > 0.01f ? "Sound: On" : "Sound: Off";
        }
    }

    private void UpdateHud()
    {
        if (currencyText != null)
        {
            int currency = CurrencyWallet.Instance != null ? CurrencyWallet.Instance.Currency : 0;
            currencyText.text = currency.ToString();
        }

        if (progressSlider != null)
        {
            float z = playerTransform != null ? playerTransform.position.z : 0f;
            progressSlider.value = Mathf.Clamp01(z / Mathf.Max(1f, trackLength));
        }
    }
}
