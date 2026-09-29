using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private static bool gameActive;
    private static bool externalGameActive;

    public static bool IsGameActive
    {
        get => Instance == null ? externalGameActive : gameActive;
        private set => gameActive = value;
    }

    public static void SetExternalGameActive(bool active)
    {
        if (Instance == null)
        {
            externalGameActive = active;
        }
    }

    [Serializable]
    public class LevelEntry
    {
        public string displayName;
        public string sceneName;
        public Button button;
    }

    private enum MenuPanel
    {
        Main,
        LevelSelect,
        Settings,
        Help,
        SkinShop,
        Loading,
        Error,
        ExitInfo
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject youWinPanel;

    [Header("HUD Elements")]
    [SerializeField] private TMP_Text cloneCountText;
    [SerializeField] private Slider progressBar;

    [Header("Game Over Elements")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button restartButton;

    [Header("You Win Elements")]
    [SerializeField] private TMP_Text winScoreText;

    [Header("Menu Panels")]
    [SerializeField] private bool menuMode = true;
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private GameObject exitInfoPanel;
    [SerializeField] private GameObject skinShopPanel;

    [Header("Menu Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button skinShopButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button soundToggleButton;
    [SerializeField] private Button levelSelectBackButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button helpBackButton;
    [SerializeField] private Button errorRetryButton;
    [SerializeField] private Button errorBackButton;
    [SerializeField] private Button exitInfoCloseButton;

    [Header("Menu Text")]
    [SerializeField] private TMP_Text selectedLevelText;
    [SerializeField] private TMP_Text soundToggleText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Slider loadingProgress;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_Text exitInfoText;

    [Header("Level Selection")]
    [SerializeField] private List<LevelEntry> levelEntries = new List<LevelEntry>();
    [SerializeField] private int selectedLevelIndex;

    [Header("Skin Shop")]
    [SerializeField] private SkinShopController skinShopController;

    [Header("Level Progress Settings")]
    [SerializeField] private float trackLength = 150f;

    private PlayerCrowdManager playerCrowd;
    private Transform playerTransform;
    private bool gameOverTriggered;
    private bool menuBindingsBound;
    private bool sceneLoadInProgress;
    private string lastRequestedScene;
    private Coroutine sceneLoadRoutine;
    private MenuPanel currentMenuPanel = MenuPanel.Main;
    private InputAction menuCancelAction;
    private int lastMenuScreenWidth = -1;
    private int lastMenuScreenHeight = -1;

    public bool IsMenuMode => menuMode;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        IsGameActive = false;
        externalGameActive = false;
    }

    private void Start()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (menuMode ||
            string.Equals(activeSceneName, menuSceneName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(activeSceneName, "Menu", StringComparison.OrdinalIgnoreCase))
        {
            menuMode = true;
            InitializeMenu();
            return;
        }

        playerCrowd = FindFirstObjectByType<PlayerCrowdManager>();
        if (playerCrowd != null)
        {
            playerTransform = playerCrowd.transform;
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (youWinPanel != null) youWinPanel.SetActive(false);

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        ConfigureLayoutResponsive();
        FindTrackLengthDynamic();
    }

    private void InitializeMenu()
    {
        Time.timeScale = 1f;
        IsGameActive = false;
        EnsureDefaultLevelEntries();
        BindMenuButtons();
        BindMenuCancelAction();
        SetMenuPanel(MenuPanel.Main);
        UpdateSoundLabel();
        SetMenuInteractable(true);
        ConfigureMenuLayoutResponsive();

        if (EventSystem.current != null)
        {
            GameObject first = startButton != null ? startButton.gameObject : levelSelectButton != null ? levelSelectButton.gameObject : null;
            if (first != null)
            {
                EventSystem.current.SetSelectedGameObject(first);
            }
        }
    }

    private void EnsureDefaultLevelEntries()
    {
        if (levelEntries == null)
        {
            levelEntries = new List<LevelEntry>();
        }

        string[] names = { "RAMP-UP RUN", "MOMENTUM TRAPWORKS", "PULSEBOUND FOUNDRY", "VAULTLINE CITADEL", "FRACTURE RELAY" };
        for (int i = levelEntries.Count; i < 5; i++)
        {
            levelEntries.Add(new LevelEntry
            {
                displayName = "LEVEL " + (i + 1) + " • " + names[i],
                sceneName = "Level" + (i + 1)
            });
        }

        for (int i = 0; i < levelEntries.Count; i++)
        {
            LevelEntry entry = levelEntries[i];
            if (entry == null)
            {
                entry = new LevelEntry();
                levelEntries[i] = entry;
            }

            if (string.IsNullOrWhiteSpace(entry.sceneName))
            {
                entry.sceneName = "Level" + (i + 1);
            }

            if (string.IsNullOrWhiteSpace(entry.displayName))
            {
                entry.displayName = "LEVEL " + (i + 1);
            }
        }

        selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, Mathf.Max(0, levelEntries.Count - 1));
    }

    private void BindMenuButtons()
    {
        if (menuBindingsBound)
        {
            UpdateSelectedLevelLabel();
            return;
        }

        Transform root = menuRoot != null ? menuRoot.transform : transform;
        startButton ??= FindChild<Button>(root, "Start Button");
        levelSelectButton ??= FindChild<Button>(root, "Level Select Button");
        settingsButton ??= FindChild<Button>(root, "Settings Button");
        helpButton ??= FindChild<Button>(root, "Help Button");
        skinShopButton ??= FindChild<Button>(root, "Skin Shop Button");
        skinShopPanel ??= FindChild<Transform>(root, "SkinShopPanel")?.gameObject;
        skinShopController ??= skinShopPanel != null ? skinShopPanel.GetComponent<SkinShopController>() : null;
        exitButton ??= FindChild<Button>(root, "Exit Button");
        soundToggleButton ??= FindChild<Button>(root, "Sound Toggle Button");
        levelSelectBackButton ??= FindChild<Button>(root, "Level Select Back Button");
        settingsBackButton ??= FindChild<Button>(root, "Settings Back Button");
        helpBackButton ??= FindChild<Button>(root, "Help Back Button");
        errorRetryButton ??= FindChild<Button>(root, "Error Retry Button");
        errorBackButton ??= FindChild<Button>(root, "Error Back Button");
        exitInfoCloseButton ??= FindChild<Button>(root, "Exit Info Close Button");
        selectedLevelText ??= FindChild<TMP_Text>(root, "Selected Level Text");
        soundToggleText ??= FindChild<TMP_Text>(root, "Sound Toggle Text");
        loadingText ??= FindChild<TMP_Text>(root, "Loading Text");
        loadingProgress ??= FindChild<Slider>(root, "Loading Progress");
        errorMessageText ??= FindChild<TMP_Text>(root, "Error Message Text");
        exitInfoText ??= FindChild<TMP_Text>(root, "Exit Info Text");

        AddListener(startButton, StartFirstLevel);
        AddListener(levelSelectButton, OpenLevelSelect);
        AddListener(settingsButton, OpenSettings);
        AddListener(helpButton, OpenHelp);
        AddListener(skinShopButton, OpenSkinShop);
        AddListener(exitButton, ShowExitInfo);
        AddListener(soundToggleButton, ToggleMenuSound);
        AddListener(levelSelectBackButton, BackToMainMenu);
        AddListener(settingsBackButton, BackToMainMenu);
        AddListener(helpBackButton, BackToMainMenu);
        AddListener(errorRetryButton, RetryLastLoad);
        AddListener(errorBackButton, BackToMainMenu);
        AddListener(exitInfoCloseButton, BackToMainMenu);

        for (int i = 0; i < levelEntries.Count; i++)
        {
            int index = i;
            LevelEntry entry = levelEntries[i];
            if (entry == null) continue;
            entry.button ??= FindChild<Button>(root, "Level" + (i + 1) + " Button");
            if (entry.button != null)
            {
                entry.button.onClick.AddListener(() => SelectLevel(index));
            }
        }

        menuBindingsBound = true;
        UpdateSelectedLevelLabel();
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null || callback == null) return;
        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    private static T FindChild<T>(Transform root, string objectName) where T : Component
    {
        if (root == null) return null;
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (string.Equals(components[i].name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return components[i];
            }
        }

        return null;
    }

    private void SetMenuPanel(MenuPanel panel)
    {
        currentMenuPanel = panel;
        if (menuRoot != null) menuRoot.SetActive(true);
        SetActive(mainMenuPanel, panel == MenuPanel.Main);
        SetActive(levelSelectPanel, panel == MenuPanel.LevelSelect);
        SetActive(settingsPanel, panel == MenuPanel.Settings);
        SetActive(helpPanel, panel == MenuPanel.Help);
        SetActive(loadingPanel, panel == MenuPanel.Loading);
        SetActive(errorPanel, panel == MenuPanel.Error);
        SetActive(exitInfoPanel, panel == MenuPanel.ExitInfo);
        SetActive(skinShopPanel, panel == MenuPanel.SkinShop);
        ConfigureMenuLayoutResponsive();
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private void SetMenuInteractable(bool interactable)
    {
        if (menuRoot == null) return;

        CanvasGroup group = menuRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        Selectable[] controls = menuRoot.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < controls.Length; i++)
        {
            controls[i].interactable = interactable;
        }
    }

    public void OpenMainMenu()
    {
        BackToMainMenu();
    }

    public void OpenLevelSelect()
    {
        if (sceneLoadInProgress) return;
        SetMenuPanel(MenuPanel.LevelSelect);
        SetMenuInteractable(true);
        UpdateSelectedLevelLabel();
        Selectable first = levelEntries.Count > 0 && levelEntries[selectedLevelIndex] != null
            ? levelEntries[selectedLevelIndex].button
            : null;
        if (EventSystem.current != null && first != null)
        {
            EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }

    public void OpenSettings()
    {
        if (sceneLoadInProgress) return;
        SetMenuPanel(MenuPanel.Settings);
        SetMenuInteractable(true);
        UpdateSoundLabel();
        SelectMenuControl(soundToggleButton != null ? soundToggleButton : settingsBackButton);
    }

    public void OpenHelp()
    {
        if (sceneLoadInProgress) return;
        SetMenuPanel(MenuPanel.Help);
        SetMenuInteractable(true);
        SelectMenuControl(helpBackButton);
    }

    public void OpenSkinShop()
    {
        if (sceneLoadInProgress) return;
        SetMenuPanel(MenuPanel.SkinShop);
        SetMenuInteractable(true);
        skinShopController ??= skinShopPanel != null ? skinShopPanel.GetComponent<SkinShopController>() : null;
        if (skinShopController != null)
        {
            skinShopController.Refresh();
            SelectMenuControl(skinShopController.BackButton);
        }
    }

    public void BackToMainMenu()
    {
        if (sceneLoadInProgress) return;
        SetMenuPanel(MenuPanel.Main);
        SetMenuInteractable(true);
        if (EventSystem.current != null && startButton != null)
        {
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
    }

    public void SelectLevel(int index)
    {
        if (levelEntries == null || index < 0 || index >= levelEntries.Count || levelEntries[index] == null) return;
        selectedLevelIndex = index;
        UpdateSelectedLevelLabel();
        BeginSceneLoad(levelEntries[index].sceneName);
    }

    private void UpdateSelectedLevelLabel()
    {
        if (selectedLevelText == null || levelEntries == null || levelEntries.Count == 0) return;
        selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, levelEntries.Count - 1);
        LevelEntry entry = levelEntries[selectedLevelIndex];
        selectedLevelText.text = entry == null ? "SELECT A LEVEL" : "SELECTED: " + entry.displayName;
    }

    public void StartSelectedLevel()
    {
        EnsureDefaultLevelEntries();
        if (levelEntries.Count == 0) return;
        selectedLevelIndex = Mathf.Clamp(selectedLevelIndex, 0, levelEntries.Count - 1);
        BeginSceneLoad(levelEntries[selectedLevelIndex].sceneName);
    }

    public void StartFirstLevel()
    {
        EnsureDefaultLevelEntries();
        if (levelEntries.Count == 0) return;
        selectedLevelIndex = 0;
        BeginSceneLoad(levelEntries[0].sceneName);
    }

    public void LoadLevelByName(string sceneName)
    {
        BeginSceneLoad(sceneName);
    }

    private void BeginSceneLoad(string sceneName)
    {
        if (!menuMode || sceneLoadInProgress) return;

        string normalizedName = NormalizeSceneName(sceneName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            ShowLoadError("No scene was assigned to this menu action.");
            return;
        }

        lastRequestedScene = normalizedName;
        if (FindSceneBuildIndex(normalizedName) < 0)
        {
            ShowLoadError("Scene '" + normalizedName + "' is not included in Build Settings.");
            return;
        }

        sceneLoadRoutine = StartCoroutine(LoadSceneRoutine(normalizedName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        sceneLoadInProgress = true;
        SetMenuPanel(MenuPanel.Loading);
        SetMenuInteractable(false);
        if (loadingText != null) loadingText.text = "LOADING " + sceneName.ToUpperInvariant();
        if (loadingProgress != null) loadingProgress.value = 0f;
        Time.timeScale = 1f;

        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            ShowLoadError("Unable to load " + sceneName + ": " + exception.Message);
            yield break;
        }

        if (operation == null)
        {
            ShowLoadError("Unable to start loading " + sceneName + ".");
            yield break;
        }

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (loadingProgress != null) loadingProgress.value = progress;
            if (loadingText != null) loadingText.text = "LOADING " + sceneName.ToUpperInvariant() + "  " + Mathf.RoundToInt(progress * 100f) + "%";
            yield return null;
        }
    }

    private void ShowLoadError(string message)
    {
        sceneLoadInProgress = false;
        sceneLoadRoutine = null;
        Debug.LogError("UIManager: " + message, this);
        if (!menuMode) return;

        if (errorMessageText != null) errorMessageText.text = message;
        SetMenuPanel(MenuPanel.Error);
        SetMenuInteractable(true);
        SelectMenuControl(errorRetryButton != null ? errorRetryButton : errorBackButton);
    }

    public void RetryLastLoad()
    {
        if (!string.IsNullOrWhiteSpace(lastRequestedScene))
        {
            BeginSceneLoad(lastRequestedScene);
        }
        else
        {
            BackToMainMenu();
        }
    }

    public void ToggleMenuSound()
    {
        AudioListener.volume = AudioListener.volume > 0.01f ? 0f : 1f;
        UpdateSoundLabel();
    }

    private void UpdateSoundLabel()
    {
        if (soundToggleText != null)
        {
            soundToggleText.text = AudioListener.volume > 0.01f ? "SOUND: ON" : "SOUND: OFF";
        }
    }

    public void ShowExitInfo()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowExitInfoPanel("To exit the WebGL game, close this browser tab or window.");
#elif UNITY_EDITOR
        Debug.Log("UIManager: Exit is available in a built player; the Unity Editor remains open.");
        ShowExitInfoPanel("EXIT is available in a built player. Close Play Mode to return to the editor.");
#else
        Application.Quit();
#endif
    }

    private void ShowExitInfoPanel(string message)
    {
        if (!menuMode) return;
        if (exitInfoText != null) exitInfoText.text = message;
        SetMenuPanel(MenuPanel.ExitInfo);
        SetMenuInteractable(true);
        SelectMenuControl(exitInfoCloseButton);
    }

    private void Update()
    {
        if (menuMode)
        {
            ConfigureMenuLayoutResponsive();
            if (!sceneLoadInProgress && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleMenuCancel();
            }

            return;
        }

        if (!IsGameActive)
        {
            bool clicked = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                clicked = true;
            }

            if (clicked && mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                StartGame();
            }

            return;
        }

        UpdateHUD();
    }

    private void HandleMenuCancel()
    {
        switch (currentMenuPanel)
        {
            case MenuPanel.LevelSelect:
            case MenuPanel.Settings:
            case MenuPanel.Help:
            case MenuPanel.SkinShop:
            case MenuPanel.Error:
            case MenuPanel.ExitInfo:
                BackToMainMenu();
                break;
        }
    }

    private void SelectMenuControl(Selectable control)
    {
        if (EventSystem.current != null && control != null && control.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(control.gameObject);
        }
    }

    private void BindMenuCancelAction()
    {
        InputSystemUIInputModule module = EventSystem.current != null
            ? EventSystem.current.GetComponent<InputSystemUIInputModule>()
            : null;
        InputAction action = module != null && module.cancel != null ? module.cancel.action : null;
        if (menuCancelAction == action)
        {
            return;
        }

        if (menuCancelAction != null)
        {
            menuCancelAction.performed -= OnMenuCancelPerformed;
        }

        menuCancelAction = action;
        if (menuCancelAction != null)
        {
            menuCancelAction.performed += OnMenuCancelPerformed;
            if (!menuCancelAction.enabled)
            {
                menuCancelAction.Enable();
            }
        }
    }

    private void OnMenuCancelPerformed(InputAction.CallbackContext context)
    {
        if (menuMode && !sceneLoadInProgress)
        {
            HandleMenuCancel();
        }
    }

    private void ConfigureMenuLayoutResponsive()
    {
        if (menuRoot == null || (Screen.width == lastMenuScreenWidth && Screen.height == lastMenuScreenHeight))
        {
            return;
        }

        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        if (rootRect == null || rootRect.rect.width <= 0f || rootRect.rect.height <= 0f)
        {
            return;
        }

        lastMenuScreenWidth = Screen.width;
        lastMenuScreenHeight = Screen.height;
        float availableWidth = Mathf.Max(1f, rootRect.rect.width - 32f);
        float availableHeight = Mathf.Max(1f, rootRect.rect.height - 32f);
        GameObject[] panels = { mainMenuPanel, levelSelectPanel, settingsPanel, helpPanel, skinShopPanel, loadingPanel, errorPanel, exitInfoPanel };
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            RectTransform panelRect = panels[i].GetComponent<RectTransform>();
            if (panelRect == null) continue;
            Vector2 authoredSize = panelRect.sizeDelta;
            float widthScale = availableWidth / Mathf.Max(1f, authoredSize.x);
            float heightScale = availableHeight / Mathf.Max(1f, authoredSize.y);
            float scale = Mathf.Clamp01(Mathf.Min(1f, Mathf.Min(widthScale, heightScale)));
            panelRect.localScale = Vector3.one * Mathf.Max(0.35f, scale);
        }
    }

    private void StartGame()
    {
        if (menuMode)
        {
            StartFirstLevel();
            return;
        }

        IsGameActive = true;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void ConfigureLayoutResponsive()
    {
        if (cloneCountText != null)
        {
            RectTransform rect = cloneCountText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -40f);
                rect.sizeDelta = new Vector2(300f, 60f);
                cloneCountText.text = "1";
                cloneCountText.alignment = TextAlignmentOptions.Center;
                cloneCountText.fontSize = 42f;
                cloneCountText.color = Color.white;
            }
        }

        if (progressBar != null)
        {
            RectTransform progressRect = progressBar.GetComponent<RectTransform>();
            if (progressRect != null)
            {
                progressRect.anchorMin = new Vector2(0.5f, 1f);
                progressRect.anchorMax = new Vector2(0.5f, 1f);
                progressRect.pivot = new Vector2(0.5f, 1f);
                progressRect.anchoredPosition = new Vector2(0f, -110f);
                progressRect.sizeDelta = new Vector2(400f, 25f);
            }
        }

        if (winScoreText != null)
        {
            RectTransform rect = winScoreText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(400f, 60f);
                winScoreText.alignment = TextAlignmentOptions.Center;
                winScoreText.fontSize = 32f;
                winScoreText.color = Color.white;
            }
        }
    }

    private void FindTrackLengthDynamic()
    {
        GameObject endMarker = GameObject.Find("FinishLine");
        if (endMarker == null)
        {
            endMarker = GameObject.Find("Finish");
        }

        if (endMarker != null)
        {
            trackLength = endMarker.transform.position.z;
        }
        else
        {
            float maxZ = 100f;
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                string objectName = renderer.gameObject.name.ToLowerInvariant();
                if (renderer.transform.position.z > maxZ && (objectName.Contains("road") || objectName.Contains("floor") || objectName.Contains("platform")))
                {
                    maxZ = renderer.transform.position.z;
                }
            }

            trackLength = maxZ;
        }

        Debug.Log("UIManager: Track length dynamically set to: " + trackLength);
    }

    private void UpdateHUD()
    {
        if (playerCrowd != null && cloneCountText != null)
        {
            cloneCountText.text = playerCrowd.ActiveRunnerCount.ToString();
        }

        if (playerTransform != null && progressBar != null)
        {
            float safeTrackLength = Mathf.Max(0.001f, trackLength);
            float progress = playerTransform.position.z / safeTrackLength;
            progressBar.value = Mathf.Clamp01(progress);

            if (progress >= 0.99f && !gameOverTriggered)
            {
                gameOverTriggered = true;
                int finalScore = playerCrowd != null ? playerCrowd.ActiveRunnerCount : 0;
                ShowYouWin(finalScore);
            }
        }
    }

    public void ShowGameOver(int finalScore)
    {
        if (menuMode || !IsGameActive)
        {
            return;
        }

        IsGameActive = false;
        Time.timeScale = 1f;
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = "Final Score: " + finalScore;
    }

    public void ShowYouWin(int finalScore)
    {
        if (menuMode || !IsGameActive)
        {
            return;
        }

        IsGameActive = false;
        Time.timeScale = 1f;
        if (hudPanel != null) hudPanel.SetActive(false);
        if (youWinPanel != null) youWinPanel.SetActive(true);
        if (winScoreText != null) winScoreText.text = "Clones Saved: " + finalScore;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameOverTriggered = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TriggerLeadFallGameOver(Transform leadTransform)
    {
        if (menuMode || gameOverTriggered) return;
        gameOverTriggered = true;
        StartCoroutine(LeadFallRoutine(leadTransform));
    }

    private IEnumerator LeadFallRoutine(Transform leadTransform)
    {
        Time.timeScale = 0.5f;
        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null && leadTransform != null)
        {
            vcam.Follow = leadTransform;
            vcam.Target.TrackingTarget = leadTransform;

            const float duration = 2f;
            float elapsed = 0f;
            float startFov = vcam.Lens.FieldOfView;
            CinemachineOrbitalFollow orbitalFollow = vcam.GetComponent<CinemachineOrbitalFollow>();
            float startRadius = orbitalFollow != null ? orbitalFollow.Radius : 5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                var lens = vcam.Lens;
                lens.FieldOfView = Mathf.Lerp(startFov, 25f, t);
                vcam.Lens = lens;
                if (orbitalFollow != null)
                {
                    orbitalFollow.Radius = Mathf.Lerp(startRadius, 2f, t);
                }

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(1.5f);
        }

        Time.timeScale = 1f;
        ShowGameOver(0);
    }

    private static string NormalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return string.Empty;
        string normalized = sceneName.Trim();
        if (normalized.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Path.GetFileNameWithoutExtension(normalized);
        }

        return Path.GetFileNameWithoutExtension(normalized);
    }

    private static int FindSceneBuildIndex(string sceneName)
    {
        string normalized = NormalizeSceneName(sceneName);
        if (string.IsNullOrWhiteSpace(normalized)) return -1;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrWhiteSpace(path) && string.Equals(Path.GetFileNameWithoutExtension(path), normalized, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void OnDestroy()
    {
        if (menuCancelAction != null)
        {
            menuCancelAction.performed -= OnMenuCancelPerformed;
            menuCancelAction = null;
        }

        if (Instance == this)
        {
            Instance = null;
            gameActive = false;
        }

        if (string.Equals(SceneManager.GetActiveScene().name, menuSceneName, StringComparison.OrdinalIgnoreCase))
        {
            externalGameActive = false;
        }
    }
}
