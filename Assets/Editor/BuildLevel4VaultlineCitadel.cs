using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLevel4VaultlineCitadel
{
    private static Material obstacleMaterial;
    private static Material telegraphMaterial;
    private static Material beamMaterial;
    private static Material cannonMaterial;
    private static Material markerMaterial;

    [MenuItem("SpiralSquad/Build Level 4 - Vaultline Citadel")]
    public static void Main()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Level4")
            throw new InvalidOperationException("Open Assets/Scenes/Level4.unity before building Level 4.");

        GameObject root = FindInScene(scene, "Level 4 - Vaultline Citadel");
        if (root == null) root = FindInScene(scene, "Level 3 - Pulsebound Foundry");
        if (root == null) throw new InvalidOperationException("Level 3 root not found in the Level4 copy.");
        root.name = "Level 4 - Vaultline Citadel";

        LoadMaterials();
        CreatePrefabFamilies();
        ConfigureCore(root);
        ConfigureGates(root);
        ConfigureHudAndCamera(scene);
        BuildTrapSections(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LEVEL4_SCENE_BUILT: Vaultline Citadel scene saved with authored vertical obstacle sections.");
    }

    private static void LoadMaterials()
    {
        obstacleMaterial = EnsureMaterial("Level4_VaultlineObstacle", new Color(0.08f, 0.30f, 0.40f));
        telegraphMaterial = EnsureMaterial("Level4_VaultlineTelegraph", new Color(0.10f, 0.95f, 0.82f));
        beamMaterial = EnsureMaterial("Level4_VaultlineBeam", new Color(1f, 0.38f, 0.08f));
        cannonMaterial = EnsureMaterial("Level4_VaultlineCannon", new Color(0.32f, 0.10f, 0.52f));
        markerMaterial = EnsureMaterial("Level4_VaultlineMarker", new Color(0.30f, 0.85f, 1f));
    }

    private static void ConfigureCore(GameObject root)
    {
        Transform track = root.transform.Find("Track/Track 10m x 340m");
        if (track == null) throw new InvalidOperationException("Track not found in Level4 copy.");
        track.name = "Track 10m x 360m";
        track.localPosition = new Vector3(0f, -0.1f, 180f);
        track.localScale = new Vector3(10f, 0.2f, 360f);

        SetLocalPosition(root.transform.Find("Track/Start Line"), new Vector3(0f, 0.01f, 3f));
        SetLocalPosition(root.transform.Find("Track/Finish Marker"), new Vector3(0f, 0.02f, 355f));
        SetLocalPosition(root.transform.Find("FinishGate"), new Vector3(0f, 0f, 358f));

        GameObject player = FindInScene(root.scene, "Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0f, 3f);
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                SetFloat(controller, "startForwardSpeed", 6f);
                SetFloat(controller, "maxForwardSpeed", 10f);
                SetFloat(controller, "speedRampStartZ", 3f);
                SetFloat(controller, "speedRampEndZ", 358f);
                SetFloat(controller, "jumpForce", 8f);
                SetFloat(controller, "gravity", -20f);
            }
        }
    }

    private static void ConfigureGates(GameObject root)
    {
        ConfigureGate(root.transform.Find("Gates/Gate A Z28"), "Gate A Z28", 28f, 35, 2);
        ConfigureGate(root.transform.Find("Gates/Gate B Z128"), "Gate B Z128", 128f, 60, 2);
        ConfigureGate(root.transform.Find("Gates/Gate C Z240"), "Gate C Z238", 238f, 90, 2);
    }

    private static void ConfigureGate(Transform gateRoot, string newName, float z, int addValue, int multiplyValue)
    {
        if (gateRoot == null) throw new InvalidOperationException(newName + " source gate not found.");
        gateRoot.name = newName;
        gateRoot.localPosition = new Vector3(0f, 0f, z);
        Gate[] gates = gateRoot.GetComponentsInChildren<Gate>(true);
        if (gates.Length != 2) throw new InvalidOperationException(newName + " expected two Gate components.");
        Array.Sort(gates, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        ConfigureGateComponent(gates[0], 0, addValue, "Left Gate +" + addValue);
        ConfigureGateComponent(gates[1], 1, multiplyValue, "Right Gate x" + multiplyValue);
    }

    private static void ConfigureGateComponent(Gate gate, int type, int value, string name)
    {
        gate.name = name;
        SetEnum(gate, "gateType", type);
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
                SetFloat(hud, "trackLength", 358f);
                SetString(hud, "levelLabel", "LEVEL 4 - VAULTLINE CITADEL");
            }
            SetText(scene, "Level HUD Canvas/HUD Level Title", "LEVEL 4 - VAULTLINE CITADEL");
            SetText(scene, "Level HUD Canvas/HUD Run Subtitle", "VAULTLINE CITADEL  •  JUMP TO CLEAR");
        }

        SetText(scene, "Player/World Level Label", "LEVEL 4");
        RenameObject(scene, "Level 3 Camera", "Level 4 Camera");
        RenameObject(scene, "CM Level3 Crowd Follow", "CM Level4 Crowd Follow");
    }

    private static void BuildTrapSections(GameObject root)
    {
        Transform sections = root.transform.Find("Trap Sections");
        if (sections == null) throw new InvalidOperationException("Trap Sections parent not found.");
        ClearChildren(sections);

        Transform vault = CreateSection(sections, "Vault Tutorial");
        CreateBarrier(vault, "Vault Barrier 01", -2.4f, 46f, 1.8f, 0.85f, false);
        CreateBarrier(vault, "Vault Barrier 02", 2.4f, 54f, 1.8f, 0.95f, false);
        CreateBarrier(vault, "Vault Barrier 03", 0f, 62f, 3.4f, 1.05f, false);
        CreateBarrier(vault, "Vault Barrier 04 Required Jump", 0f, 70f, 9f, 1.10f, true);

        Transform rising = CreateSection(sections, "Rising Block Yard");
        CreateRising(rising, "Rising Block 01", -2.4f, 84f, 1.8f, 3.2f, 0f);
        CreateRising(rising, "Rising Block 02", 2.4f, 94f, 1.8f, 3.2f, 0.8f);
        CreateRising(rising, "Rising Block 03", 0f, 106f, 3.6f, 3.6f, 1.6f);
        CreateRising(rising, "Rising Block 04 L", -1.2f, 116f, 1.4f, 3.8f, 2f);
        CreateRising(rising, "Rising Block 04 R", 1.2f, 116f, 1.4f, 3.8f, 2f);

        Transform sweep = CreateSection(sections, "Sweep Beam Alley");
        CreateSweep(sweep, "Sweep Beam 01", 150f, -3.6f, 3.6f, 0f);
        CreateSweep(sweep, "Sweep Beam 02", 168f, 3.6f, -3.6f, 1.4f);

        Transform shutter = CreateSection(sections, "Shutter Yard");
        CreateShutter(shutter, "Shutter 01", -2.4f, 194f, 0f);
        CreateShutter(shutter, "Shutter 02", 2.4f, 204f, 0.75f);
        CreateShutter(shutter, "Shutter 03", 0f, 214f, 1.5f);
        CreateShutter(shutter, "Shutter 04 L", -2.4f, 224f, 2.25f);
        CreateShutter(shutter, "Shutter 04 R", 2.4f, 224f, 2.25f);

        Transform cross = CreateSection(sections, "Cross-Pressure Course");
        CreateBarrier(cross, "Cross Barrier", -2.4f, 254f, 2f, 1f, false);
        CreateRising(cross, "Cross Rising Block", 2.4f, 262f, 1.8f, 3.2f, 0.6f);
        CreateSweep(cross, "Cross Sweep Beam", 278f, -3.6f, 3.6f, 0f);
        CreateCannon(cross, "Cross Side Cannon", -4.35f, 282f, 1.5f);
        CreateShutter(cross, "Cross Shutter L", -1.2f, 290f, 1.2f);
        CreateShutter(cross, "Cross Shutter R", 1.2f, 290f, 1.2f);
        CreateMarker(cross, "Cross Recovery Marker", 0f, 296f);

        Transform citadelLock = CreateSection(sections, "Citadel Lock");
        CreateBarrier(citadelLock, "Lock Barrier L", -2.4f, 304f, 1.8f, 0.95f, false);
        CreateBarrier(citadelLock, "Lock Barrier R", 2.4f, 304f, 1.8f, 1.05f, false);
        CreateSweep(citadelLock, "Lock Sweep Beam", 318f, 3.6f, -3.6f, 1.4f);
        CreateRising(citadelLock, "Lock Rising Block", 0f, 320f, 3.6f, 3.8f, 1f);
        CreateCannon(citadelLock, "Lock Cannon L", -4.35f, 328f, 0f);
        CreateCannon(citadelLock, "Lock Cannon R", 4.35f, 328f, 1.25f);
        CreateShutter(citadelLock, "Lock Shutter L", -2.4f, 336f, 2.25f);
        CreateShutter(citadelLock, "Lock Shutter R", 2.4f, 336f, 2.25f);
        CreateMarker(citadelLock, "Lock Recovery Runway", 0f, 344f);
    }

    private static Transform CreateSection(Transform parent, string name)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
        return section.transform;
    }

    private static void CreateBarrier(Transform parent, string name, float x, float z, float width, float height, bool wide)
    {
        string path = wide ? "Assets/Prefabs/Traps/Level4/VaultBarrier_Wide.prefab" : "Assets/Prefabs/Traps/Level4/VaultBarrier.prefab";
        GameObject instance = InstantiatePrefab(path, parent, name, new Vector3(x, 0f, z));
        SetFloat(instance.GetComponent<JumpBarrierHazard>(), "width", width);
        SetFloat(instance.GetComponent<JumpBarrierHazard>(), "height", height);
        SetFloat(instance.GetComponent<JumpBarrierHazard>(), "warningLead", 8f);
        SetInt(instance.GetComponent<JumpBarrierHazard>(), "lossCap", 8);
    }

    private static void CreateRising(Transform parent, string name, float x, float z, float width, float cycle, float phase)
    {
        GameObject instance = InstantiatePrefab("Assets/Prefabs/Traps/Level4/RisingBlock_Single.prefab", parent, name, new Vector3(x, 0f, z));
        SetFloat(instance.GetComponent<RisingBlockHazard>(), "width", width);
        SetFloat(instance.GetComponent<RisingBlockHazard>(), "cycleDuration", cycle);
        SetFloat(instance.GetComponent<RisingBlockHazard>(), "phaseOffset", phase);
        SetFloat(instance.GetComponent<RisingBlockHazard>(), "warningLead", 1f);
        SetInt(instance.GetComponent<RisingBlockHazard>(), "lossCap", 6);
    }

    private static void CreateSweep(Transform parent, string name, float z, float startX, float endX, float phase)
    {
        string path = startX < endX ? "Assets/Prefabs/Traps/Level4/SweepBeam_LeftStart.prefab" : "Assets/Prefabs/Traps/Level4/SweepBeam_RightStart.prefab";
        GameObject instance = InstantiatePrefab(path, parent, name, new Vector3(0f, 0.7f, z));
        SetFloat(instance.GetComponent<SweepBeamHazard>(), "startX", startX);
        SetFloat(instance.GetComponent<SweepBeamHazard>(), "endX", endX);
        SetFloat(instance.GetComponent<SweepBeamHazard>(), "phaseOffset", phase);
        SetFloat(instance.GetComponent<SweepBeamHazard>(), "warningLead", 1f);
        SetInt(instance.GetComponent<SweepBeamHazard>(), "lossCap", 5);
    }

    private static void CreateShutter(Transform parent, string name, float x, float z, float phase)
    {
        GameObject instance = InstantiatePrefab("Assets/Prefabs/Traps/Level4/ShutterBlock_Single.prefab", parent, name, new Vector3(x, 0f, z));
        SetFloat(instance.GetComponent<ShutterBlockHazard>(), "phaseOffset", phase);
        SetFloat(instance.GetComponent<ShutterBlockHazard>(), "warningLead", 0.8f);
        SetInt(instance.GetComponent<ShutterBlockHazard>(), "lossCap", 6);
    }

    private static void CreateCannon(Transform parent, string name, float x, float z, float phase)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 1.4f, z);
        CreatePrimitive("Cannon Body", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -1.4f, 0f), new Vector3(0.8f, 1.4f, 0.8f), cannonMaterial);
        CreatePrimitive("Cannon Barrel", PrimitiveType.Cube, root.transform, new Vector3(-Mathf.Sign(x) * 0.45f, -0.55f, 0f), new Vector3(0.9f, 0.25f, 0.25f), cannonMaterial);
        ProjectileLauncher launcher = root.AddComponent<ProjectileLauncher>();
        SetInt(launcher, "poolSize", 2);
        SetFloat(launcher, "fireInterval", 3.5f);
        SetFloat(launcher, "triggerRadius", 14f);
        SetColor(launcher, "projectileColor", new Color(1f, 0.2f, 0.9f));
        SetFloat(launcher, "initialFireDelay", phase);
    }

    private static void CreateMarker(Transform parent, string name, float x, float z)
    {
        CreatePrimitive(name, PrimitiveType.Cylinder, parent, new Vector3(x, 0.03f, z), new Vector3(2.4f, 0.02f, 2.4f), markerMaterial);
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, string name, Vector3 localPosition)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Level 4 prefab not found: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        return instance;
    }

    private static void CreatePrefabFamilies()
    {
        EnsureFolder("Assets/Prefabs/Traps");
        EnsureFolder("Assets/Prefabs/Traps/Level4");
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/JumpObstacleBase.prefab", typeof(JumpBarrierHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/VaultBarrier.prefab", typeof(JumpBarrierHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/VaultBarrier_Wide.prefab", typeof(JumpBarrierHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/RisingBlockBase.prefab", typeof(RisingBlockHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/RisingBlock_Single.prefab", typeof(RisingBlockHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/RisingBlock_Double.prefab", typeof(RisingBlockHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/SweepBeamBase.prefab", typeof(SweepBeamHazard), beamMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/SweepBeam_LeftStart.prefab", typeof(SweepBeamHazard), beamMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/SweepBeam_RightStart.prefab", typeof(SweepBeamHazard), beamMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/ShutterBlockBase.prefab", typeof(ShutterBlockHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/ShutterBlock_Single.prefab", typeof(ShutterBlockHazard), obstacleMaterial);
        CreateHazardPrefab("Assets/Prefabs/Traps/Level4/ShutterBlock_Pair.prefab", typeof(ShutterBlockHazard), obstacleMaterial);
        CreateTelegraphPrefab("Assets/Prefabs/Traps/Level4/ObstacleTelegraph.prefab");
    }

    private static void CreateHazardPrefab(string path, Type hazardType, Material material)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents.transform.Find("HitVolume") == null)
            {
                GameObject hitVolume = new GameObject("HitVolume");
                hitVolume.transform.SetParent(contents.transform, false);
            }
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
        CreatePrimitive("Visual", PrimitiveType.Cube, root.transform, Vector3.zero, Vector3.one, material);
        CreatePrimitive("Telegraph", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.4f, 0.02f, 2.4f), telegraphMaterial);
        GameObject newHitVolume = new GameObject("HitVolume");
        newHitVolume.transform.SetParent(root.transform, false);
        root.AddComponent(hazardType);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateTelegraphPrefab(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = new GameObject("ObstacleTelegraph");
        CreatePrimitive("Ground Marker", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(2.4f, 0.02f, 2.4f), telegraphMaterial);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetFloat(UnityEngine.Object target, string property, float value) => SetSerialized(target, property, p => p.floatValue = value);
    private static void SetInt(UnityEngine.Object target, string property, int value) => SetSerialized(target, property, p => p.intValue = value);
    private static void SetEnum(UnityEngine.Object target, string property, int value) => SetSerialized(target, property, p => p.enumValueIndex = value);
    private static void SetString(UnityEngine.Object target, string property, string value) => SetSerialized(target, property, p => p.stringValue = value);
    private static void SetColor(UnityEngine.Object target, string property, Color value) => SetSerialized(target, property, p => p.colorValue = value);

    private static void SetSerialized(UnityEngine.Object target, string property, Action<SerializedProperty> setter)
    {
        if (target == null) return;
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty field = serialized.FindProperty(property);
        if (field == null) throw new InvalidOperationException(target.GetType().Name + " missing serialized field " + property);
        setter(field);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetLocalPosition(Transform transform, Vector3 position)
    {
        if (transform != null) transform.localPosition = position;
    }

    private static void RenameObject(Scene scene, string oldName, string newName)
    {
        GameObject go = FindInScene(scene, oldName);
        if (go != null) go.name = newName;
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
        string[] parts = path.Split('/');
        GameObject current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == parts[0])
            {
                current = root;
                break;
            }
        }

        if (current == null) return null;
        for (int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Find(parts[i]);
            if (child == null) return null;
            current = child.gameObject;
        }
        return current;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }
}
