using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkinShopController : MonoBehaviour
{
    private const int SkinCount = 9;
    private const string OwnedKey = "SpiralSquad.SkinShop.OwnedMask";
    private const string EquippedKey = "SpiralSquad.SkinShop.EquippedId";
    private static readonly Color ShopPreviewBlueTint = new Color(0.34f, 0.62f, 0.95f, 1f);
    // The default preview has visible pixels on 139 of its 256 source rows.
    private const float DefaultShopPreviewScale = 256f / 139f;

    [Header("Shop Assets")]
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private Sprite[] previews = new Sprite[SkinCount];
    [SerializeField] private Sprite cardFrameSprite;
    [SerializeField] private Sprite buttonBlueSprite;
    [SerializeField] private Sprite buttonGreenSprite;
    [SerializeField] private TMP_FontAsset font;

    [Header("Shop Balance")]
    [SerializeField] private int[] prices = { 0, 150, 300, 450, 600, 750, 900, 1050, 1200 };

    private sealed class SkinCard
    {
        public RectTransform root;
        public RectTransform previewRect;
        public Image preview;
        public Image accent;
        public TMP_Text title;
        public Button action;
        public TMP_Text actionText;
    }

    private readonly SkinCard[] cards = new SkinCard[SkinCount];
    private RectTransform panelRect;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text walletText;
    private TMP_Text statusText;
    private Button backButton;
    [SerializeField] private SfxPlayer sfxPlayer;
    private Material shopPreviewTintMaterial;
    private bool isBuilt;
    private string currentStatus;

    private static readonly Color[] SkinTints =
    {
        Color.white,
        new Color(0.35f, 0.82f, 1f),
        new Color(1f, 0.48f, 0.78f),
        new Color(0.48f, 1f, 0.58f),
        new Color(0.48f, 0.62f, 1f),
        new Color(1f, 0.68f, 0.34f),
        new Color(0.78f, 0.52f, 1f),
        new Color(1f, 0.45f, 0.38f),
        new Color(0.78f, 0.96f, 1f)
    };

    public Button BackButton => backButton;
    public static int EquippedSkinId => Mathf.Clamp(PlayerPrefs.GetInt(EquippedKey, 0), 0, SkinCount - 1);

    private void Awake()
    {
        shopPreviewTintMaterial = Resources.Load<Material>("ShopPreviewBlueSilhouette");
        if (shopPreviewTintMaterial == null)
        {
            Debug.LogError("SkinShopController: Resources/ShopPreviewBlueSilhouette material is missing.", this);
        }

        BuildPage();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isBuilt)
        {
            LayoutPage();
        }
    }

    public void Refresh()
    {
        if (!isBuilt)
        {
            BuildPage();
        }

        wallet ??= CurrencyWallet.Instance != null ? CurrencyWallet.Instance : FindFirstObjectByType<CurrencyWallet>();
        EnsureDefaultOwnership();
        LayoutPage();
        RefreshCards();
    }

    public static Color GetSkinTint(int skinId)
    {
        return SkinTints[Mathf.Clamp(skinId, 0, SkinTints.Length - 1)];
    }

    public static void ApplyEquippedSkin(GameObject runner)
    {
        if (runner == null)
        {
            return;
        }

        PlayerCrowdManager.ApplyEquippedSkinToRunner(runner, EquippedSkinId);
    }

    private void BuildPage()
    {
        if (isBuilt)
        {
            return;
        }

        panelRect = transform as RectTransform;
        if (panelRect == null)
        {
            Debug.LogError("SkinShopController must be attached to a UI panel.", this);
            return;
        }

        wallet ??= CurrencyWallet.Instance != null ? CurrencyWallet.Instance : FindFirstObjectByType<CurrencyWallet>();
        EnsureDefaultOwnership();

        titleText = CreateText("Shop Title", "RUNNER SKINS", 38f, Color.white, TextAlignmentOptions.Center);
        subtitleText = CreateText("Shop Subtitle", "CHOOSE A LOOK FOR YOUR NEXT RUN", 18f, new Color(0.68f, 0.87f, 1f), TextAlignmentOptions.Center);
        walletText = CreateText("Shop Wallet", string.Empty, 20f, new Color(1f, 0.86f, 0.24f), TextAlignmentOptions.Center);
        statusText = CreateText("Shop Status", "RUN COINS ARE BANKED AT FINISH OR GAME OVER.", 16f, new Color(0.68f, 0.83f, 0.95f), TextAlignmentOptions.Center);

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = CreateCard(i);
        }

        backButton = CreateButton(transform, "Shop Back Button", "BACK", buttonBlueSprite, new Color(0.92f, 0.96f, 1f));
        backButton.onClick.AddListener(BackToMenu);
        isBuilt = true;
        LayoutPage();
        RefreshCards();
    }

    private SkinCard CreateCard(int index)
    {
        GameObject cardObject = new GameObject("Skin Card " + (index + 1).ToString("00"), typeof(RectTransform), typeof(Image));
        cardObject.transform.SetParent(transform, false);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        Image cardImage = cardObject.GetComponent<Image>();
        cardImage.sprite = cardFrameSprite;
        cardImage.type = cardFrameSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        cardImage.color = new Color(0.035f, 0.105f, 0.2f, 0.96f);
        cardImage.raycastTarget = false;

        GameObject accentObject = new GameObject("Skin Accent", typeof(RectTransform), typeof(Image));
        accentObject.transform.SetParent(cardObject.transform, false);
        Image accent = accentObject.GetComponent<Image>();
        accent.color = GetSkinTint(index);
        accent.raycastTarget = false;

        GameObject previewObject = new GameObject("Skin Preview", typeof(RectTransform), typeof(Image));
        previewObject.transform.SetParent(cardObject.transform, false);
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        if (index == 0)
        {
            previewRect.localScale = Vector3.one * DefaultShopPreviewScale;
        }

        Image preview = previewObject.GetComponent<Image>();
        preview.sprite = index < previews.Length ? previews[index] : null;
        preview.preserveAspect = true;
        preview.color = ShopPreviewBlueTint;
        preview.material = shopPreviewTintMaterial;
        preview.raycastTarget = false;

        TMP_Text name = CreateText("Skin Name", "RUNNER " + (index + 1).ToString("00"), 18f, Color.white, TextAlignmentOptions.Center);
        name.transform.SetParent(cardObject.transform, false);

        Button action = CreateButton(cardObject.transform, "Skin Action", string.Empty, buttonBlueSprite, Color.white);
        TMP_Text actionLabel = CreateText("Skin Action Label", string.Empty, 17f, Color.white, TextAlignmentOptions.Center);
        actionLabel.transform.SetParent(action.transform, false);

        return new SkinCard
        {
            root = cardRect,
            previewRect = previewRect,
            preview = preview,
            accent = accent,
            title = name,
            action = action,
            actionText = actionLabel
        };
    }

    private void LayoutPage()
    {
        if (panelRect == null)
        {
            return;
        }

        float width = Mathf.Max(320f, panelRect.rect.width);
        float height = Mathf.Max(300f, panelRect.rect.height);
        bool narrow = Screen.width < 760;
        int columns = narrow ? 2 : 3;
        float sidePadding = narrow ? 20f : 26f;
        float columnGap = narrow ? 12f : 14f;
        float cardWidth = (width - sidePadding * 2f - columnGap * (columns - 1)) / columns;
        float cardHeight = narrow ? 72f : 112f;
        float rowGap = narrow ? 8f : 12f;
        float gridTop = height * 0.5f - (narrow ? 100f : 103f);

        Place(titleText.rectTransform, new Vector2(-width * 0.12f, height * 0.5f - 35f), new Vector2(width * 0.56f, 54f));
        titleText.fontSize = narrow ? 44f : 38f;
        Place(subtitleText.rectTransform, new Vector2(0f, height * 0.5f - 76f), new Vector2(width - 48f, 28f));
        subtitleText.fontSize = narrow ? 22f : 18f;
        Place(walletText.rectTransform, new Vector2(width * 0.32f, height * 0.5f - 35f), new Vector2(width * 0.3f, 44f));
        walletText.fontSize = narrow ? 22f : 20f;

        for (int i = 0; i < cards.Length; i++)
        {
            SkinCard card = cards[i];
            if (card == null || card.root == null)
            {
                continue;
            }

            int row = i / columns;
            int column = i % columns;
            float totalWidth = columns * cardWidth + (columns - 1) * columnGap;
            float x = -totalWidth * 0.5f + cardWidth * 0.5f + column * (cardWidth + columnGap);
            float y = gridTop - cardHeight * 0.5f - row * (cardHeight + rowGap);
            Place(card.root, new Vector2(x, y), new Vector2(cardWidth, cardHeight));

            float previewSize = Mathf.Min(cardHeight - 10f, cardWidth * 0.32f);
            RectTransform accentRect = card.accent.rectTransform;
            accentRect.pivot = new Vector2(0f, 0.5f);
            Place(accentRect, new Vector2(-cardWidth * 0.5f + 4f, 0f), new Vector2(5f, cardHeight - 14f));
            Place(card.previewRect, new Vector2(-cardWidth * 0.32f, 0f), new Vector2(previewSize, previewSize));

            float contentCenter = cardWidth * 0.15f;
            float contentWidth = cardWidth * 0.55f;
            Place(card.title.rectTransform, new Vector2(contentCenter, cardHeight * 0.24f), new Vector2(contentWidth, cardHeight * 0.35f));
            card.title.fontSize = narrow ? 22f : 18f;
            Place(card.action.GetComponent<RectTransform>(), new Vector2(contentCenter, -cardHeight * 0.22f), new Vector2(contentWidth, narrow ? 26f : 34f));
            Place(card.actionText.rectTransform, Vector2.zero, new Vector2(contentWidth - 10f, narrow ? 24f : 30f));
            card.actionText.fontSize = narrow ? 21f : 17f;
        }

        float bottomY = -height * 0.5f + (narrow ? 24f : 28f);
        float backWidth = Mathf.Min(narrow ? 168f : 188f, width * 0.32f);
        Place(backButton.GetComponent<RectTransform>(), new Vector2(-width * 0.5f + sidePadding + backWidth * 0.5f, bottomY), new Vector2(backWidth, narrow ? 34f : 40f));
        Place(statusText.rectTransform, new Vector2(width * 0.5f - sidePadding - (width - backWidth - 3f * sidePadding) * 0.5f, bottomY), new Vector2(width - backWidth - 3f * sidePadding, 38f));
        statusText.fontSize = narrow ? 17f : 16f;
    }

    private void RefreshCards()
    {
        if (!isBuilt)
        {
            return;
        }

        int ownedMask = GetOwnedMask();
        int equipped = EquippedSkinId;
        if ((ownedMask & (1 << equipped)) == 0)
        {
            equipped = 0;
            PlayerPrefs.SetInt(EquippedKey, equipped);
            PlayerPrefs.Save();
        }

        if (walletText != null)
        {
            int balance = wallet != null ? wallet.WalletCoins : 0;
            walletText.text = "WALLET  " + balance.ToString("N0") + " COINS";
        }

        if (statusText != null && !string.IsNullOrEmpty(currentStatus))
        {
            statusText.text = currentStatus;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            SkinCard card = cards[i];
            if (card == null)
            {
                continue;
            }

            bool isOwned = (ownedMask & (1 << i)) != 0;
            card.actionText.text = i == equipped ? "EQUIPPED" : isOwned ? "EQUIP" : "BUY " + GetPrice(i).ToString("N0");
            card.action.image.sprite = i == equipped ? buttonGreenSprite : buttonBlueSprite;
            card.action.image.type = card.action.image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            card.action.onClick.RemoveAllListeners();
            int capturedIndex = i;
            card.action.onClick.AddListener(() => BuyOrEquip(capturedIndex));
        }
    }

    private void BuyOrEquip(int skinId)
    {
        if (skinId < 0 || skinId >= SkinCount)
        {
            return;
        }

        wallet ??= CurrencyWallet.Instance != null ? CurrencyWallet.Instance : FindFirstObjectByType<CurrencyWallet>();
        int ownedMask = GetOwnedMask();
        int bit = 1 << skinId;
        bool purchased = false;
        if ((ownedMask & bit) == 0)
        {
            int price = GetPrice(skinId);
            if (wallet == null)
            {
                SetStatus("WALLET IS UNAVAILABLE. TRY REOPENING THE MENU.");
                RefreshCards();
                return;
            }

            if (!wallet.SpendWalletCoins(price))
            {
                int shortfall = price - wallet.WalletCoins;
                SetStatus("NEED " + shortfall.ToString("N0") + " MORE COINS.");
                RefreshCards();
                return;
            }

            ownedMask |= bit;
            PlayerPrefs.SetInt(OwnedKey, ownedMask);
            purchased = true;
        }

        PlayerPrefs.SetInt(EquippedKey, skinId);
        PlayerPrefs.Save();
        PlayerCrowdManager.ApplyEquippedSkinToActiveCrowds(skinId);
        currentStatus = purchased
            ? "RUNNER " + (skinId + 1).ToString("00") + " UNLOCKED AND EQUIPPED."
            : "RUNNER " + (skinId + 1).ToString("00") + " EQUIPPED.";

        RefreshCards();
    }

    private void BackToMenu()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BackToMainMenu();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SetStatus(string message)
    {
        currentStatus = message;
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private int GetPrice(int skinId)
    {
        return prices != null && skinId < prices.Length ? Mathf.Max(0, prices[skinId]) : skinId * 150;
    }

    private static int GetOwnedMask()
    {
        EnsureDefaultOwnership();
        return PlayerPrefs.GetInt(OwnedKey, 1) | 1;
    }

    private static void EnsureDefaultOwnership()
    {
        if (!PlayerPrefs.HasKey(OwnedKey))
        {
            PlayerPrefs.SetInt(OwnedKey, 1);
            PlayerPrefs.SetInt(EquippedKey, 0);
            PlayerPrefs.Save();
        }
    }

    private TMP_Text CreateText(string objectName, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string objectName, string label, Sprite sprite, Color labelColor)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = Color.white;
        colorBlock.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colorBlock.pressedColor = new Color(0.82f, 0.9f, 1f, 1f);
        button.colors = colorBlock;

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text buttonLabel = CreateText("Label", label, 18f, labelColor, TextAlignmentOptions.Center);
            buttonLabel.transform.SetParent(buttonObject.transform, false);
            Place(buttonLabel.rectTransform, Vector2.zero, new Vector2(170f, 34f));
        }

        if (sfxPlayer != null) sfxPlayer.RegisterButton(button);
        return button;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
