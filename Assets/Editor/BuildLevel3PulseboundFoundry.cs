using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLevel3PulseboundFoundry
{
    private const string PulsePlatePrefabPath = "Assets/Prefabs/Traps/Level3/PulsePlate.prefab";
    private const string PulsePlatePrefabGuid = "3857dd2ec61e66f4ba246095bb02bec2";
    private const string SwingHammerPrefabPath = "Assets/Prefabs/Traps/Shared/SwingHammer.prefab";
    private const string SwingHammerPrefabGuid = "6a636c385c2777a4e9b2f5f288707ec5";

    private static Material pulseBody;
    private static Material pulseCharge;
    private static Material pulseDischarge;
    private static Material spikeMaterial;
    private static Material railMaterial;
    private static Material hammerMaterial;
    private static Material cannonMaterial;
    private static Material markerMaterial;
    private static GameObject pulsePlatePrefab;
    private static GameObject swingHammerPrefab;

    [MenuItem("Spiral Squad/Levels/Build Level3 - Pulsebound Foundry")]
    public static void Main()
    {
        Build(EditorSceneManager.GetActiveScene(), save: true);
    }

    /// <summary>
    /// Builds the Level 3 route inside the supplied scene only.  Passing the
    /// scene explicitly is important when Level1/Level3/Level5 are open
    /// additively: no hierarchy lookup may escape the target scene.
    /// </summary>
    public static void Build(Scene scene, bool save)
    {
        if (!scene.IsValid() || scene.name != "Level3")
            throw new InvalidOperationException("Open Assets/Scenes/Level3.unity before building Level 3.");

        GameObject root = FindInScene(scene, "Level 3 - Pulsebound Foundry");
        if (root == null) root = FindInScene(scene, "Level 4 - Vaultline Citadel");
        if (root == null) root = FindInScene(scene, "Level 2 - Momentum Trapworks");
        if (root == null) throw new InvalidOperationException("Level3 root not found in the supplied scene.");
        pulsePlatePrefab = LoadPulsePlatePrefab();
        LoadMaterials(allowAssetWrite: save);
        root.name = "Level 3 - Pulsebound Foundry";
        ConfigureCore(scene, root);
        foreach (FinishGate finish in CollisionAuditValidator.Components<FinishGate>(scene))
            CollisionAuditValidator.AuthorFinish(finish);
        ConfigureGates(root);
        ConfigureHudAndCamera(scene);
        BuildTrapSections(root);
        LevelEnemyEncounterAuthoring.RebuildForLevel(root, 3);
        EditorSceneManager.MarkSceneDirty(scene);
        if (save)
        {
            SaveGeneratedMaterials();
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("LEVEL3_SCENE_BUILT: Pulsebound Foundry route built" + (save ? " and saved." : " without saving assets or scene."));
    }

    private static void LoadMaterials(bool allowAssetWrite)
    {
        pulseBody = EnsureMaterial("Level3_PulseBody", new Color(0.06f, 0.22f, 0.30f), allowAssetWrite);
        pulseCharge = EnsureMaterial("Level3_PulseCharge", new Color(0.10f, 0.75f, 1f), allowAssetWrite);
        pulseDischarge = EnsureMaterial("Level3_PulseDischarge", new Color(1f, 0.16f, 0.12f), allowAssetWrite);
        spikeMaterial = EnsureMaterial("Level3_Spikes", new Color(0.95f, 0.12f, 0.14f), allowAssetWrite);
        railMaterial = EnsureMaterial("Level3_Rail", new Color(0.08f, 0.08f, 0.12f), allowAssetWrite);
        hammerMaterial = EnsureMaterial("Level3_Hammer", new Color(1f, 0.58f, 0.08f), allowAssetWrite);
        cannonMaterial = EnsureMaterial("Level3_Cannon", new Color(0.36f, 0.12f, 0.55f), allowAssetWrite);
        markerMaterial = EnsureMaterial("Level3_Marker", new Color(0.18f, 0.95f, 0.65f), allowAssetWrite);
    }

    private static GameObject LoadPulsePlatePrefab()
    {
        string guidPath = AssetDatabase.GUIDToAssetPath(PulsePlatePrefabGuid);
        if (!string.Equals(guidPath, PulsePlatePrefabPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PulsePlate GUID resolves to '" + guidPath + "', expected '" + PulsePlatePrefabPath + "'.");

        string actualGuid = AssetDatabase.AssetPathToGUID(PulsePlatePrefabPath);
        if (!string.Equals(actualGuid, PulsePlatePrefabGuid, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PulsePlate prefab at '" + PulsePlatePrefabPath + "' has GUID '" + actualGuid + "', expected '" + PulsePlatePrefabGuid + "'.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PulsePlatePrefabPath);
        if (prefab == null)
            throw new InvalidOperationException("PulsePlate prefab could not be loaded at '" + PulsePlatePrefabPath + "'.");
        if (prefab.GetComponent<PulsePlateHazard>() == null)
            throw new InvalidOperationException("PulsePlate prefab at '" + PulsePlatePrefabPath + "' is missing PulsePlateHazard.");
        return prefab;
    }

    private static void SaveGeneratedMaterials()
    {
        AssetDatabase.SaveAssetIfDirty(pulseBody);
        AssetDatabase.SaveAssetIfDirty(pulseCharge);
        AssetDatabase.SaveAssetIfDirty(pulseDischarge);
        AssetDatabase.SaveAssetIfDirty(spikeMaterial);
        AssetDatabase.SaveAssetIfDirty(railMaterial);
        AssetDatabase.SaveAssetIfDirty(hammerMaterial);
        AssetDatabase.SaveAssetIfDirty(cannonMaterial);
        AssetDatabase.SaveAssetIfDirty(markerMaterial);
    }

    private static void ConfigureCore(Scene scene, GameObject root)
    {
        Transform track = FindFirstChild(root,
            "Track/Track 10m x 300m",
            "Track/Track 10m x 340m",
            "Track/Track 10m x 360m");
        if (track == null) throw new InvalidOperationException("Track not found.");
        track.name = "Track 10m x 340m";
        track.localPosition = new Vector3(0f, -0.1f, 170f);
        track.localScale = new Vector3(10f, 0.2f, 340f);

        Transform startLine = root.transform.Find("Track/Start Line");
        if (startLine != null) startLine.localPosition = new Vector3(0f, 0.01f, 3f);
        Transform finishMarker = root.transform.Find("Track/Finish Marker");
        if (finishMarker != null) finishMarker.localPosition = new Vector3(0f, 0.02f, 335f);
        Transform finish = root.transform.Find("FinishGate");
        if (finish != null) finish.localPosition = new Vector3(0f, 0f, 338f);

        GameObject player = FindInScene(scene, "Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0f, 3f);
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                SetFloat(controller, "forwardSpeed", 6f);
                SetFloat(controller, "startForwardSpeed", 6f);
                SetFloat(controller, "maxForwardSpeed", 18f);
                SetFloat(controller, "speedRampStartZ", 3f);
                SetFloat(controller, "speedRampEndZ", 169f);
            }
        }
    }

    private static void ConfigureGates(GameObject root)
    {
        ConfigureGate(FindFirstChild(root, "Gates/Gate A Z24", "Gates/Gate A Z28"),
            "Gate A Z28", 28f, 30, 2);
        ConfigureGate(FindFirstChild(root, "Gates/Gate B Z104", "Gates/Gate B Z128"),
            "Gate B Z128", 128f, 50, 2);
        ConfigureGate(FindFirstChild(root, "Gates/Gate C Z240", "Gates/Gate C Z238"),
            "Gate C Z240", 240f, 75, 2);
    }

    private static void ConfigureGate(Transform gateRoot, string newName, float z, int addValue, int multiplyValue)
    {
        if (gateRoot == null) throw new InvalidOperationException("Expected gate root not found.");
        gateRoot.name = newName;
        gateRoot.localPosition = new Vector3(0f, 0f, z);
        Gate[] gates = gateRoot.GetComponentsInChildren<Gate>(true);
        if (gates.Length != 2) throw new InvalidOperationException(newName + " expected two Gate components.");
        Array.Sort(gates, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        ConfigureGateComponent(gates[0], 0, addValue, "Left Gate +" + addValue);
        ConfigureGateComponent(gates[1], 1, multiplyValue, "Right Gate x" + multiplyValue);
    }

    private static void ConfigureGateComponent(Gate gate, int gateType, int value, string name)
    {
        gate.name = name;
        SetEnum(gate, "gateType", gateType);
        SetInt(gate, "value", value);
        gate.UpdateGateText();
    }

    private static void ConfigureHudAndCamera(Scene scene)
    {
        GameObject canvasObject = FindInScene(scene, "Level HUD Canvas");
        if (canvasObject != null)
        {
            LevelHud hud = canvasObject.GetComponent<LevelHud>();
            if (hud != null)
            {
                SetFloat(hud, "trackLength", 338f);
                SetString(hud, "levelLabel", "LEVEL 3 - PULSEBOUND FOUNDRY");
            }
            SetText(scene, "Level HUD Canvas/HUD Level Title", "LEVEL 3 - PULSEBOUND FOUNDRY");
            SetText(scene, "Level HUD Canvas/HUD Run Subtitle", "PULSEBOUND FOUNDRY");
        }

        SetText(scene, "Player/World Level Label", "LEVEL 3");
        GameObject camera = FindInScene(scene, "Level 2 Camera");
        if (camera == null) camera = FindInScene(scene, "Level 3 Camera");
        if (camera == null) camera = FindInScene(scene, "Level 4 Camera");
        if (camera != null) camera.name = "Level 3 Camera";
        GameObject follow = FindInScene(scene, "CM Level2 Crowd Follow");
        if (follow == null) follow = FindInScene(scene, "CM Level3 Crowd Follow");
        if (follow == null) follow = FindInScene(scene, "CM Level4 Crowd Follow");
        if (follow != null) follow.name = "CM Level3 Crowd Follow";
    }

    private static void BuildTrapSections(GameObject root)
    {
        Transform sections = root.transform.Find("Trap Sections");
        if (sections == null) throw new InvalidOperationException("Trap Sections parent not found.");
        ClearChildren(sections);

        Transform pulseTutorial = CreateSection(sections, "Pulse Plate Tutorial");
        CreatePulse(pulseTutorial, "Pulse Plate 01", 0f, 44f, 0f);
        CreatePulse(pulseTutorial, "Pulse Plate 02", -2.4f, 51f, 0.55f);
        CreatePulse(pulseTutorial, "Pulse Plate 03", 2.4f, 58f, 1.1f);
        CreatePulse(pulseTutorial, "Pulse Plate 04", -1.2f, 65f, 1.65f);
        CreatePulse(pulseTutorial, "Pulse Plate 05", 1.2f, 65f, 0.35f);

        Transform shuttleTutorial = CreateSection(sections, "Spike Shuttle Tutorial");
        CreateSpike(shuttleTutorial, "Spike Shuttle 01", 88f, -3.8f, 3.8f, 0f);
        CreateSpike(shuttleTutorial, "Spike Shuttle 02", 106f, 3.8f, -3.8f, 0f);

        Transform hammerAlley = CreateSection(sections, "Hammer Alley");
        CreateHammer(hammerAlley, "Swing Hammer 01", -2.4f, 148f, 0f);
        CreateHammer(hammerAlley, "Swing Hammer 02", 2.4f, 160f, 0.85f);
        CreateHammer(hammerAlley, "Swing Hammer 03", 0f, 172f, 1.7f);

        Transform cannonWalkway = CreateSection(sections, "Cannon Walkway");
        CreateCannon(cannonWalkway, "Side Cannon 01", -4.35f, 190f, 0f);
        CreateCannon(cannonWalkway, "Side Cannon 02", 4.35f, 202f, 0.85f);
        CreateCannon(cannonWalkway, "Side Cannon 03", -4.35f, 214f, 1.7f);
        CreateCannon(cannonWalkway, "Side Cannon 04", 4.35f, 222f, 2.55f);

        Transform exchange = CreateSection(sections, "Foundry Exchange");
        CreatePulse(exchange, "Exchange Pulse L", -2.4f, 254f, 0f);
        CreatePulse(exchange, "Exchange Pulse R", 2.4f, 254f, 1f);
        CreateSpike(exchange, "Spike Shuttle 03", 270f, -3.8f, 3.8f, 0f);
        CreateHammer(exchange, "Exchange Hammer L", -2.4f, 270f, 1.3f);
        CreateHammer(exchange, "Exchange Hammer R", 2.4f, 270f, 1.3f);
        CreatePulse(exchange, "Exchange Pulse C1", -1.2f, 278f, 0.4f);
        CreatePulse(exchange, "Exchange Pulse C2", 0f, 278f, 1.2f);
        CreatePulse(exchange, "Exchange Pulse C3", 1.2f, 278f, 1.8f);
        CreateMarker(exchange, "Recovery Marker", 0f, 288f);

        Transform finalLock = CreateSection(sections, "Final Pulse Lock");
        CreatePulse(finalLock, "Final Pulse L1", -2.4f, 296f, 0f);
        CreatePulse(finalLock, "Final Pulse R1", 2.4f, 296f, 0.8f);
        CreateSpike(finalLock, "Spike Shuttle 04", 312f, 3.8f, -3.8f, 3.75f);
        CreateHammer(finalLock, "Final Hammer", 0f, 312f, 1.25f);
        CreateCannon(finalLock, "Final Cannon L", -4.35f, 319f, 0f);
        CreateCannon(finalLock, "Final Cannon R", 4.35f, 319f, 1.25f);
        CreatePulse(finalLock, "Final Pulse L2", -1.4f, 326f, 1f);
        CreatePulse(finalLock, "Final Pulse R2", 1.4f, 326f, 1.6f);
        CreateMarker(finalLock, "Finish Recovery Marker", 0f, 330f);
    }

    private static Transform CreateSection(Transform parent, string name)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
        return section.transform;
    }

    private static void CreatePulse(Transform parent, string name, float x, float z, float phase)
    {
        if (pulsePlatePrefab == null)
            throw new InvalidOperationException("PulsePlate prefab was not validated before trap construction.");

        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(pulsePlatePrefab, parent);
        prefabInstance.name = name;
        prefabInstance.transform.localPosition = new Vector3(x, 0.12f, z);
        PulsePlateHazard hazard = prefabInstance.GetComponent<PulsePlateHazard>();
        if (hazard == null)
            throw new InvalidOperationException("PulsePlate prefab is missing PulsePlateHazard.");
        SetFloat(hazard, "chargeRingBaseRadius", 1.1f);
        SetFloat(hazard, "dischargeRingBaseRadius", 1.2f);
        SetFloat(hazard, "runnerHeight", 1.6f);
        SetFloat(hazard, "phaseOffset", phase);
    }

    private static void CreateSpike(Transform parent, string name, float z, float startX, float endX, float phase)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0.55f, z);
        CreatePrimitive("Rail", PrimitiveType.Cube, root.transform,
            new Vector3(0f, -0.48f, 0f), new Vector3(8.8f, 0.12f, 0.24f), railMaterial);
        CreatePrimitive("Start Endpoint", PrimitiveType.Cylinder, root.transform,
            new Vector3(startX, -0.28f, 0f), new Vector3(0.35f, 0.15f, 0.35f), markerMaterial);
        CreatePrimitive("End Endpoint", PrimitiveType.Cylinder, root.transform,
            new Vector3(endX, -0.28f, 0f), new Vector3(0.35f, 0.15f, 0.35f), markerMaterial);
        GameObject carriage = CreatePrimitive("Spiked Carriage", PrimitiveType.Cube, root.transform,
            new Vector3(startX, 0f, 0f), new Vector3(1.55f, 0.7f, 1.1f), spikeMaterial);
        CreatePrimitive("Spike Tip", PrimitiveType.Cylinder, carriage.transform,
            new Vector3(0.45f, 0.42f, 0f), new Vector3(0.18f, 0.28f, 0.18f), spikeMaterial);
        CreatePrimitive("Spike Tip", PrimitiveType.Cylinder, carriage.transform,
            new Vector3(0f, 0.42f, 0f), new Vector3(0.18f, 0.28f, 0.18f), spikeMaterial);
        CreatePrimitive("Spike Tip", PrimitiveType.Cylinder, carriage.transform,
            new Vector3(-0.45f, 0.42f, 0f), new Vector3(0.18f, 0.28f, 0.18f), spikeMaterial);
        SpikeSweepHazard hazard = root.AddComponent<SpikeSweepHazard>();
        SetFloat(hazard, "startX", startX);
        SetFloat(hazard, "endX", endX);
        SetFloat(hazard, "travelDuration", 0.5f);
        SetFloat(hazard, "endpointPause", 0.2f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "approachDistance", 24f);
        SetFloat(hazard, "carriageRadius", 0.775f);
        SetFloat(hazard, "verticalHitRange", 0.8f);
        SetTransform(hazard, "carriageVisual", carriage.transform);
    }

    private static void CreateHammer(Transform parent, string name, float x, float z, float phase)
    {
        if (swingHammerPrefab == null) swingHammerPrefab = LoadSwingHammerPrefab();
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(swingHammerPrefab, parent);
        root.name = name;
        root.transform.localPosition = new Vector3(x, 3.2f, z);
        Transform marker = root.transform.Find("Reach Marker");
        Transform arm = root.transform.Find("Hammer Arm");
        Transform head = root.transform.Find("Hammer Head");
        BoxCollider armHitVolume = arm != null ? arm.GetComponent<BoxCollider>() : null;
        SwingHammerHazard hazard = root.GetComponent<SwingHammerHazard>();
        if (marker == null || arm == null || head == null || armHitVolume == null || hazard == null)
            throw new InvalidOperationException("Shared SwingHammer prefab contract is incomplete.");

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        Renderer armRenderer = arm.GetComponent<Renderer>();
        Renderer headRenderer = head.GetComponent<Renderer>();
        if (markerRenderer != null) markerRenderer.sharedMaterial = markerMaterial;
        if (armRenderer != null) armRenderer.sharedMaterial = hammerMaterial;
        if (headRenderer != null) headRenderer.sharedMaterial = hammerMaterial;
        SetFloat(hazard, "oscillationDuration", 2.6f);
        SetFloat(hazard, "angleRange", 55f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "activationDistance", 22f);
        SetFloat(hazard, "armLength", 2f);
        SetFloat(hazard, "killRadius", 0.8f);
        SetFloat(hazard, "verticalHitRange", 1.25f);
        SetFloat(hazard, "armVerticalHitRange", 0.375f);
        SetFloat(hazard, "runnerHeight", 1.6f);
        SetTransform(hazard, "armVisual", arm);
        SetTransform(hazard, "hammerHead", head);
        SetSerialized(hazard, "armHitVolume", property => property.objectReferenceValue = armHitVolume);
    }

    private static GameObject LoadSwingHammerPrefab()
    {
        string guidPath = AssetDatabase.GUIDToAssetPath(SwingHammerPrefabGuid);
        if (!string.Equals(guidPath, SwingHammerPrefabPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SwingHammer GUID resolves to '" + guidPath + "', expected '" + SwingHammerPrefabPath + "'.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwingHammerPrefabPath);
        Transform arm = prefab != null ? prefab.transform.Find("Hammer Arm") : null;
        BoxCollider hitVolume = arm != null ? arm.GetComponent<BoxCollider>() : null;
        if (prefab == null || prefab.GetComponent<SwingHammerHazard>() == null || hitVolume == null || !hitVolume.isTrigger)
            throw new InvalidOperationException("Shared SwingHammer prefab is missing its hazard or trigger handle contract.");
        return prefab;
    }

    private static void CreateCannon(Transform parent, string name, float x, float z, float phase)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 1.4f, z);
        CreatePrimitive("Cannon Body", PrimitiveType.Cylinder, root.transform,
            new Vector3(0f, -1.4f, 0f), new Vector3(0.8f, 1.4f, 0.8f), cannonMaterial);
        CreatePrimitive("Cannon Barrel", PrimitiveType.Cube, root.transform,
            new Vector3(-Mathf.Sign(x) * 0.45f, -0.55f, 0f), new Vector3(0.9f, 0.25f, 0.25f), cannonMaterial);
        ProjectileLauncher launcher = root.AddComponent<ProjectileLauncher>();
        SetInt(launcher, "poolSize", 6);
        SetFloat(launcher, "fireInterval", 2.5f);
        SetFloat(launcher, "triggerRadius", 18f);
        SetColor(launcher, "projectileColor", new Color(1f, 0.2f, 0.9f));
        SetFloat(launcher, "initialFireDelay", phase);
    }

    private static void CreateMarker(Transform parent, string name, float x, float z)
    {
        CreatePrimitive(name, PrimitiveType.Cylinder, parent,
            new Vector3(x, 0.03f, z), new Vector3(2.4f, 0.02f, 2.4f), markerMaterial);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return go;
    }

    private static Material EnsureMaterial(string name, Color color, bool allowAssetWrite)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (!allowAssetWrite)
                throw new InvalidOperationException("Generated material is missing and save=false forbids creating '" + path + "'.");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported shader found for generated material '" + path + "'.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        if (allowAssetWrite)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    private static void SetFloat(UnityEngine.Object target, string property, float value)
    {
        SetSerialized(target, property, serialized => serialized.floatValue = value);
    }

    private static void SetInt(UnityEngine.Object target, string property, int value)
    {
        SetSerialized(target, property, serialized => serialized.intValue = value);
    }

    private static void SetEnum(UnityEngine.Object target, string property, int value)
    {
        SetSerialized(target, property, serialized => serialized.enumValueIndex = value);
    }

    private static void SetString(UnityEngine.Object target, string property, string value)
    {
        SetSerialized(target, property, serialized => serialized.stringValue = value);
    }

    private static void SetColor(UnityEngine.Object target, string property, Color value)
    {
        SetSerialized(target, property, serialized => serialized.colorValue = value);
    }

    private static void SetTransform(UnityEngine.Object target, string property, Transform value)
    {
        SetSerialized(target, property, serialized => serialized.objectReferenceValue = value);
    }

    private static void SetSerialized(UnityEngine.Object target, string property, Action<SerializedProperty> setter)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) throw new InvalidOperationException(target.GetType().Name + " missing serialized field " + property);
        setter(field);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetText(Scene scene, string path, string value)
    {
        GameObject go = FindInScene(scene, path);
        if (go == null) return;
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }

    private static GameObject FindInScene(Scene scene, string path)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(path)) return null;

        string[] parts = path.Split('/');
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (parts.Length == 1)
            {
                Transform named = FindDescendantByName(roots[i].transform, parts[0]);
                if (named != null) return named.gameObject;
                continue;
            }

            if (roots[i].name != parts[0]) continue;
            Transform current = roots[i].transform;
            for (int partIndex = 1; partIndex < parts.Length && current != null; partIndex++)
                current = current.Find(parts[partIndex]);
            if (current != null) return current.gameObject;
        }
        return null;
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindFirstChild(GameObject root, params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            Transform result = root.transform.Find(paths[i]);
            if (result != null) return result;
        }
        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }
}
