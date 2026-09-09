using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class FinishGate : MonoBehaviour
{
    [Header("Finish UI")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite bannerSprite;
    [SerializeField] private Sprite rewardPanelSprite;
    [SerializeField] private Sprite primaryButtonSprite;
    [SerializeField] private Sprite coinSprite;

    private bool hasFinished;
    private GameObject overlay;
    private PlayerCrowdManager crowd;

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds world = renderer.bounds;
                Vector3 scale = transform.lossyScale;
                col.center = transform.InverseTransformPoint(world.center);
                col.size = new Vector3(
                    Mathf.Abs(world.size.x / scale.x),
                    Mathf.Abs(world.size.y / scale.y),
                    Mathf.Abs(world.size.z / scale.z));
            }
        }
    }

    private void Start()
    {
        crowd = FindFirstObjectByType<PlayerCrowdManager>();
    }

    private void Update()
    {
        if (hasFinished || crowd == null)
        {
            return;
        }

        if (crowd.transform.position.z >= transform.position.z)
        {
            hasFinished = true;
            ShowFinish();
        }
    }

    private void ShowFinish()
    {
        Time.timeScale = 0f;
        if (overlay != null)
        {
            return;
        }

        Canvas canvas = FindCanvas();
        overlay = BuildOverlay(canvas);
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

        Button next = CreateButton(card.transform, primaryButtonSprite, Color.white, "Next Level Button");
        SetRect(next.GetComponent<RectTransform>(), 0f, -108f, 300f, 78f, new Vector2(0.5f, 0.5f));
        next.onClick.AddListener(LoadNextLevel);

        TMP_Text nextLabel = CreateText(next.transform, "NEXT LEVEL", 25, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold, "Next Level Label");
        SetRect(nextLabel.rectTransform, 0f, 0f, 278f, 56f, new Vector2(0.5f, 0.5f));

        TMP_Text nextLevel = CreateText(card.transform, "LEVEL " + GetNextLevelNumber(), 15,
            new Color(0.55f, 0.78f, 0.9f), TextAlignmentOptions.Center, FontStyles.Bold, "Next Level Number");
        SetRect(nextLevel.rectTransform, 0f, -164f, 260f, 26f, new Vector2(0.5f, 0.5f));

        return root;
    }

    private string GetLevelTitle()
    {
        return "LEVEL " + GetCurrentLevelNumber() + " COMPLETE";
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
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        return buildIndex >= 0 ? buildIndex + 2 : 2;
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex >= 0 && buildIndex + 1 < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(buildIndex + 1);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
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

    private static Button CreateButton(Transform parent, Sprite sprite, Color color, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
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
        text.enableWordWrapping = false;
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
