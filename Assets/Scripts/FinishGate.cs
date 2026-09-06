using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class FinishGate : MonoBehaviour
{
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
        if (hasFinished) return;

        if (crowd == null) return;

        if (crowd.transform.position.z >= transform.position.z)
        {
            hasFinished = true;
            ShowFinish();
        }
    }

    private void ShowFinish()
    {
        Time.timeScale = 0f;
        if (overlay != null) return;

        Canvas canvas = FindCanvas();
        overlay = BuildOverlay(canvas);
    }

    private Canvas FindCanvas()
    {
        GameObject hud = GameObject.Find("Level HUD Canvas");
        Canvas canvas = hud != null ? hud.GetComponent<Canvas>() : null;
        if (canvas != null) return canvas;

        canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas;

        GameObject canvasGo = new GameObject("Finish Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
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
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Level2")
        {
            Text levelTitle = CreateText(root.transform, "LEVEL 2 COMPLETE", 52,
                Color.white, TextAnchor.MiddleCenter);
            SetRect(levelTitle.rectTransform, 0f, 245f, 760f, 72f, new Vector2(0.5f, 0.5f));

            Text levelName = CreateText(root.transform, "MOMENTUM TRAPWORKS", 28,
                new Color(1f, 0.84f, 0.16f), TextAnchor.MiddleCenter);
            SetRect(levelName.rectTransform, 0f, 195f, 760f, 48f, new Vector2(0.5f, 0.5f));
        }
        else if (sceneName == "Level3")
        {
            Text levelTitle = CreateText(root.transform, "LEVEL 3 COMPLETE", 52,
                Color.white, TextAnchor.MiddleCenter);
            SetRect(levelTitle.rectTransform, 0f, 245f, 760f, 72f, new Vector2(0.5f, 0.5f));

            Text levelName = CreateText(root.transform, "PULSEBOUND FOUNDRY", 28,
                new Color(0.25f, 0.9f, 1f), TextAnchor.MiddleCenter);
            SetRect(levelName.rectTransform, 0f, 195f, 760f, 48f, new Vector2(0.5f, 0.5f));
        }

        Sprite circle = CreateCircleSprite(128);
        Sprite arrow = CreateArrowSprite(128);

        Image coin = CreateImage(root.transform, circle, new Color(1f, 0.84f, 0.16f));
        SetRect(coin.rectTransform, 0f, 90f, 88f, 88f, new Vector2(0.5f, 0.5f));

        Text coinCount = CreateText(root.transform, GetCoins().ToString(), 84,
            Color.white, TextAnchor.MiddleLeft);
        SetRect(coinCount.rectTransform, 62f, 90f, 260f, 110f, new Vector2(0.5f, 0.5f));

        Button next = CreateButton(root.transform, circle, new Color(0.16f, 0.75f, 0.35f));
        SetRect(next.GetComponent<RectTransform>(), 0f, -110f, 118f, 118f, new Vector2(0.5f, 0.5f));
        next.onClick.AddListener(LoadNextLevel);

        Image arrowIcon = CreateImage(next.transform, arrow, Color.white);
        SetRect(arrowIcon.rectTransform, 0f, 8f, 56f, 56f, new Vector2(0.5f, 0.5f));

        Text nextLevel = CreateText(root.transform, GetNextLevelNumber().ToString(), 50,
            Color.white, TextAnchor.MiddleCenter);
        SetRect(nextLevel.rectTransform, 0f, -216f, 140f, 70f, new Vector2(0.5f, 0.5f));

        return root;
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

    private static Image CreateImage(Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject("Image", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    private static Button CreateButton(Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return go.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, string value, int fontSize,
        Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text txt = go.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = value;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = anchor;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    private static void SetRect(RectTransform rect, float x, float y, float w, float h, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                tex.SetPixel(x, y, (dx * dx + dy * dy) <= radius * radius
                    ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateArrowSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float spread = Mathf.Abs(v - 0.5f) * 2f;
                tex.SetPixel(x, y, u >= spread ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
