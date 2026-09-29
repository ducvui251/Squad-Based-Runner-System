using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class FinishGate : MonoBehaviour
{
    [Header("Finish Collision")]
    [Tooltip("Dedicated finite trigger volume used for completion. Leave empty only for legacy scenes; the root BoxCollider is then used as a compatibility fallback.")]
    [SerializeField] private BoxCollider finishVolume;

    [Header("Finish UI")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite bannerSprite;
    [SerializeField] private Sprite rewardPanelSprite;
    [SerializeField] private Sprite primaryButtonSprite;
    [SerializeField] private Sprite coinSprite;

    private bool hasFinished;
    private GameObject overlay;
    private GameObject transitionErrorOverlay;
    private PlayerCrowdManager crowd;
    [SerializeField] private SfxPlayer sfxPlayer;
    private BoxCollider resolvedFinishVolume;
    private Vector3 previousCrowdPosition;
    private bool hasPreviousCrowdPosition;
    private bool wasEligibleForCrossing;

    private const float GEOMETRY_EPSILON = 0.00001f;

    private void Awake()
    {
        // Finish volumes are authored in the scene. Runtime renderer fitting used to
        // make the effective finish region depend on imported art bounds and could
        // leave the trigger offset from the intended line. Keep the root collider as
        // a compatibility fallback for legacy scenes that have not been migrated yet.
        resolvedFinishVolume = finishVolume != null ? finishVolume : GetComponent<BoxCollider>();
    }

    private void Start()
    {
        crowd = FindFirstObjectByType<PlayerCrowdManager>();
        if (crowd != null)
        {
            previousCrowdPosition = crowd.transform.position;
            hasPreviousCrowdPosition = true;
        }
    }

    private void Update()
    {
        if (hasFinished)
        {
            return;
        }

        if (crowd == null)
        {
            crowd = FindFirstObjectByType<PlayerCrowdManager>();
            if (crowd == null)
            {
                hasPreviousCrowdPosition = false;
                wasEligibleForCrossing = false;
                return;
            }

            previousCrowdPosition = crowd.transform.position;
            hasPreviousCrowdPosition = true;
            wasEligibleForCrossing = false;
            return;
        }

        Vector3 currentCrowdPosition = crowd.transform.position;
        if (!hasPreviousCrowdPosition)
        {
            previousCrowdPosition = currentCrowdPosition;
            hasPreviousCrowdPosition = true;
            return;
        }

        // Keep the history current while paused, inactive, empty, or terminal. This
        // prevents a teleport/crossing that occurred outside an active run from being
        // consumed when the run becomes active again.
        if (!CanCompleteRun())
        {
            previousCrowdPosition = currentCrowdPosition;
            hasPreviousCrowdPosition = true;
            wasEligibleForCrossing = false;
            return;
        }

        // The first sample after a paused, inactive, empty, or terminal interval is
        // a new baseline. Without this edge, an inactive teleport into the finish
        // volume could be consumed as a valid swept crossing on resume.
        if (!wasEligibleForCrossing)
        {
            previousCrowdPosition = currentCrowdPosition;
            hasPreviousCrowdPosition = true;
            wasEligibleForCrossing = true;
            return;
        }

        if (HasForwardCrossedFinish(previousCrowdPosition, currentCrowdPosition))
        {
            hasFinished = true;
            ShowFinish();
        }

        previousCrowdPosition = currentCrowdPosition;
        wasEligibleForCrossing = true;
    }

    private bool CanCompleteRun()
    {
        return crowd != null &&
            crowd.isActiveAndEnabled &&
            crowd.gameObject.activeInHierarchy &&
            UIManager.IsGameActive &&
            !crowd.IsGameOver &&
            !crowd.IsLeadFallGameOver &&
            crowd.ActiveRunnerCount > 0;
    }

    private bool HasForwardCrossedFinish(Vector3 fromWorld, Vector3 toWorld)
    {
        BoxCollider volume = resolvedFinishVolume;
        if (!IsValidFinishVolume(volume))
        {
            return false;
        }

        Vector3 fromLocal = volume.transform.InverseTransformPoint(fromWorld);
        Vector3 toLocal = volume.transform.InverseTransformPoint(toWorld);
        if (!IsFinite(fromLocal) || !IsFinite(toLocal))
        {
            return false;
        }

        return IsFinishCrossing(
            crowd != null && crowd.isActiveAndEnabled && crowd.gameObject.activeInHierarchy && UIManager.IsGameActive,
            crowd != null && crowd.ActiveRunnerCount > 0,
            crowd != null && crowd.IsGameOver,
            crowd != null && crowd.IsLeadFallGameOver,
            fromLocal,
            toLocal,
            volume.center,
            volume.size);
    }

    private static bool IsValidFinishVolume(BoxCollider volume)
    {
        if (volume == null || !volume.enabled || !volume.isTrigger || !volume.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 size = volume.size;
        Vector3 lossyScale = volume.transform.lossyScale;
        return IsFinite(volume.center) &&
            IsFinite(size) &&
            IsFinite(lossyScale) &&
            size.x > GEOMETRY_EPSILON &&
            size.y > GEOMETRY_EPSILON &&
            size.z > GEOMETRY_EPSILON &&
            Mathf.Abs(lossyScale.x) > GEOMETRY_EPSILON &&
            Mathf.Abs(lossyScale.y) > GEOMETRY_EPSILON &&
            Mathf.Abs(lossyScale.z) > GEOMETRY_EPSILON;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>
    /// Tests an eligible forward movement segment against an authored finish box.
    /// Endpoints must already be expressed in the volume's local space.
    /// </summary>
    public static bool IsFinishCrossing(
        bool isRunActive,
        bool hasAliveRunners,
        bool isGameOver,
        bool isLeadFallGameOver,
        Vector3 fromLocal,
        Vector3 toLocal,
        Vector3 center,
        Vector3 size)
    {
        if (!isRunActive || !hasAliveRunners || isGameOver || isLeadFallGameOver ||
            !IsFinite(fromLocal) || !IsFinite(toLocal) || !IsFinite(center) || !IsFinite(size) ||
            size.x <= GEOMETRY_EPSILON || size.y <= GEOMETRY_EPSILON || size.z <= GEOMETRY_EPSILON)
        {
            return false;
        }

        // The runner must travel through the volume in the gate's authored forward
        // direction. A backward crossing should not complete the run.
        if (toLocal.z <= fromLocal.z + GEOMETRY_EPSILON)
        {
            return false;
        }

        return SegmentIntersectsBox(fromLocal, toLocal, center, size * 0.5f);
    }

    // Segment-versus-AABB slab test in the finish volume's local space. Transforming
    // both endpoints first keeps this exact for rotated and nonuniformly scaled boxes
    // without allocating or relying on the collider's world-space AABB.
    private static bool SegmentIntersectsBox(Vector3 start, Vector3 end, Vector3 center, Vector3 halfExtents)
    {
        Vector3 min = center - halfExtents;
        Vector3 max = center + halfExtents;
        Vector3 delta = end - start;
        float entry = 0f;
        float exit = 1f;

        for (int axis = 0; axis < 3; axis++)
        {
            float origin = start[axis];
            float direction = delta[axis];
            if (Mathf.Abs(direction) <= GEOMETRY_EPSILON)
            {
                if (origin < min[axis] - GEOMETRY_EPSILON || origin > max[axis] + GEOMETRY_EPSILON)
                {
                    return false;
                }

                continue;
            }

            float inverseDirection = 1f / direction;
            float axisEntry = (min[axis] - origin) * inverseDirection;
            float axisExit = (max[axis] - origin) * inverseDirection;
            if (axisEntry > axisExit)
            {
                float swap = axisEntry;
                axisEntry = axisExit;
                axisExit = swap;
            }

            entry = Mathf.Max(entry, axisEntry);
            exit = Mathf.Min(exit, axisExit);
            if (entry > exit + GEOMETRY_EPSILON)
            {
                return false;
            }
        }

        return entry <= 1f + GEOMETRY_EPSILON && exit >= -GEOMETRY_EPSILON;
    }

    private void ShowFinish()
    {
        sfxPlayer?.PlayFinish();
        Time.timeScale = 0f;
        if (overlay != null)
        {
            return;
        }

        Canvas canvas = FindCanvas();
        overlay = BuildOverlay(canvas);
        if (CurrencyWallet.Instance != null)
        {
            CurrencyWallet.Instance.BankRunCoins();
        }
    }

    private Canvas FindCanvas()
    {
        GameObject hud = GameObject.Find("Level HUD Canvas");
        Canvas canvas = hud != null ? hud.GetComponent<Canvas>() : null;
        if (canvas != null)
        {
            return canvas;
        }

        canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasGo = new GameObject(
            "Finish Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 600f);
        return canvas;
    }

    private GameObject BuildOverlay(Canvas canvas)
    {
        GameObject root = new GameObject("Finish Screen", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image dim = root.GetComponent<Image>();
        dim.color = new Color(0.008f, 0.025f, 0.055f, 0.84f);

        Image card = CreateImage(root.transform, panelSprite, Color.white, true, "Finish Card");
        SetRect(card.rectTransform, 0f, 0f, 820f, 500f, new Vector2(0.5f, 0.5f));

        Image banner = CreateImage(card.transform, bannerSprite, Color.white, true, "Complete Banner");
        SetRect(banner.rectTransform, 0f, 178f, 560f, 70f, new Vector2(0.5f, 0.5f));

        TMP_Text title = CreateText(card.transform, GetLevelTitle(), 34, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Level Complete");
        SetRect(title.rectTransform, 0f, 178f, 520f, 54f, new Vector2(0.5f, 0.5f));

        TMP_Text levelName = CreateText(card.transform, GetLevelName(), 18, GetAccentColor(),
            TextAlignmentOptions.Center, FontStyles.Bold, "Level Name");
        SetRect(levelName.rectTransform, 0f, 132f, 680f, 32f, new Vector2(0.5f, 0.5f));

        Image rewardPanel = CreateImage(card.transform,
            rewardPanelSprite != null ? rewardPanelSprite : panelSprite,
            Color.white, true, "Reward Panel");
        SetRect(rewardPanel.rectTransform, 0f, 52f, 580f, 112f, new Vector2(0.5f, 0.5f));

        TMP_Text rewardLabel = CreateText(card.transform, "RUN REWARD", 15, new Color(0.55f, 0.78f, 0.9f),
            TextAlignmentOptions.Center, FontStyles.Bold, "Reward Label");
        SetRect(rewardLabel.rectTransform, -80f, 83f, 190f, 24f, new Vector2(0.5f, 0.5f));

        Image coin = CreateImage(card.transform, coinSprite, Color.white, false, "Reward Coin");
        coin.preserveAspect = true;
        SetRect(coin.rectTransform, -196f, 52f, 72f, 72f, new Vector2(0.5f, 0.5f));

        TMP_Text coinCount = CreateText(card.transform, GetCoins().ToString("N0"), 42, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Reward Count");
        SetRect(coinCount.rectTransform, -80f, 48f, 190f, 64f, new Vector2(0.5f, 0.5f));

        TMP_Text coinCaption = CreateText(card.transform, "COINS COLLECTED", 13, new Color(1f, 0.82f, 0.2f),
            TextAlignmentOptions.Center, FontStyles.Bold, "Reward Caption");
        SetRect(coinCaption.rectTransform, 125f, 49f, 170f, 24f, new Vector2(0.5f, 0.5f));

        bool finalLevel = IsFinalLevel();
        Button next = CreateButton(card.transform, primaryButtonSprite, Color.white, "Next Level Button");
        SetRect(next.GetComponent<RectTransform>(), 0f, finalLevel ? -92f : -108f, 300f, finalLevel ? 68f : 78f, new Vector2(0.5f, 0.5f));
        next.onClick.AddListener(finalLevel ? ReplayCurrentLevel : LoadNextLevel);

        TMP_Text nextLabel = CreateText(next.transform, finalLevel ? "REPLAY LEVEL 5" : "NEXT LEVEL", 25, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Next Level Label");
        SetRect(nextLabel.rectTransform, 0f, 0f, 278f, 56f, new Vector2(0.5f, 0.5f));

        TMP_Text nextLevel = CreateText(card.transform, finalLevel ? "ALL LEVELS COMPLETE" : "LEVEL " + GetNextLevelNumber(), 15,
            new Color(0.55f, 0.78f, 0.9f), TextAlignmentOptions.Center, FontStyles.Bold, "Next Level Number");
        SetRect(nextLevel.rectTransform, 0f, finalLevel ? -142f : -164f, 320f, 26f, new Vector2(0.5f, 0.5f));

        Button menu = CreateButton(card.transform, primaryButtonSprite, Color.white, "Main Menu Button");
        SetRect(menu.GetComponent<RectTransform>(), 0f, finalLevel ? -202f : -220f, 300f, finalLevel ? 58f : 46f, new Vector2(0.5f, 0.5f));
        menu.onClick.AddListener(LoadMainMenu);

        TMP_Text menuLabel = CreateText(menu.transform, "MAIN MENU", 22, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Main Menu Label");
        SetRect(menuLabel.rectTransform, 0f, 0f, 278f, 46f, new Vector2(0.5f, 0.5f));

        return root;
    }

    private string GetLevelTitle()
    {
        return IsFinalLevel() ? "YOU WON!" : "LEVEL " + GetCurrentLevelNumber() + " COMPLETE";
    }

    private static string GetLevelName()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        switch (sceneName)
        {
            case "Level1": return "RAMP-UP RUN";
            case "Level2": return "MOMENTUM TRAPWORKS";
            case "Level3": return "PULSEBOUND FOUNDRY";
            case "Level4": return "VAULTLINE CITADEL";
            case "Level5": return "FRACTURE RELAY";
            default: return sceneName.ToUpperInvariant();
        }
    }

    private static Color GetAccentColor()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        switch (sceneName)
        {
            case "Level2": return new Color(1f, 0.84f, 0.16f);
            case "Level3": return new Color(0.25f, 0.9f, 1f);
            case "Level4": return new Color(1f, 0.55f, 0.18f);
            case "Level5": return new Color(0.86f, 0.16f, 0.98f);
            default: return new Color(0.25f, 0.9f, 1f);
        }
    }

    private static int GetCurrentLevelNumber()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring(5), out int levelNumber))
        {
            return levelNumber;
        }

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        return buildIndex >= 0 ? buildIndex + 1 : 1;
    }

    private static int GetCoins()
    {
        return CurrencyWallet.Instance != null ? CurrencyWallet.Instance.RunCoins : 0;
    }

    private static int GetNextLevelNumber()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Level1": return 2;
            case "Level2": return 3;
            case "Level3": return 4;
            case "Level4": return 5;
            default: return 0;
        }
    }

    private static bool IsFinalLevel()
    {
        return SceneManager.GetActiveScene().name == "Level5";
    }

    private static bool TryGetNextLevel(string currentScene, out string nextScene)
    {
        switch (currentScene)
        {
            case "Level1": nextScene = "Level2"; return true;
            case "Level2": nextScene = "Level3"; return true;
            case "Level3": nextScene = "Level4"; return true;
            case "Level4": nextScene = "Level5"; return true;
            default:
                nextScene = null;
                return false;
        }
    }

    private bool transitionInProgress;

    private void LoadNextLevel()
    {
        if (transitionInProgress) return;
        Time.timeScale = 1f;
        if (!TryGetNextLevel(SceneManager.GetActiveScene().name, out string nextScene))
        {
            ShowTransitionError("No next level is mapped for " + SceneManager.GetActiveScene().name + ".");
            return;
        }

        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + nextScene + ".unity") < 0)
        {
            ShowTransitionError("The next level (" + nextScene + ") is not included in Build Settings.");
            return;
        }

        transitionInProgress = true;
        SceneManager.LoadScene(nextScene);
    }

    private void ShowTransitionError(string message)
    {
        if (transitionErrorOverlay != null)
        {
            return;
        }

        Debug.LogError("FinishGate: " + message, this);
        Canvas canvas = FindCanvas();
        transitionErrorOverlay = new GameObject("Finish Transition Error", typeof(RectTransform), typeof(Image));
        transitionErrorOverlay.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = transitionErrorOverlay.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Image blocker = transitionErrorOverlay.GetComponent<Image>();
        blocker.color = Color.clear;
        blocker.raycastTarget = true;

        Image errorCard = CreateImage(transitionErrorOverlay.transform, panelSprite,
            new Color(0.16f, 0.08f, 0.18f, 0.98f), true, "Transition Error Card");
        SetRect(errorCard.rectTransform, 0f, 0f, 620f, 280f, new Vector2(0.5f, 0.5f));

        TMP_Text title = CreateText(errorCard.transform, "LEVEL LOAD ERROR", 28f,
            new Color(1f, 0.78f, 0.3f), TextAlignmentOptions.Center, FontStyles.Bold, "Transition Error Title");
        SetRect(title.rectTransform, 0f, 82f, 560f, 48f, new Vector2(0.5f, 0.5f));

        TMP_Text detail = CreateText(errorCard.transform, message, 17f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Normal, "Transition Error Message");
        detail.textWrappingMode = TextWrappingModes.Normal;
        SetRect(detail.rectTransform, 0f, 28f, 540f, 70f, new Vector2(0.5f, 0.5f));

        Button retry = CreateButton(errorCard.transform, primaryButtonSprite, Color.white, "Transition Error Retry Button");
        SetRect(retry.GetComponent<RectTransform>(), -132f, -82f, 230f, 52f, new Vector2(0.5f, 0.5f));
        retry.onClick.AddListener(LoadNextLevel);
        TMP_Text retryLabel = CreateText(retry.transform, "RETRY", 22f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Transition Error Retry Label");
        SetRect(retryLabel.rectTransform, 0f, 0f, 210f, 44f, new Vector2(0.5f, 0.5f));

        Button menu = CreateButton(errorCard.transform, primaryButtonSprite, Color.white, "Transition Error Main Menu Button");
        SetRect(menu.GetComponent<RectTransform>(), 132f, -82f, 230f, 52f, new Vector2(0.5f, 0.5f));
        menu.onClick.AddListener(LoadMainMenu);
        TMP_Text menuLabel = CreateText(menu.transform, "MAIN MENU", 22f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Transition Error Main Menu Label");
        SetRect(menuLabel.rectTransform, 0f, 0f, 210f, 44f, new Vector2(0.5f, 0.5f));

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(retry.gameObject);
        }
    }

    private void ReplayCurrentLevel()
    {
        if (transitionInProgress) return;
        Time.timeScale = 1f;
        transitionInProgress = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadMainMenu()
    {
        if (transitionInProgress) return;
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Menu.unity") < 0)
        {
            Debug.LogError("FinishGate: Menu is not available in Build Settings.", this);
            return;
        }

        Time.timeScale = 1f;
        transitionInProgress = true;
        SceneManager.LoadScene("Menu");
    }

    private static Image CreateImage(Transform parent, Sprite sprite, Color color, bool sliced, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        return image;
    }

    private Button CreateButton(Transform parent, Sprite sprite, Color color, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        sfxPlayer?.RegisterButton(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.78f);
        button.colors = colors;
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string value, float fontSize, Color color,
        TextAlignmentOptions alignment, FontStyles fontStyle, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.margin = new Vector4(4f, 0f, 4f, 0f);
        return text;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
