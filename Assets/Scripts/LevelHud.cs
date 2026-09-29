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
    [SerializeField] private SfxPlayer sfxPlayer;

    [SerializeField] private float trackLength = 220f;
    [SerializeField] private string levelLabel = "LEVEL 1";

    private PlayerCrowdManager playerCrowd;
    private Transform playerTransform;
    private bool settingsOpen;
    private bool shopOpen;
    private float runStartZ;
    private float runElapsedSeconds;
    private bool gameOverShown;
    private bool transitionInProgress;

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
        EnsureGameOverUi();
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
        if (transitionInProgress) return;
        Time.timeScale = 1f;
        transitionInProgress = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMenu()
    {
        if (transitionInProgress) return;
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Menu.unity") < 0)
        {
            Debug.LogError("LevelHud: Menu is not available in Build Settings.", this);
            return;
        }

        Time.timeScale = 1f;
        transitionInProgress = true;
        SceneManager.LoadScene("Menu");
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

    private void EnsureGameOverUi()
    {
        if (gameOverPanel != null && gameOverTitleText != null && gameOverMoneyText != null &&
            gameOverTimeText != null && gameOverDistanceText != null && retryButton != null &&
            mainMenuButton != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogWarning("LevelHud: no parent Canvas found; the game-over screen cannot be created.", this);
            return;
        }

        if (gameOverPanel == null)
        {
            Transform existingPanel = canvas.transform.Find("Game Over Screen");
            gameOverPanel = existingPanel != null ? existingPanel.gameObject : null;
        }

        if (gameOverPanel == null)
        {
            GameObject authoredPanel = Resources.Load<GameObject>("UI/GameOverScreen");
            if (authoredPanel != null)
            {
                gameOverPanel = Instantiate(authoredPanel, canvas.transform, false);
                gameOverPanel.name = "Game Over Screen";
            }
        }

        if (gameOverPanel == null)
        {
            gameOverPanel = new GameObject("Game Over Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameOverPanel.transform.SetParent(canvas.transform, false);
        }

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        Image panelImage = gameOverPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.015f, 0.04f, 0.08f, 0.94f);
            panelImage.raycastTarget = true;
        }

        Transform panelTransform = gameOverPanel.transform;
        gameOverTitleText ??= FindChildComponent<TMP_Text>(panelTransform, "Game Over Title");
        gameOverMoneyText ??= FindChildComponent<TMP_Text>(panelTransform, "Game Over Money");
        gameOverTimeText ??= FindChildComponent<TMP_Text>(panelTransform, "Game Over Time");
        gameOverDistanceText ??= FindChildComponent<TMP_Text>(panelTransform, "Game Over Distance");
        retryButton ??= FindChildComponent<Button>(panelTransform, "Retry Button");
        retryButton ??= FindChildComponent<Button>(panelTransform, "Restart Button");
        mainMenuButton ??= FindChildComponent<Button>(panelTransform, "Main Menu Button");

        gameOverTitleText ??= CreateGameOverText(
            panelTransform, "Game Over Title", "GAME OVER", 48f,
            TextAlignmentOptions.Center, new Vector2(0f, 150f), new Vector2(560f, 80f));
        gameOverMoneyText ??= CreateGameOverText(
            panelTransform, "Game Over Money", "0\nMONEY", 26f,
            TextAlignmentOptions.Center, new Vector2(-190f, 35f), new Vector2(180f, 90f));
        gameOverTimeText ??= CreateGameOverText(
            panelTransform, "Game Over Time", "00:00\nRUN TIME", 26f,
            TextAlignmentOptions.Center, new Vector2(0f, 35f), new Vector2(180f, 90f));
        gameOverDistanceText ??= CreateGameOverText(
            panelTransform, "Game Over Distance", "0m\nDISTANCE", 26f,
            TextAlignmentOptions.Center, new Vector2(190f, 35f), new Vector2(180f, 90f));

        retryButton ??= CreateGameOverButton(
            panelTransform, "Restart Button", "RESTART", new Vector2(-120f, -100f),
            new Color(0.12f, 0.62f, 1f, 1f));
        mainMenuButton ??= CreateGameOverButton(
            panelTransform, "Main Menu Button", "MAIN MENU", new Vector2(120f, -100f),
            new Color(0.25f, 0.3f, 0.38f, 1f));

        gameOverPanel.SetActive(false);
        gameOverPanel.transform.SetAsLastSibling();
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null) return null;

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.name == childName)
            {
                return components[i];
            }
        }

        return null;
    }

    private static TMP_Text CreateGameOverText(
        Transform parent,
        string objectName,
        string initialText,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static Button CreateGameOverButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition,
        Color buttonColor)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(210f, 58f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(buttonColor, Color.black, 0.15f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TMP_Text labelText = CreateGameOverText(
            buttonObject.transform, objectName + " Label", label, 22f,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;

        return button;
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

        if (sfxPlayer != null)
        {
            sfxPlayer.RegisterButton(retryButton);
            sfxPlayer.RegisterButton(mainMenuButton);
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
        if (CurrencyWallet.Instance != null)
        {
            CurrencyWallet.Instance.BankRunCoins();
        }
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
