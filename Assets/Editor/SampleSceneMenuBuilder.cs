#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SampleSceneMenuBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string LayerLabRoot = "Assets/Layer Lab/GUI Pro-CasualGame/ResourcesData/";
    private const string ClashShopRoot = "Assets/Models/Assets/Clash Masters/Sprites/Shop/";

    public static void AddSkinShopToOpenMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, MenuScenePath, StringComparison.OrdinalIgnoreCase))
        {
            scene = SceneManager.GetSceneByPath(MenuScenePath);
        }

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("MenuSceneBuilder: open Menu before adding the skin shop.");
            return;
        }

        GameObject canvasObject = FindInScene(scene, "Canvas");
        Transform menuRoot = canvasObject != null ? canvasObject.transform.Find("MenuRoot") : null;
        Transform home = menuRoot != null ? menuRoot.Find("HomePanel") : null;
        UIManager uiManager = canvasObject != null ? canvasObject.GetComponent<UIManager>() : null;
        if (canvasObject == null || menuRoot == null || home == null || uiManager == null)
        {
            Debug.LogError("MenuSceneBuilder: Canvas, MenuRoot/HomePanel, and UIManager are required to add the skin shop.");
            return;
        }

        if (SceneManager.GetActiveScene() != scene)
        {
            EditorSceneManager.SetActiveScene(scene);
        }

        Sprite panelBackground = LoadSprite(LayerLabRoot + "Sprites/Components/Frame/PanelFrame01_Round_Bg.png");
        Sprite panelLine = LoadSprite(LayerLabRoot + "Sprites/Components/Frame/PanelFrame01_Round_Line.png");
        Sprite buttonBlue = LoadSprite(LayerLabRoot + "Sprites/Components/Button/Button01_225_Blue.png");
        Sprite buttonGreen = LoadSprite(LayerLabRoot + "Sprites/Components/Button/Button01_225_Green.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LayerLabRoot + "Fonts/LilitaOne-Regular Outline 54 SDF.asset");

        Button shopButton = home.Find("Skin Shop Button")?.GetComponent<Button>();
        if (shopButton == null)
        {
            shopButton = CreateButton(home, "Skin Shop Button", "SKIN SHOP", new Vector2(0f, -136f), new Vector2(310f, 52f), buttonBlue, font);
        }
        SetButtonLabel(shopButton, "SKIN SHOP");
        SetRectPosition(shopButton.GetComponent<RectTransform>(), new Vector2(0f, -136f), new Vector2(310f, 52f));

        Button exitButton = home.Find("Exit Button")?.GetComponent<Button>();
        if (exitButton != null)
        {
            SetRectPosition(exitButton.GetComponent<RectTransform>(), new Vector2(0f, -196f), new Vector2(310f, 52f));
        }

        Transform footer = home.Find("MenuFooter");
        if (footer != null)
        {
            SetRectPosition(footer.GetComponent<RectTransform>(), new Vector2(0f, -240f), new Vector2(640f, 24f));
        }

        GameObject shopPanel = menuRoot.Find("SkinShopPanel") != null
            ? menuRoot.Find("SkinShopPanel").gameObject
            : CreatePanel(menuRoot, "SkinShopPanel", new Vector2(900f, 570f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        shopPanel.SetActive(false);
        CurrencyWallet wallet = canvasObject.GetComponent<CurrencyWallet>();
        if (wallet == null)
        {
            wallet = Undo.AddComponent<CurrencyWallet>(canvasObject);
        }

        SkinShopController controller = shopPanel.GetComponent<SkinShopController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<SkinShopController>(shopPanel);
        }
        ConfigureSkinShop(controller, wallet, panelBackground, buttonBlue, buttonGreen, font);

        SerializedObject serializedUi = new SerializedObject(uiManager);
        SetObject(serializedUi, "skinShopPanel", shopPanel);
        SetObject(serializedUi, "skinShopButton", shopButton);
        SetObject(serializedUi, "skinShopController", controller);
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(wallet);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MenuSceneBuilder: added the Clash Masters preview-based skin shop to Menu.");
    }

    [MenuItem("SpiralSquad/Build Menu")]
    public static void BuildFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(MenuScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("MenuSceneBuilder: open Menu before building the menu.");
            return;
        }

        Build(scene);
    }

    public static void Build(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("MenuSceneBuilder: Menu is not loaded.");
            return;
        }

        if (SceneManager.GetActiveScene() != scene)
        {
            EditorSceneManager.SetActiveScene(scene);
        }

        GameObject canvasObject = FindInScene(scene, "Canvas");
        if (canvasObject == null)
        {
            Debug.LogError("MenuSceneBuilder: UI/Canvas was not found.");
            return;
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        UIManager uiManager = canvasObject.GetComponent<UIManager>();
        if (canvas == null || uiManager == null)
        {
            Debug.LogError("MenuSceneBuilder: UI/Canvas must contain Canvas and UIManager.");
            return;
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform existingMenu = canvas.transform.Find("MenuRoot");
        if (existingMenu != null)
        {
            UnityEngine.Object.DestroyImmediate(existingMenu.gameObject);
        }

        Sprite panelBackground = LoadSprite(LayerLabRoot + "Sprites/Components/Frame/PanelFrame01_Round_Bg.png");
        Sprite panelLine = LoadSprite(LayerLabRoot + "Sprites/Components/Frame/PanelFrame01_Round_Line.png");
        Sprite buttonBlue = LoadSprite(LayerLabRoot + "Sprites/Components/Button/Button01_225_Blue.png");
        Sprite buttonGreen = LoadSprite(LayerLabRoot + "Sprites/Components/Button/Button01_225_Green.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LayerLabRoot + "Fonts/LilitaOne-Regular Outline 54 SDF.asset");

        GameObject menuRoot = CreateRect("MenuRoot", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backdrop = menuRoot.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.035f, 0.08f, 0.98f);
        backdrop.raycastTarget = true;
        menuRoot.AddComponent<CanvasGroup>();

        GameObject home = CreatePanel(menuRoot.transform, "HomePanel", new Vector2(700f, 530f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CreateText(home.transform, "MenuTitle", "SPIRAL SQUAD", 54f, new Color(1f, 0.86f, 0.24f), TextAlignmentOptions.Center, new Vector2(0f, 198f), new Vector2(620f, 76f), font);
        CreateText(home.transform, "MenuSubtitle", "RUN THE SPIRAL • BUILD YOUR SQUAD", 18f, new Color(0.66f, 0.9f, 1f), TextAlignmentOptions.Center, new Vector2(0f, 153f), new Vector2(620f, 30f), font);

        Button startButton = CreateButton(home.transform, "Start Button", "START GAME", new Vector2(0f, 104f), new Vector2(310f, 52f), buttonGreen, font);
        Button levelSelectButton = CreateButton(home.transform, "Level Select Button", "LEVEL SELECT", new Vector2(0f, 44f), new Vector2(310f, 52f), buttonBlue, font);
        Button settingsButton = CreateButton(home.transform, "Settings Button", "SETTINGS", new Vector2(0f, -16f), new Vector2(310f, 52f), buttonBlue, font);
        Button helpButton = CreateButton(home.transform, "Help Button", "HELP / CONTROLS", new Vector2(0f, -76f), new Vector2(310f, 52f), buttonBlue, font);
        Button skinShopButton = CreateButton(home.transform, "Skin Shop Button", "SKIN SHOP", new Vector2(0f, -136f), new Vector2(310f, 52f), buttonBlue, font);
        Button exitButton = CreateButton(home.transform, "Exit Button", "EXIT", new Vector2(0f, -196f), new Vector2(310f, 52f), buttonBlue, font);
        CreateText(home.transform, "MenuFooter", "SELECT A LEVEL • SURVIVE THE HAZARDS • REACH THE TOP", 13f, new Color(0.55f, 0.72f, 0.86f), TextAlignmentOptions.Center, new Vector2(0f, -240f), new Vector2(640f, 24f), font);

        GameObject levelPanel = CreatePanel(menuRoot.transform, "LevelSelectPanel", new Vector2(820f, 540f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CreateText(levelPanel.transform, "Level Select Title", "SELECT LEVEL", 42f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 215f), new Vector2(700f, 58f), font);
        TMP_Text selectedLevelText = CreateText(levelPanel.transform, "Selected Level Text", "SELECTED: LEVEL 1", 17f, new Color(1f, 0.86f, 0.24f), TextAlignmentOptions.Center, new Vector2(0f, 175f), new Vector2(700f, 30f), font);

        string[] levelNames = { "LEVEL 1 • RAMP-UP RUN", "LEVEL 2 • MOMENTUM TRAPWORKS", "LEVEL 3 • PULSEBOUND FOUNDRY", "LEVEL 4 • VAULTLINE CITADEL", "LEVEL 5 • FRACTURE RELAY" };
        string[] levelScenes = { "Level1", "Level2", "Level3", "Level4", "Level5" };
        Button[] levelButtons = new Button[5];
        Vector2[] levelPositions = { new Vector2(-180f, 112f), new Vector2(180f, 112f), new Vector2(-180f, 42f), new Vector2(180f, 42f), new Vector2(0f, -28f) };
        for (int i = 0; i < levelButtons.Length; i++)
        {
            levelButtons[i] = CreateButton(levelPanel.transform, "Level" + (i + 1) + " Button", levelNames[i], levelPositions[i], new Vector2(320f, 54f), i == 0 ? buttonGreen : buttonBlue, font);
        }

        Button levelBackButton = CreateButton(levelPanel.transform, "Level Select Back Button", "BACK", new Vector2(0f, -155f), new Vector2(240f, 50f), buttonBlue, font);

        GameObject settingsPanel = CreatePanel(menuRoot.transform, "SettingsPanel", new Vector2(620f, 420f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CreateText(settingsPanel.transform, "Settings Title", "SETTINGS", 42f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 145f), new Vector2(540f, 58f), font);
        Button soundButton = CreateButton(settingsPanel.transform, "Sound Toggle Button", "SOUND: ON", new Vector2(0f, 35f), new Vector2(320f, 56f), buttonGreen, font, "Sound Toggle Text");
        Button settingsBackButton = CreateButton(settingsPanel.transform, "Settings Back Button", "BACK", new Vector2(0f, -90f), new Vector2(240f, 50f), buttonBlue, font);
        CreateText(settingsPanel.transform, "Settings Note", "Sound changes apply for this play session.", 15f, new Color(0.65f, 0.82f, 0.94f), TextAlignmentOptions.Center, new Vector2(0f, -25f), new Vector2(520f, 30f), font);

        GameObject helpPanel = CreatePanel(menuRoot.transform, "HelpPanel", new Vector2(780f, 500f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CreateText(helpPanel.transform, "Help Title", "HOW TO PLAY", 42f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 185f), new Vector2(680f, 58f), font);
        TMP_Text helpText = CreateText(helpPanel.transform, "Help Content", "DRAG OR USE A / D (LEFT / RIGHT) TO STEER\\n\\nSPACE OR PRIMARY TOUCH JUMPS\\n\\nCHOOSE THE STRONGER GATE, DODGE HAZARDS, AND KEEP YOUR SQUAD ALIVE\\n\\nREACH THE FINISH WITH AS MANY RUNNERS AS POSSIBLE", 20f, new Color(0.86f, 0.94f, 1f), TextAlignmentOptions.Center, new Vector2(0f, 28f), new Vector2(660f, 230f), font);
        helpText.textWrappingMode = TextWrappingModes.Normal;
        Button helpBackButton = CreateButton(helpPanel.transform, "Help Back Button", "BACK", new Vector2(0f, -175f), new Vector2(240f, 50f), buttonBlue, font);

        GameObject loadingPanel = CreatePanel(menuRoot.transform, "LoadingPanel", new Vector2(620f, 300f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CreateText(loadingPanel.transform, "Loading Title", "LOADING", 42f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 82f), new Vector2(540f, 56f), font);
        TMP_Text loadingText = CreateText(loadingPanel.transform, "Loading Text", "LOADING...", 17f, new Color(0.72f, 0.9f, 1f), TextAlignmentOptions.Center, new Vector2(0f, 30f), new Vector2(540f, 30f), font);
        Slider loadingProgress = CreateProgressSlider(loadingPanel.transform, "Loading Progress", new Vector2(0f, -30f));

        GameObject errorPanel = CreatePanel(menuRoot.transform, "ErrorPanel", new Vector2(640f, 350f), Vector2.zero, panelBackground, panelLine, new Color(0.28f, 0.12f, 0.18f, 0.98f));
        CreateText(errorPanel.transform, "Error Title", "UNAVAILABLE", 38f, new Color(1f, 0.78f, 0.3f), TextAlignmentOptions.Center, new Vector2(0f, 110f), new Vector2(560f, 52f), font);
        TMP_Text errorMessageText = CreateText(errorPanel.transform, "Error Message Text", "This level is not available in the current build.", 17f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 38f), new Vector2(560f, 70f), font);
        errorMessageText.textWrappingMode = TextWrappingModes.Normal;
        Button errorRetryButton = CreateButton(errorPanel.transform, "Error Retry Button", "RETRY", new Vector2(-130f, -90f), new Vector2(230f, 50f), buttonGreen, font);
        Button errorBackButton = CreateButton(errorPanel.transform, "Error Back Button", "BACK", new Vector2(130f, -90f), new Vector2(230f, 50f), buttonBlue, font);

        GameObject exitInfoPanel = CreatePanel(menuRoot.transform, "ExitInfoPanel", new Vector2(620f, 330f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CreateText(exitInfoPanel.transform, "Exit Info Title", "EXIT", 40f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 102f), new Vector2(540f, 54f), font);
        TMP_Text exitInfoText = CreateText(exitInfoPanel.transform, "Exit Info Text", "EXIT is available in a built player. Close Play Mode to return to the editor.", 17f, new Color(0.86f, 0.94f, 1f), TextAlignmentOptions.Center, new Vector2(0f, 25f), new Vector2(540f, 70f), font);
        exitInfoText.textWrappingMode = TextWrappingModes.Normal;
        Button exitInfoCloseButton = CreateButton(exitInfoPanel.transform, "Exit Info Close Button", "CLOSE", new Vector2(0f, -90f), new Vector2(240f, 50f), buttonBlue, font);

        GameObject skinShopPanel = CreatePanel(menuRoot.transform, "SkinShopPanel", new Vector2(900f, 570f), Vector2.zero, panelBackground, panelLine, new Color(0.12f, 0.26f, 0.48f, 0.97f));
        CurrencyWallet wallet = canvasObject.GetComponent<CurrencyWallet>();
        if (wallet == null) wallet = Undo.AddComponent<CurrencyWallet>(canvasObject);
        SkinShopController skinShopController = skinShopPanel.AddComponent<SkinShopController>();
        ConfigureSkinShop(skinShopController, wallet, panelBackground, buttonBlue, buttonGreen, font);

        SetPanelStates(home, levelPanel, settingsPanel, helpPanel, loadingPanel, errorPanel, exitInfoPanel, skinShopPanel);
        SetLegacyObjectsInactive(canvas.transform);
        DisableGameplayRoots(scene);
        ConfigureMenuCamera(scene);

        SerializedObject serializedUi = new SerializedObject(uiManager);
        SetObject(serializedUi, "mainMenuPanel", home);
        SetObject(serializedUi, "menuRoot", menuRoot);
        SetObject(serializedUi, "levelSelectPanel", levelPanel);
        SetObject(serializedUi, "settingsPanel", settingsPanel);
        SetObject(serializedUi, "helpPanel", helpPanel);
        SetObject(serializedUi, "loadingPanel", loadingPanel);
        SetObject(serializedUi, "errorPanel", errorPanel);
        SetObject(serializedUi, "exitInfoPanel", exitInfoPanel);
        SetObject(serializedUi, "skinShopPanel", skinShopPanel);
        SetObject(serializedUi, "skinShopController", skinShopController);
        SetObject(serializedUi, "startButton", startButton);
        SetObject(serializedUi, "levelSelectButton", levelSelectButton);
        SetObject(serializedUi, "settingsButton", settingsButton);
        SetObject(serializedUi, "helpButton", helpButton);
        SetObject(serializedUi, "skinShopButton", skinShopButton);
        SetObject(serializedUi, "exitButton", exitButton);
        SetObject(serializedUi, "soundToggleButton", soundButton);
        SetObject(serializedUi, "levelSelectBackButton", levelBackButton);
        SetObject(serializedUi, "settingsBackButton", settingsBackButton);
        SetObject(serializedUi, "helpBackButton", helpBackButton);
        SetObject(serializedUi, "errorRetryButton", errorRetryButton);
        SetObject(serializedUi, "errorBackButton", errorBackButton);
        SetObject(serializedUi, "exitInfoCloseButton", exitInfoCloseButton);
        SetObject(serializedUi, "selectedLevelText", selectedLevelText);
        SetObject(serializedUi, "soundToggleText", soundButton.GetComponentInChildren<TMP_Text>(true));
        SetObject(serializedUi, "loadingText", loadingText);
        SetObject(serializedUi, "loadingProgress", loadingProgress);
        SetObject(serializedUi, "errorMessageText", errorMessageText);
        SetObject(serializedUi, "exitInfoText", exitInfoText);
        serializedUi.FindProperty("menuMode").boolValue = true;
        serializedUi.FindProperty("menuSceneName").stringValue = "Menu";
        serializedUi.FindProperty("selectedLevelIndex").intValue = 0;

        SerializedProperty entries = serializedUi.FindProperty("levelEntries");
        entries.arraySize = levelButtons.Length;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            SerializedProperty element = entries.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("displayName").stringValue = levelNames[i];
            element.FindPropertyRelative("sceneName").stringValue = levelScenes[i];
            element.FindPropertyRelative("button").objectReferenceValue = levelButtons[i];
        }

        serializedUi.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);

        EventSystem eventSystem = FindInScene(scene, "EventSystem")?.GetComponent<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.firstSelectedGameObject = startButton.gameObject;
            EditorUtility.SetDirty(eventSystem);
        }

        EditorBuildSettings.scenes = BuildSceneList();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MenuSceneBuilder: built menu hierarchy and set shared build order Menu, Level1, Level2, Level3, Level4, Level5.");
    }

    private static EditorBuildSettingsScene[] BuildSceneList()
    {
        string[] paths = { "Assets/Scenes/Menu.unity", "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity", "Assets/Scenes/Level3.unity", "Assets/Scenes/Level4.unity", "Assets/Scenes/Level5.unity" };
        List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>(paths.Length);
        for (int i = 0; i < paths.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(paths[i]) == null)
            {
                Debug.LogWarning("MenuSceneBuilder: scene asset not found: " + paths[i]);
                continue;
            }

            result.Add(new EditorBuildSettingsScene(paths[i], true));
        }

        return result.ToArray();
    }

    private static void ConfigureSkinShop(SkinShopController controller, CurrencyWallet wallet, Sprite frame, Sprite buttonBlue, Sprite buttonGreen, TMP_FontAsset font)
    {
        Sprite[] previews = new Sprite[9];
        for (int i = 0; i < previews.Length; i++)
        {
            previews[i] = LoadSprite(ClashShopRoot + "Shop_Item_" + i.ToString("00") + ".png");
        }

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "wallet", wallet);
        SetObject(serialized, "cardFrameSprite", frame);
        SetObject(serialized, "buttonBlueSprite", buttonBlue);
        SetObject(serialized, "buttonGreenSprite", buttonGreen);
        SetObject(serialized, "font", font);
        SetObjectArray(serialized, "previews", previews);
        SetIntArray(serialized, "prices", new[] { 0, 150, 300, 450, 600, 750, 900, 1050, 1200 });
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void SetObjectArray(SerializedObject serialized, string propertyName, UnityEngine.Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray) return;
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetIntArray(SerializedObject serialized, string propertyName, int[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray) return;
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).intValue = values[i];
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null) text.text = label;
    }

    private static void SetRectPosition(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetPanelStates(params GameObject[] panels)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) panels[i].SetActive(i == 0);
        }
    }

    private static void SetLegacyObjectsInactive(Transform canvas)
    {
        string[] names = { "MainMenu", "HudPanel", "GameOverPanel", "YouWinPanel" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = canvas.Find(names[i]);
            if (child != null) child.gameObject.SetActive(false);
        }
    }

    private static void DisableGameplayRoots(Scene scene)
    {
        string[] names = { "Environment", "Hazards", "Spawner", "Player", "Main Camera", "FreeLook Camera" };
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            for (int j = 0; j < names.Length; j++)
            {
                if (string.Equals(roots[i].name, names[j], StringComparison.OrdinalIgnoreCase))
                {
                    roots[i].SetActive(false);
                    break;
                }
            }
        }
    }

    private static void ConfigureMenuCamera(Scene scene)
    {
        GameObject menuCameraObject = FindInScene(scene, "Menu Camera");
        if (menuCameraObject == null)
        {
            menuCameraObject = new GameObject("Menu Camera");
            SceneManager.MoveGameObjectToScene(menuCameraObject, scene);
        }

        menuCameraObject.SetActive(true);
        menuCameraObject.tag = "MainCamera";
        menuCameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);

        Camera menuCamera = menuCameraObject.GetComponent<Camera>();
        if (menuCamera == null) menuCamera = menuCameraObject.AddComponent<Camera>();
        menuCamera.enabled = true;
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.015f, 0.035f, 0.08f, 1f);
        menuCamera.cullingMask = 0;
        menuCamera.orthographic = true;
        menuCamera.orthographicSize = 5f;
        menuCamera.nearClipPlane = 0.1f;
        menuCamera.farClipPlane = 100f;
        menuCamera.depth = -100f;
        menuCamera.allowHDR = false;
        menuCamera.allowMSAA = false;
        menuCamera.useOcclusionCulling = false;

        AudioListener listener = menuCameraObject.GetComponent<AudioListener>();
        if (listener == null) listener = menuCameraObject.AddComponent<AudioListener>();
        listener.enabled = true;

        UniversalAdditionalCameraData cameraData = menuCameraObject.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null) cameraData = menuCameraObject.AddComponent<UniversalAdditionalCameraData>();

        EditorUtility.SetDirty(menuCameraObject);
        EditorUtility.SetDirty(menuCamera);
        EditorUtility.SetDirty(listener);
        EditorUtility.SetDirty(cameraData);
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (string.Equals(children[j].name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return children[j].gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        if (anchorMin == anchorMax)
        {
            rect.sizeDelta = size;
        }
        else
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position, Sprite background, Sprite line, Color color)
    {
        GameObject panel = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Image image = panel.AddComponent<Image>();
        image.sprite = background;
        image.type = background != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = true;
        if (line != null)
        {
            GameObject border = CreateRect("Border", panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image borderImage = border.AddComponent<Image>();
            borderImage.sprite = line;
            borderImage.type = Image.Type.Sliced;
            borderImage.color = Color.white;
            borderImage.raycastTarget = false;
        }

        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 size, TMP_FontAsset font, string childName = null)
    {
        GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        if (!string.IsNullOrWhiteSpace(childName)) go.name = childName;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Sprite sprite, TMP_FontAsset font, string labelName = null)
    {
        GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.7f, 1f);
        colors.pressedColor = new Color(0.72f, 0.84f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.5f, 0.58f, 0.75f);
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        CreateText(go.transform, labelName ?? "Text (TMP)", label, 22f, Color.white, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(18f, 8f), font);
        return button;
    }

    private static Slider CreateProgressSlider(Transform parent, string name, Vector2 position)
    {
        GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(470f, 24f));
        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        GameObject background = CreateRect("Background", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.03f, 0.08f, 0.16f, 1f);
        GameObject fillArea = CreateRect("Fill Area", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(3f, 3f);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(-3f, -3f);
        GameObject fill = CreateRect("Fill", fillArea.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.18f, 0.86f, 1f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite loadedSprite) return loadedSprite;
        }

        return null;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }
}
#endif
