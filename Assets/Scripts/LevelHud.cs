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

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverTitleText;
    [SerializeField] private TMP_Text gameOverMoneyText;
    [SerializeField] private TMP_Text gameOverTimeText;
    [SerializeField] private TMP_Text gameOverDistanceText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [SerializeField] private float trackLength = 220f;
    [SerializeField] private string levelLabel = "LEVEL 1";

    private PlayerCrowdManager playerCrowd;
    private Transform playerTransform;
    private bool settingsOpen;
    private bool shopOpen;
    private float runStartZ;
    private float runElapsedSeconds;
    private bool gameOverShown;

    private void Awake()
    {
        // Level scenes use this HUD without the legacy UIManager/menu flow.
        UIManager.SetExternalGameActive(true);
    }

    private void Start()
    {
        playerCrowd = FindFirstObjectByType<PlayerCrowdManager>();
        if (playerCrowd != null)
        {
            playerTransform = playerCrowd.transform;
            runStartZ = playerTransform.position.z;
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
        SetGameOverOpen(false);
        BindGameOverButtons();
        UpdateHud();
    }

    private void OnDestroy()
    {
        UIManager.SetExternalGameActive(false);
    }

    private void Update()
    {
        if (!gameOverShown && UIManager.IsGameActive)
        {
            runElapsedSeconds += Time.deltaTime;
        }

        UpdateHud();
        TryShowGameOver();
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

    private void BindGameOverButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RestartLevel);
            retryButton.onClick.AddListener(RestartLevel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ExitToMenu);
            mainMenuButton.onClick.AddListener(ExitToMenu);
        }
    }

    private void TryShowGameOver()
    {
        if (gameOverShown || playerTransform == null)
        {
            return;
        }

        if (playerCrowd == null || (!playerCrowd.IsGameOver && !playerCrowd.IsLeadFallGameOver))
        {
            return;
        }

        gameOverShown = true;
        Time.timeScale = 1f;
        UIManager.SetExternalGameActive(false);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

        int money = CurrencyWallet.Instance != null ? CurrencyWallet.Instance.RunCoins : 0;
        float distance = Mathf.Max(0f, playerTransform.position.z - runStartZ);

        if (gameOverTitleText != null) gameOverTitleText.text = "GAME OVER";
        if (gameOverMoneyText != null) gameOverMoneyText.text = $"<size=150%>{money}</size>\n<size=70%>MONEY</size>";
        if (gameOverTimeText != null) gameOverTimeText.text = $"<size=150%>{FormatRunTime(runElapsedSeconds)}</size>\n<size=70%>RUN TIME</size>";
        if (gameOverDistanceText != null) gameOverDistanceText.text = $"<size=150%>{distance:0}m</size>\n<size=70%>DISTANCE</size>";

        SetGameOverOpen(true);
    }

    private void SetGameOverOpen(bool open)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(open);
        }
    }

    private static string FormatRunTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
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
