using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLevel3PulseboundFoundry
{
    private static Material pulseBody;
    private static Material pulseCharge;
    private static Material pulseDischarge;
    private static Material spikeMaterial;
    private static Material railMaterial;
    private static Material hammerMaterial;
    private static Material cannonMaterial;
    private static Material markerMaterial;

    public static void Main()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Level3")
            throw new InvalidOperationException("Open Assets/Scenes/Level3.unity before building Level 3.");

        GameObject root = GameObject.Find("Level 2 - Momentum Trapworks");
        if (root == null) throw new InvalidOperationException("Level 2 root not found in the Level3 copy.");
        root.name = "Level 3 - Pulsebound Foundry";

        LoadMaterials();
        ConfigureCore(root);
        ConfigureGates(root);
        ConfigureHudAndCamera();
        BuildTrapSections(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL3_SCENE_BUILT: Pulsebound Foundry scene saved with authored trap sections.");
    }

    private static void LoadMaterials()
    {
        pulseBody = EnsureMaterial("Level3_PulseBody", new Color(0.06f, 0.22f, 0.30f));
        pulseCharge = EnsureMaterial("Level3_PulseCharge", new Color(0.10f, 0.75f, 1f));
        pulseDischarge = EnsureMaterial("Level3_PulseDischarge", new Color(1f, 0.16f, 0.12f));
        spikeMaterial = EnsureMaterial("Level3_Spikes", new Color(0.95f, 0.12f, 0.14f));
        railMaterial = EnsureMaterial("Level3_Rail", new Color(0.08f, 0.08f, 0.12f));
        hammerMaterial = EnsureMaterial("Level3_Hammer", new Color(1f, 0.58f, 0.08f));
        cannonMaterial = EnsureMaterial("Level3_Cannon", new Color(0.36f, 0.12f, 0.55f));
        markerMaterial = EnsureMaterial("Level3_Marker", new Color(0.18f, 0.95f, 0.65f));
    }

    private static void ConfigureCore(GameObject root)
    {
        Transform track = root.transform.Find("Track/Track 10m x 300m");
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

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0f, 3f);
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                SetFloat(controller, "startForwardSpeed", 6f);
                SetFloat(controller, "maxForwardSpeed", 10f);
                SetFloat(controller, "speedRampStartZ", 3f);
                SetFloat(controller, "speedRampEndZ", 338f);
            }
        }
    }

    private static void ConfigureGates(GameObject root)
    {
        ConfigureGate(root.transform.Find("Gates/Gate A Z24"), "Gate A Z28", 28f, 30, 2);
        ConfigureGate(root.transform.Find("Gates/Gate B Z104"), "Gate B Z128", 128f, 50, 2);
        ConfigureGate(root.transform.Find("Gates/Gate C Z240"), "Gate C Z240", 240f, 75, 2);
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

    private static void ConfigureHudAndCamera()
    {
        GameObject canvasObject = GameObject.Find("Level HUD Canvas");
        if (canvasObject != null)
        {
            LevelHud hud = canvasObject.GetComponent<LevelHud>();
            if (hud != null)
            {
                SetFloat(hud, "trackLength", 338f);
                SetString(hud, "levelLabel", "LEVEL 3 - PULSEBOUND FOUNDRY");
            }
            SetText("Level HUD Canvas/HUD Level Title", "LEVEL 3 - PULSEBOUND FOUNDRY");
            SetText("Level HUD Canvas/HUD Run Subtitle", "PULSEBOUND FOUNDRY");
        }

        SetText("Player/World Level Label", "LEVEL 3");
        GameObject camera = GameObject.Find("Level 2 Camera");
        if (camera != null) camera.name = "Level 3 Camera";
        GameObject follow = GameObject.Find("CM Level2 Crowd Follow");
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
        CreateSpike(shuttleTutorial, "Spike Shuttle 02", 106f, 3.8f, -3.8f, 3.75f);

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
        GameObject pulsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level3/PulsePlate.prefab");
        if (pulsePrefab != null)
        {
            GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(pulsePrefab, parent);
            prefabInstance.name = name;
            prefabInstance.transform.localPosition = new Vector3(x, 0.12f, z);
            SetFloat(prefabInstance.GetComponent<PulsePlateHazard>(), "phaseOffset", phase);
            return;
        }

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 0.12f, z);
        CreatePrimitive("Plate", PrimitiveType.Cylinder, root.transform, Vector3.zero,
            new Vector3(1.9f, 0.06f, 1.9f), pulseBody);
        GameObject charge = CreatePrimitive("Charge Ring", PrimitiveType.Cylinder, root.transform,
            new Vector3(0f, 0.08f, 0f), new Vector3(2.2f, 0.025f, 2.2f), pulseCharge);
        GameObject discharge = CreatePrimitive("Discharge Ring", PrimitiveType.Cylinder, root.transform,
            new Vector3(0f, 0.11f, 0f), new Vector3(2.4f, 0.03f, 2.4f), pulseDischarge);
        PulsePlateHazard hazard = root.AddComponent<PulsePlateHazard>();
        SetFloat(hazard, "cycleDuration", 3.5f);
        SetFloat(hazard, "chargeDuration", 1.5f);
        SetFloat(hazard, "dischargeDuration", 0.5f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "killRadius", 1.05f);
        SetInt(hazard, "maxRunnersPerDischarge", 2);
        SetTransform(hazard, "chargeRing", charge.transform);
        SetTransform(hazard, "dischargeRing", discharge.transform);
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
        SetFloat(hazard, "travelDuration", 3f);
        SetFloat(hazard, "endpointPause", 0.75f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "approachDistance", 24f);
        SetFloat(hazard, "killRadius", 0.65f);
        SetFloat(hazard, "verticalHitRange", 0.8f);
        SetInt(hazard, "maxRunnersPerLeg", 4);
        SetTransform(hazard, "carriageVisual", carriage.transform);
    }

    private static void CreateHammer(Transform parent, string name, float x, float z, float phase)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 3.2f, z);
        CreatePrimitive("Reach Marker", PrimitiveType.Cylinder, root.transform,
            new Vector3(0f, -3.08f, 0f), new Vector3(4.2f, 0.025f, 1.1f), markerMaterial);
        GameObject arm = CreatePrimitive("Hammer Arm", PrimitiveType.Cube, root.transform,
            new Vector3(0f, -1.1f, 0f), new Vector3(4f, 0.25f, 0.25f), hammerMaterial);
        GameObject head = CreatePrimitive("Hammer Head", PrimitiveType.Sphere, root.transform,
            new Vector3(0f, -2.2f, 0f), new Vector3(0.9f, 0.9f, 0.9f), hammerMaterial);
        SwingHammerHazard hazard = root.AddComponent<SwingHammerHazard>();
        SetFloat(hazard, "oscillationDuration", 2.6f);
        SetFloat(hazard, "angleRange", 55f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "activationDistance", 22f);
        SetFloat(hazard, "armLength", 2f);
        SetFloat(hazard, "killRadius", 0.8f);
        SetFloat(hazard, "verticalHitRange", 1.25f);
        SetInt(hazard, "maxHeadRunnersPerCycle", 8);
        SetInt(hazard, "maxArmRunnersPerCycle", 3);
        SetFloat(hazard, "armVerticalHitRange", 2.2f);
        SetTransform(hazard, "armVisual", arm.transform);
        SetTransform(hazard, "hammerHead", head.transform);
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

    private static Material EnsureMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
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

    private static void SetText(string path, string value)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }
}
