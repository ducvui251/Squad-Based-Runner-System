using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildLevel5FractureRelay
{
    private const string SourceScenePath = "Assets/Scenes/Level4.unity";
    private const string Level5ScenePath = "Assets/Scenes/Level5.unity";

    private static Material obstacleMaterial;
    private static Material telegraphMaterial;
    private static Material beamMaterial;
    private static Material pulseBodyMaterial;
    private static Material pulseChargeMaterial;
    private static Material pulseDischargeMaterial;
    private static Material spikeMaterial;
    private static Material railMaterial;
    private static Material hammerMaterial;
    private static Material cannonMaterial;
    private static Material markerMaterial;
    private static Material fractureSurfaceMaterial;
    private static Material fractureEdgeMaterial;

    [MenuItem("SpiralSquad/Build Level 5 - Fracture Relay")]
    public static void Main()
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            throw new InvalidOperationException("Open Level4.unity before building Level 5.");
        }

        if (activeScene.name == "Level4")
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level5ScenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScenePath, Level5ScenePath))
                {
                    throw new InvalidOperationException("Could not duplicate Assets/Scenes/Level4.unity to Assets/Scenes/Level5.unity.");
                }

                AssetDatabase.Refresh();
            }

            activeScene = EditorSceneManager.OpenScene(Level5ScenePath, OpenSceneMode.Single);
        }
        else if (activeScene.name != "Level5")
        {
            throw new InvalidOperationException("Open Assets/Scenes/Level4.unity or Assets/Scenes/Level5.unity before building Level 5.");
        }

        BuildScene(activeScene);
    }

    private static void BuildScene(Scene scene)
    {
        GameObject root = FindInScene(scene, "Level 5 - Fracture Relay");
        if (root == null) root = FindInScene(scene, "Level 4 - Vaultline Citadel");
        if (root == null) throw new InvalidOperationException("Level 4 root not found in the Level 5 copy.");

        root.name = "Level 5 - Fracture Relay";

        LoadMaterials();
        int playerOnlyLayer = EnsurePlayerOnlyLayer();
        CreateCrackPrefabFamilies(playerOnlyLayer);
        ConfigureCore(scene, root);
        ConfigureGates(root);
        ConfigureHudAndCamera(scene);
        ConfigurePickups(root);
        BuildTrapSections(root, playerOnlyLayer);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LEVEL5_SCENE_BUILT: Fracture Relay scene saved with segmented road gaps and two crowd-splitting fractures.");
    }

    private static void LoadMaterials()
    {
        obstacleMaterial = LoadOrCreateMaterial("Assets/Materials/Level4_VaultlineObstacle.mat", "Level5_FractureObstacle", new Color(0.08f, 0.30f, 0.40f));
        telegraphMaterial = LoadOrCreateMaterial("Assets/Materials/Level4_VaultlineTelegraph.mat", "Level5_FractureTelegraph", new Color(0.10f, 0.95f, 0.82f));
        beamMaterial = LoadOrCreateMaterial("Assets/Materials/Level4_VaultlineBeam.mat", "Level5_FractureBeam", new Color(1f, 0.38f, 0.08f));
        pulseBodyMaterial = LoadOrCreateMaterial("Assets/Materials/Level3_PulseBody.mat", "Level5_PulseBody", new Color(0.06f, 0.22f, 0.30f));
        pulseChargeMaterial = LoadOrCreateMaterial("Assets/Materials/Level3_PulseCharge.mat", "Level5_PulseCharge", new Color(0.10f, 0.75f, 1f));
        pulseDischargeMaterial = LoadOrCreateMaterial("Assets/Materials/Level3_PulseDischarge.mat", "Level5_PulseDischarge", new Color(1f, 0.16f, 0.12f));
        spikeMaterial = LoadOrCreateMaterial("Assets/Materials/Level3_Spikes.mat", "Level5_Spikes", new Color(0.95f, 0.12f, 0.14f));
        railMaterial = LoadOrCreateMaterial("Assets/Materials/Level3_Rail.mat", "Level5_Rail", new Color(0.08f, 0.08f, 0.12f));
        hammerMaterial = LoadOrCreateMaterial("Assets/Materials/Level3_Hammer.mat", "Level5_Hammer", new Color(1f, 0.58f, 0.08f));
        cannonMaterial = LoadOrCreateMaterial("Assets/Materials/Level4_VaultlineCannon.mat", "Level5_Cannon", new Color(0.32f, 0.10f, 0.52f));
        markerMaterial = LoadOrCreateMaterial("Assets/Materials/Level4_VaultlineMarker.mat", "Level5_Marker", new Color(0.30f, 0.85f, 1f));
        fractureSurfaceMaterial = EnsureMaterial("Level5_FractureSurface", new Color(0.04f, 0.03f, 0.08f));
        fractureEdgeMaterial = EnsureMaterial("Level5_FractureEdge", new Color(0.86f, 0.16f, 0.98f));
    }

    private static void ConfigureCore(Scene scene, GameObject root)
    {
        Transform track = root.transform.Find("Track/Track 10m x 420m");
        if (track == null) track = root.transform.Find("Track/Track 10m x 360m");
        if (track == null) throw new InvalidOperationException("Track not found in the Level 5 copy.");

        track.name = "Track 10m x 420m";
        track.localPosition = new Vector3(0f, -0.1f, 210f);
        track.localScale = new Vector3(10f, 0.2f, 420f);

        MeshRenderer legacyTrackRenderer = track.GetComponent<MeshRenderer>();
        Material trackMaterial = legacyTrackRenderer != null ? legacyTrackRenderer.sharedMaterial : null;
        if (legacyTrackRenderer != null) legacyTrackRenderer.enabled = false;
        foreach (Collider collider in track.GetComponents<Collider>())
        {
            collider.enabled = false;
        }

        Transform trackParent = track.parent;
        EnsureRoadSegment(trackParent, "Road Segment 00-304", 152f, 304f, trackMaterial);
        EnsureRoadSegment(trackParent, "Road Segment 308-386", 347f, 78f, trackMaterial);
        EnsureRoadSegment(trackParent, "Road Segment 390-420", 405f, 30f, trackMaterial);

        SetLocalPosition(root.transform.Find("Track/Start Line"), new Vector3(0f, 0.01f, 3f));
        SetLocalPosition(root.transform.Find("Track/Finish Marker"), new Vector3(0f, 0.02f, 416f));
        SetLocalPosition(root.transform.Find("FinishGate"), new Vector3(0f, 0f, 418f));

        GameObject player = FindInScene(scene, "Player");
        if (player == null) throw new InvalidOperationException("Player not found in the Level 5 copy.");

        player.transform.position = new Vector3(0f, 0f, 3f);
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            SetFloat(controller, "forwardSpeed", 6f);
            SetFloat(controller, "startForwardSpeed", 6f);
            SetFloat(controller, "maxForwardSpeed", 10f);
            SetFloat(controller, "speedRampStartZ", 3f);
            SetFloat(controller, "speedRampEndZ", 418f);
            SetFloat(controller, "jumpForce", 8f);
            SetFloat(controller, "gravity", -20f);
            SetFloat(controller, "jumpReleaseVelocityMultiplier", 0.35f);
        }
    }

    private static void EnsureRoadSegment(Transform parent, string name, float centerZ, float length, Material material)
    {
        if (parent == null) throw new InvalidOperationException("Track parent not found while creating Level 5 road segments.");

        Transform existing = parent.Find(name);
        GameObject segment;
        if (existing == null)
        {
            segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent, false);
        }
        else
        {
            segment = existing.gameObject;
        }

        segment.SetActive(true);
        segment.transform.localPosition = new Vector3(0f, -0.1f, centerZ);
        segment.transform.localRotation = Quaternion.identity;
        segment.transform.localScale = new Vector3(10f, 0.2f, length);

        MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        BoxCollider collider = segment.GetComponent<BoxCollider>();
        if (collider == null) collider = segment.AddComponent<BoxCollider>();
        collider.enabled = true;
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = Vector3.one;
    }

    private static void ConfigureGates(GameObject root)
    {
        ConfigureGate(
            FindChildAny(root.transform, "Gates/Gate A Z28", "Gates/Gate A Z30"),
            "Gate A Z30", 30f, 45, 2);
        ConfigureGate(
            FindChildAny(root.transform, "Gates/Gate B Z128", "Gates/Gate B Z150"),
            "Gate B Z150", 150f, 75, 2);
        ConfigureGate(
            FindChildAny(root.transform, "Gates/Gate C Z238", "Gates/Gate C Z320"),
            "Gate C Z320", 320f, 110, 2);
    }

    private static void ConfigureGate(Transform gateRoot, string newName, float z, int addValue, int multiplyValue)
    {
        if (gateRoot == null) throw new InvalidOperationException(newName + " source gate not found.");

        gateRoot.name = newName;
        gateRoot.localPosition = new Vector3(0f, 0f, z);
        Gate[] gates = gateRoot.GetComponentsInChildren<Gate>(true);
        if (gates.Length != 2) throw new InvalidOperationException(newName + " expected two Gate components.");

        Array.Sort(gates, (left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
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
                SetFloat(hud, "trackLength", 418f);
                SetString(hud, "levelLabel", "LEVEL 5 - FRACTURE RELAY");
            }

            SetText(scene, "Level HUD Canvas/HUD Level Title", "LEVEL 5 - FRACTURE RELAY");
            SetText(scene, "Level HUD Canvas/HUD Run Label", "FRACTURE RELAY");
            SetText(scene, "Level HUD Canvas/HUD Run Subtitle", "FRACTURE RELAY  •  JUMP THE BREAK");
            SetText(scene, "Level HUD Canvas/Stage 1/Stage 1 Text", "CROSSWIND / DROP-SAW");
            SetText(scene, "Level HUD Canvas/Stage 2/Stage 2 Text", "FRACTURE RELAY");
            SetText(scene, "Level HUD Canvas/Settings Panel/Controls Hint", "DRAG / A-D / SPACE TO JUMP");
        }

        SetText(scene, "Player/World Level Label", "LEVEL 5");
        RenameObject(scene, "Level 4 Camera", "Level 5 Camera");
        RenameObject(scene, "CM Level4 Crowd Follow", "CM Level5 Crowd Follow");
    }

    private static void ConfigurePickups(GameObject root)
    {
        Transform pickups = root.transform.Find("Pickups");
        if (pickups == null || pickups.childCount == 0) return;

        Vector3[] positions =
        {
            new Vector3(0f, 0f, 36f),
            new Vector3(-2.5f, 0f, 136f),
            new Vector3(2.5f, 0f, 136f),
            new Vector3(0f, 0f, 208f),
            new Vector3(-2.5f, 0f, 270f),
            new Vector3(2.5f, 0f, 270f),
            new Vector3(0f, 0f, 312f),
            new Vector3(-2.5f, 0f, 328f),
            new Vector3(2.5f, 0f, 328f),
            new Vector3(0f, 0f, 404f)
        };

        while (pickups.childCount < positions.Length)
        {
            GameObject source = pickups.GetChild(0).gameObject;
            GameObject duplicate = UnityEngine.Object.Instantiate(source, pickups);
            duplicate.name = "Level5 Pickup " + pickups.childCount.ToString("00");
        }

        for (int i = 0; i < positions.Length; i++)
        {
            Transform pickup = pickups.GetChild(i);
            pickup.name = "Level5 Pickup " + (i + 1).ToString("00");
            pickup.localPosition = new Vector3(positions[i].x, pickup.localPosition.y, positions[i].z);
        }
    }

    private static void BuildTrapSections(GameObject root, int playerOnlyLayer)
    {
        Transform sections = root.transform.Find("Trap Sections");
        if (sections == null) throw new InvalidOperationException("Trap Sections parent not found.");
        ClearChildren(sections);

        Transform crosswind = CreateSection(sections, "Crosswind Pulse Weave");
        CreatePulse(crosswind, "Pulse Plate 01", 0f, 44f, 0f);
        CreateConeRow(crosswind, "Cone Row 01", -3.2f, 3.2f, 50f);
        CreatePulse(crosswind, "Pulse Plate 02", -2.4f, 58f, 0.55f);
        CreateConeRow(crosswind, "Cone Row 02", -1.2f, 3.2f, 64f);
        CreatePulse(crosswind, "Pulse Plate 03", 2.4f, 72f, 1.10f);
        CreateConeRow(crosswind, "Cone Row 03", -3.2f, 1.2f, 78f);

        Transform dropSaw = CreateSection(sections, "Drop-Saw Relay");
        CreateFalling(dropSaw, "Hidden Falling Trap 01", -2.4f, 94f, 7f);
        CreateConeRow(dropSaw, "Cone Row 04", -3.2f, 3.2f, 100f);
        CreateSaw(dropSaw, "Circular Saw 01", 110f, false, 3.4f);
        CreateFalling(dropSaw, "Hidden Falling Trap 02", 2.4f, 122f, 7f);
        CreateConeRow(dropSaw, "Cone Row 05", -1.2f, 1.2f, 128f);

        Transform blockSaw = CreateSection(sections, "Block-Saw Interlock");
        CreateRising(blockSaw, "Rising Block A", -2.4f, 164f, 1.8f, 3.2f, 0f);
        CreateSaw(blockSaw, "Circular Saw 02", 176f, true, 3.4f);
        CreatePulse(blockSaw, "Pulse Row L", -1.2f, 188f, 0f);
        CreatePulse(blockSaw, "Pulse Row R", 1.2f, 188f, 0.8f);
        CreateRising(blockSaw, "Rising Block B", 2.4f, 198f, 1.8f, 3.2f, 1.6f);

        Transform pendulum = CreateSection(sections, "Pendulum Shutter Exchange");
        CreateHammer(pendulum, "Swing Hammer 01", -2.4f, 220f, 0f);
        CreateShutter(pendulum, "Shutter Panel 01", 2.4f, 230f, 0.75f);
        CreateSpike(pendulum, "Spike Shuttle 01", 244f, -3.8f, 3.8f, 0f, -1f);
        CreateCannon(pendulum, "Side Cannon 01", 4.35f, 254f, 1f);
        CreateShutter(pendulum, "Shutter Pair L", -1.2f, 264f, 1.8f);
        CreateShutter(pendulum, "Shutter Pair R", 1.2f, 264f, 1.8f);

        Transform tutorial = CreateSection(sections, "Fracture Tutorial");
        CreateBarrier(tutorial, "Vault Barrier 01", -2.4f, 276f, 1.8f, 0.95f);
        CreateSweep(tutorial, "Sweep Beam 01", 288f, 3.6f, -3.6f, 0f);
        CreateCrackedSpan(tutorial, "Cracked Span 01 Tutorial", 304f, "Assets/Prefabs/Traps/Level5/CrackedSpan_Short.prefab", playerOnlyLayer);

        Transform finalRelay = CreateSection(sections, "Final Fracture Relay");
        CreateConeRow(finalRelay, "Final Cone Row", -3.2f, 3.2f, 336f);
        CreatePulse(finalRelay, "Final Pulse L", -2.4f, 344f, 0f);
        CreatePulse(finalRelay, "Final Pulse R", 2.4f, 344f, 0.8f);
        CreateFalling(finalRelay, "Final Hidden Falling Trap", 2.4f, 352f, 7f);
        CreateCannon(finalRelay, "Final Side Cannon", -4.35f, 360f, 0.6f);
        CreateRising(finalRelay, "Final Rising Block", 0f, 368f, 3.4f, 3.2f, 1f);
        CreateSaw(finalRelay, "Circular Saw 03", 378f, false, 3.6f);
        CreateCrackedSpan(finalRelay, "Cracked Span 02 Final", 386f, "Assets/Prefabs/Traps/Level5/CrackedSpan_Final.prefab", playerOnlyLayer);
        CreateSpike(finalRelay, "Spike Shuttle 02 Final", 400f, 3.8f, -3.8f, 0f, 402f);
    }

    private static void CreateConeRow(Transform parent, string name, float leftX, float rightX, float z)
    {
        CreateCone(parent, name + " L", leftX, z);
        CreateCone(parent, name + " R", rightX, z);
    }

    private static void CreateCone(Transform parent, string name, float x, float z)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 0f, z);
        CreatePrimitive("Cone", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.85f, 0.55f, 0.85f), obstacleMaterial);
        CreatePrimitive("Cone Ring", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.9f, 0.02f, 1.9f), telegraphMaterial);
        ConeHazard hazard = root.AddComponent<ConeHazard>();
        SetFloat(hazard, "killRadius", 0.85f);
        SetFloat(hazard, "boundaryX", 5f);
        SetFloat(hazard, "boundaryEliminationMargin", 0.25f);
    }

    private static void CreateFalling(Transform parent, string name, float x, float z, float activationOffset)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 0f, z);
        CreatePrimitive("Drop Block", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(1.8f, 1.4f, 1.8f), obstacleMaterial);
        CreatePrimitive("Drop Marker", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2.4f, 0.02f, 2.4f), telegraphMaterial);
        FallingHazard hazard = root.AddComponent<FallingHazard>();
        SetFloat(hazard, "gravity", -20f);
        SetFloat(hazard, "killRadius", 1.2f);
        SetFloat(hazard, "floatingHeight", 5f);
        SetFloat(hazard, "resetDelay", 1.5f);
        SetFloat(hazard, "activationOffset", activationOffset);
    }

    private static void CreateSaw(Transform parent, string name, float z, bool startAtRight, float moveSpeed)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0.55f, z);
        CreatePrimitive("Saw Disc", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(1.8f, 0.12f, 1.8f), spikeMaterial);
        CreatePrimitive("Saw Hub", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.14f, 0f), new Vector3(0.45f, 0.22f, 0.45f), markerMaterial);
        CircularSaw saw = root.AddComponent<CircularSaw>();
        SetFloat(saw, "moveSpeed", moveSpeed);
        SetFloat(saw, "spinSpeed", 420f);
        SetFloat(saw, "killRadius", 0.8f);
        SetBool(saw, "useExplicitXLimits", true);
        SetFloat(saw, "leftLimitOverride", -3.6f);
        SetFloat(saw, "rightLimitOverride", 3.6f);
        SetBool(saw, "startAtRight", startAtRight);
        SetFloat(saw, "phaseOffset", 0f);
    }

    private static void CreatePulse(Transform parent, string name, float x, float z, float phase)
    {
        GameObject pulsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level3/PulsePlate.prefab");
        if (pulsePrefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(pulsePrefab, parent);
            instance.name = name;
            instance.transform.localPosition = new Vector3(x, 0.12f, z);
            PulsePlateHazard hazard = instance.GetComponent<PulsePlateHazard>();
            SetFloat(hazard, "cycleDuration", 2.45f);
            SetFloat(hazard, "chargeDuration", 0.75f);
            SetFloat(hazard, "dischargeDuration", 0.35f);
            SetFloat(hazard, "phaseOffset", phase);
            SetFloat(hazard, "killRadius", 1.05f);
            SetInt(hazard, "maxRunnersPerDischarge", 2);
            return;
        }

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 0.12f, z);
        CreatePrimitive("Plate", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(1.9f, 0.06f, 1.9f), pulseBodyMaterial);
        GameObject charge = CreatePrimitive("Charge Ring", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(2.2f, 0.025f, 2.2f), pulseChargeMaterial);
        GameObject discharge = CreatePrimitive("Discharge Ring", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.11f, 0f), new Vector3(2.4f, 0.03f, 2.4f), pulseDischargeMaterial);
        PulsePlateHazard pulse = root.AddComponent<PulsePlateHazard>();
        SetFloat(pulse, "cycleDuration", 2.45f);
        SetFloat(pulse, "chargeDuration", 0.75f);
        SetFloat(pulse, "dischargeDuration", 0.35f);
        SetFloat(pulse, "phaseOffset", phase);
        SetFloat(pulse, "killRadius", 1.05f);
        SetInt(pulse, "maxRunnersPerDischarge", 2);
        SetTransform(pulse, "chargeRing", charge.transform);
        SetTransform(pulse, "dischargeRing", discharge.transform);
    }

    private static void CreateRising(Transform parent, string name, float x, float z, float width, float cycle, float phase)
    {
        GameObject instance = InstantiatePrefab("Assets/Prefabs/Traps/Level4/RisingBlock_Single.prefab", parent, name, new Vector3(x, 0f, z));
        RisingBlockHazard hazard = instance.GetComponent<RisingBlockHazard>();
        SetFloat(hazard, "width", width);
        SetFloat(hazard, "raisedHeight", 1.05f);
        SetFloat(hazard, "cycleDuration", cycle);
        SetFloat(hazard, "riseDuration", 0.45f);
        SetFloat(hazard, "raisedHold", 1.1f);
        SetFloat(hazard, "lowerDuration", 0.45f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "warningLead", 1f);
        SetInt(hazard, "lossCap", 6);
    }

    private static void CreateBarrier(Transform parent, string name, float x, float z, float width, float height)
    {
        GameObject instance = InstantiatePrefab("Assets/Prefabs/Traps/Level4/VaultBarrier.prefab", parent, name, new Vector3(x, 0f, z));
        JumpBarrierHazard hazard = instance.GetComponent<JumpBarrierHazard>();
        SetFloat(hazard, "width", width);
        SetFloat(hazard, "height", height);
        SetFloat(hazard, "verticalClearanceMargin", 0.25f);
        SetFloat(hazard, "warningLead", 8f);
        SetInt(hazard, "lossCap", 8);
    }

    private static void CreateSweep(Transform parent, string name, float z, float startX, float endX, float phase)
    {
        string path = startX < endX
            ? "Assets/Prefabs/Traps/Level4/SweepBeam_LeftStart.prefab"
            : "Assets/Prefabs/Traps/Level4/SweepBeam_RightStart.prefab";
        GameObject instance = InstantiatePrefab(path, parent, name, new Vector3(0f, 0.7f, z));
        SweepBeamHazard hazard = instance.GetComponent<SweepBeamHazard>();
        SetFloat(hazard, "startX", startX);
        SetFloat(hazard, "endX", endX);
        SetFloat(hazard, "beamHeight", 0.65f);
        SetFloat(hazard, "beamWidth", 1.4f);
        SetFloat(hazard, "travelDuration", 2.8f);
        SetFloat(hazard, "endpointPause", 0.5f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "warningLead", 1f);
        SetInt(hazard, "lossCap", 5);
    }

    private static void CreateShutter(Transform parent, string name, float x, float z, float phase)
    {
        GameObject instance = InstantiatePrefab("Assets/Prefabs/Traps/Level4/ShutterBlock_Single.prefab", parent, name, new Vector3(x, 0f, z));
        ShutterBlockHazard hazard = instance.GetComponent<ShutterBlockHazard>();
        SetFloat(hazard, "panelWidth", 1.8f);
        SetFloat(hazard, "panelHeight", 1f);
        SetFloat(hazard, "openDuration", 1.1f);
        SetFloat(hazard, "closeDuration", 0.35f);
        SetFloat(hazard, "raisedHold", 1.25f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "warningLead", 0.8f);
        SetInt(hazard, "lossCap", 6);
    }

    private static void CreateSpike(Transform parent, string name, float z, float startX, float endX, float phase, float hitCheckEndZ)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, 0.55f, z);
        CreatePrimitive("Rail", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.48f, 0f), new Vector3(8.8f, 0.12f, 0.24f), railMaterial);
        CreatePrimitive("Start Endpoint", PrimitiveType.Cylinder, root.transform, new Vector3(startX, -0.28f, 0f), new Vector3(0.35f, 0.15f, 0.35f), markerMaterial);
        CreatePrimitive("End Endpoint", PrimitiveType.Cylinder, root.transform, new Vector3(endX, -0.28f, 0f), new Vector3(0.35f, 0.15f, 0.35f), markerMaterial);
        GameObject carriage = CreatePrimitive("Spiked Carriage", PrimitiveType.Cube, root.transform, new Vector3(startX, 0f, 0f), new Vector3(1.55f, 0.7f, 1.1f), spikeMaterial);
        CreatePrimitive("Spike Tip 01", PrimitiveType.Cylinder, carriage.transform, new Vector3(0.45f, 0.42f, 0f), new Vector3(0.18f, 0.28f, 0.18f), spikeMaterial);
        CreatePrimitive("Spike Tip 02", PrimitiveType.Cylinder, carriage.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.18f, 0.28f, 0.18f), spikeMaterial);
        CreatePrimitive("Spike Tip 03", PrimitiveType.Cylinder, carriage.transform, new Vector3(-0.45f, 0.42f, 0f), new Vector3(0.18f, 0.28f, 0.18f), spikeMaterial);

        SpikeSweepHazard hazard = root.AddComponent<SpikeSweepHazard>();
        SetFloat(hazard, "startX", startX);
        SetFloat(hazard, "endX", endX);
        SetFloat(hazard, "travelDuration", 3f);
        SetFloat(hazard, "endpointPause", 0.75f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "approachDistance", 24f);
        SetFloat(hazard, "hitCheckEndZ", hitCheckEndZ);
        SetFloat(hazard, "killRadius", 0.65f);
        SetFloat(hazard, "runnerCollisionPadding", 0.25f);
        SetFloat(hazard, "verticalHitRange", 0.8f);
        SetInt(hazard, "maxRunnersPerLeg", 4);
        SetTransform(hazard, "carriageVisual", carriage.transform);
    }

    private static void CreateHammer(Transform parent, string name, float x, float z, float phase)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 3.2f, z);
        CreatePrimitive("Reach Marker", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -3.08f, 0f), new Vector3(4.2f, 0.025f, 1.1f), markerMaterial);
        GameObject arm = CreatePrimitive("Hammer Arm", PrimitiveType.Cube, root.transform, new Vector3(0f, -1.1f, 0f), new Vector3(4f, 0.25f, 0.25f), hammerMaterial);
        GameObject head = CreatePrimitive("Hammer Head", PrimitiveType.Sphere, root.transform, new Vector3(0f, -2.2f, 0f), new Vector3(0.9f, 0.9f, 0.9f), hammerMaterial);
        SwingHammerHazard hazard = root.AddComponent<SwingHammerHazard>();
        SetFloat(hazard, "oscillationDuration", 2.6f);
        SetFloat(hazard, "angleRange", 55f);
        SetFloat(hazard, "phaseOffset", phase);
        SetFloat(hazard, "activationDistance", 22f);
        SetFloat(hazard, "armLength", 2f);
        SetFloat(hazard, "killRadius", 0.8f);
        SetFloat(hazard, "verticalHitRange", 1.25f);
        SetInt(hazard, "maxHeadRunnersPerCycle", 2);
        SetInt(hazard, "maxArmRunnersPerCycle", 1);
        SetFloat(hazard, "armVerticalHitRange", 2.2f);
        SetTransform(hazard, "armVisual", arm.transform);
        SetTransform(hazard, "hammerHead", head.transform);
    }

    private static void CreateCannon(Transform parent, string name, float x, float z, float phase)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, 1.4f, z);
        CreatePrimitive("Cannon Body", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -1.4f, 0f), new Vector3(0.8f, 1.4f, 0.8f), cannonMaterial);
        CreatePrimitive("Cannon Barrel", PrimitiveType.Cube, root.transform, new Vector3(-Mathf.Sign(x) * 0.45f, -0.55f, 0f), new Vector3(0.9f, 0.25f, 0.25f), cannonMaterial);
        CreatePrimitive("Charge Cue", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -0.55f, 0f), new Vector3(1.6f, 0.02f, 1.6f), telegraphMaterial);
        ProjectileLauncher launcher = root.AddComponent<ProjectileLauncher>();
        SetInt(launcher, "poolSize", 2);
        SetFloat(launcher, "fireInterval", 3.5f);
        SetFloat(launcher, "triggerRadius", 14f);
        SetColor(launcher, "projectileColor", new Color(1f, 0.2f, 0.9f));
        SetFloat(launcher, "initialFireDelay", phase);
    }

    private static void CreateCrackedSpan(Transform parent, string name, float z, string prefabPath, int playerOnlyLayer)
    {
        GameObject instance = InstantiatePrefab(prefabPath, parent, name, new Vector3(0f, 0f, z));
        CrackedSpanHazard hazard = instance.GetComponent<CrackedSpanHazard>();
        SetFloat(hazard, "spanLength", 4f);
        SetFloat(hazard, "requiredClearance", 1.25f);
        SetFloat(hazard, "stopperWidth", 9.2f);
        SetFloat(hazard, "stopperHeight", 1.15f);
        SetFloat(hazard, "stopperDepth", 0.25f);
        SetFloat(hazard, "warningDistance", 10f);
        SetInt(hazard, "failedLossCap", 10);
        SetFloat(hazard, "failedLossPercent", 0.15f);
        SetInt(hazard, "leadStopperLayer", playerOnlyLayer);
        SetFloat(hazard, "hitPadding", 0f);
        SetBool(hazard, "usePhysicalGap", true);
        SetFloat(hazard, "pitDepth", 2.5f);
        SetFloat(hazard, "roadSurfaceOffset", 0f);
        SetFloat(hazard, "farEdgeClearance", 0.15f);
        SetFloat(hazard, "runnerLandingTolerance", 0.05f);

        Transform bridge = instance.transform.Find("Bridge");
        if (bridge != null) bridge.gameObject.SetActive(false);
        Transform leadStopper = instance.transform.Find("LeadStopper");
        if (leadStopper != null) leadStopper.gameObject.SetActive(false);
    }

    private static void CreateCrackPrefabFamilies(int playerOnlyLayer)
    {
        EnsureFolder("Assets/Prefabs/Traps");
        EnsureFolder("Assets/Prefabs/Traps/Level5");
        EnsureCrackPrefab("Assets/Prefabs/Traps/Level5/CrackedSpanBase.prefab", "CrackedSpanBase", 4f, playerOnlyLayer, false);
        EnsureCrackPrefab("Assets/Prefabs/Traps/Level5/CrackedSpan_Short.prefab", "CrackedSpan_Short", 4f, playerOnlyLayer, false);
        EnsureCrackPrefab("Assets/Prefabs/Traps/Level5/CrackedSpan_Final.prefab", "CrackedSpan_Final", 4f, playerOnlyLayer, true);
        ConfigureCrackPrefabForGap("Assets/Prefabs/Traps/Level5/CrackedSpanBase.prefab", playerOnlyLayer, 4f);
        ConfigureCrackPrefabForGap("Assets/Prefabs/Traps/Level5/CrackedSpan_Short.prefab", playerOnlyLayer, 4f);
        ConfigureCrackPrefabForGap("Assets/Prefabs/Traps/Level5/CrackedSpan_Final.prefab", playerOnlyLayer, 4f);
        EnsureTelegraphPrefab("Assets/Prefabs/Traps/Level5/CrackTelegraph.prefab");
        EnsureLeadStopperPrefab("Assets/Prefabs/Traps/Level5/CrackLeadStopper.prefab", playerOnlyLayer);
    }

    private static void EnsureCrackPrefab(string path, string name, float spanLength, int playerOnlyLayer, bool finalStyle)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject root = new GameObject(name);
        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);

        Material surface = finalStyle ? fractureSurfaceMaterial : fractureSurfaceMaterial;
        Material edge = finalStyle ? fractureEdgeMaterial : fractureEdgeMaterial;
        CreatePrimitive("CrackBed", PrimitiveType.Cube, visual, new Vector3(0f, -0.015f, spanLength * 0.5f), new Vector3(9.2f, 0.03f, spanLength), surface);
        CreatePrimitive("CrackCore", PrimitiveType.Cube, visual, new Vector3(0f, 0.03f, spanLength * 0.5f), new Vector3(finalStyle ? 0.6f : 0.45f, 0.06f, spanLength - 0.3f), surface);
        CreatePrimitive("NearLip", PrimitiveType.Cube, visual, new Vector3(0f, 0.04f, 0.05f), new Vector3(9.2f, 0.12f, 0.12f), edge);
        CreatePrimitive("FarLip", PrimitiveType.Cube, visual, new Vector3(0f, 0.04f, spanLength - 0.05f), new Vector3(9.2f, 0.12f, 0.12f), edge);
        CreatePrimitive("LeftEdge", PrimitiveType.Cube, visual, new Vector3(-4.55f, 0.05f, spanLength * 0.5f), new Vector3(0.10f, 0.10f, spanLength), edge);
        CreatePrimitive("RightEdge", PrimitiveType.Cube, visual, new Vector3(4.55f, 0.05f, spanLength * 0.5f), new Vector3(0.10f, 0.10f, spanLength), edge);

        for (int i = 0; i < 4; i++)
        {
            float crackZ = 0.65f + i * 0.85f;
            CreatePrimitive("Crack Mark " + (i + 1), PrimitiveType.Cube, visual, new Vector3(0f, 0.07f, crackZ), new Vector3(8.8f, 0.025f, 0.07f), edge);
        }

        Transform telegraph = new GameObject("Telegraph").transform;
        telegraph.SetParent(root.transform, false);
        CreatePrimitive("Approach Marker 10m", PrimitiveType.Cube, telegraph, new Vector3(0f, 0.06f, -10f), new Vector3(9f, 0.04f, 0.12f), telegraphMaterial);
        CreatePrimitive("Approach Marker 05m", PrimitiveType.Cube, telegraph, new Vector3(0f, 0.06f, -5f), new Vector3(9f, 0.04f, 0.12f), telegraphMaterial);
        CreatePrimitive("Near Edge Lamp L", PrimitiveType.Cube, telegraph, new Vector3(-4.55f, 0.18f, 0f), new Vector3(0.18f, 0.32f, 0.32f), edge);
        CreatePrimitive("Near Edge Lamp R", PrimitiveType.Cube, telegraph, new Vector3(4.55f, 0.18f, 0f), new Vector3(0.18f, 0.32f, 0.32f), edge);
        CreatePrimitive("Far Edge Lamp L", PrimitiveType.Cube, telegraph, new Vector3(-4.55f, 0.18f, spanLength), new Vector3(0.18f, 0.32f, 0.32f), edge);
        CreatePrimitive("Far Edge Lamp R", PrimitiveType.Cube, telegraph, new Vector3(4.55f, 0.18f, spanLength), new Vector3(0.18f, 0.32f, 0.32f), edge);

        GameObject stopper = new GameObject("LeadStopper");
        stopper.transform.SetParent(root.transform, false);
        stopper.transform.localPosition = new Vector3(0f, 0f, -0.125f);
        stopper.layer = playerOnlyLayer;
        BoxCollider stopperCollider = stopper.AddComponent<BoxCollider>();
        stopperCollider.isTrigger = false;
        stopperCollider.center = new Vector3(0f, 0.575f, 0f);
        stopperCollider.size = new Vector3(9.2f, 1.15f, 0.25f);

        GameObject bridge = new GameObject("Bridge");
        bridge.transform.SetParent(root.transform, false);
        bridge.transform.localPosition = new Vector3(0f, -0.1f, spanLength * 0.5f);
        BoxCollider bridgeCollider = bridge.AddComponent<BoxCollider>();
        bridgeCollider.isTrigger = false;
        bridgeCollider.size = new Vector3(9.2f, 0.2f, spanLength + 0.1f);

        new GameObject("HitVolume").transform.SetParent(root.transform, false);

        CrackedSpanHazard hazard = root.AddComponent<CrackedSpanHazard>();
        SetFloat(hazard, "spanLength", spanLength);
        SetFloat(hazard, "requiredClearance", 1.25f);
        SetFloat(hazard, "stopperWidth", 9.2f);
        SetFloat(hazard, "stopperHeight", 1.15f);
        SetFloat(hazard, "stopperDepth", 0.25f);
        SetFloat(hazard, "warningDistance", 10f);
        SetInt(hazard, "failedLossCap", 10);
        SetFloat(hazard, "failedLossPercent", 0.15f);
        SetInt(hazard, "leadStopperLayer", playerOnlyLayer);
        SetFloat(hazard, "hitPadding", 0f);
        SetBool(hazard, "usePhysicalGap", true);
        SetFloat(hazard, "pitDepth", 2.5f);
        SetFloat(hazard, "roadSurfaceOffset", 0f);
        SetFloat(hazard, "farEdgeClearance", 0.15f);
        SetFloat(hazard, "runnerLandingTolerance", 0.05f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ConfigureCrackPrefabForGap(string path, int playerOnlyLayer, float spanLength)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            CrackedSpanHazard hazard = root.GetComponent<CrackedSpanHazard>();
            if (hazard != null)
            {
                SetBool(hazard, "usePhysicalGap", true);
                SetFloat(hazard, "pitDepth", 2.5f);
                SetFloat(hazard, "roadSurfaceOffset", 0f);
                SetFloat(hazard, "farEdgeClearance", 0.15f);
                SetFloat(hazard, "runnerLandingTolerance", 0.05f);
                SetInt(hazard, "leadStopperLayer", playerOnlyLayer);
            }

            Transform stopper = root.transform.Find("LeadStopper");
            if (stopper != null)
            {
                stopper.gameObject.layer = playerOnlyLayer;
                stopper.gameObject.SetActive(false);
            }

            Transform bridge = root.transform.Find("Bridge");
            if (bridge != null) bridge.gameObject.SetActive(false);

            Transform visual = root.transform.Find("Visual");
            if (visual != null)
            {
                Transform bed = visual.Find("CrackBed");
                if (bed != null)
                {
                    bed.localPosition = new Vector3(0f, -2.5f, spanLength * 0.5f);
                    bed.localScale = new Vector3(9.2f, bed.localScale.y, spanLength);
                }

                Transform core = visual.Find("CrackCore");
                if (core != null)
                {
                    core.localPosition = new Vector3(0f, -2.35f, spanLength * 0.5f);
                    core.localScale = new Vector3(core.localScale.x, core.localScale.y, Mathf.Max(0.2f, spanLength - 0.3f));
                }

                for (int i = 0; i < 4; i++)
                {
                    Transform mark = visual.Find("Crack Mark " + (i + 1));
                    if (mark != null)
                    {
                        mark.localPosition = new Vector3(mark.localPosition.x, -2.2f, mark.localPosition.z);
                    }
                }
            }

            // Visual primitives are not allowed to provide an accidental floor;
            // retain only the disabled fallback bridge/stopper colliders.
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (bridge != null && collider.transform == bridge) continue;
                if (stopper != null && collider.transform == stopper) continue;
                collider.enabled = false;
            }

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureTelegraphPrefab(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject root = new GameObject("CrackTelegraph");
        CreatePrimitive("Approach Marker", PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(9f, 0.04f, 0.12f), telegraphMaterial);
        CreatePrimitive("Edge Lamp", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.18f, 0.32f, 0.32f), fractureEdgeMaterial);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void EnsureLeadStopperPrefab(string path, int playerOnlyLayer)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject root = new GameObject("CrackLeadStopper");
        root.layer = playerOnlyLayer;
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.center = new Vector3(0f, 0.575f, 0f);
        collider.size = new Vector3(9.2f, 1.15f, 0.25f);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, string name, Vector3 localPosition)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Level 5 source prefab not found: " + path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        return instance;
    }

    private static Transform CreateSection(Transform parent, string name)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);
        return section.transform;
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

    private static Material LoadOrCreateMaterial(string existingPath, string fallbackName, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(existingPath);
        return existing != null ? existing : EnsureMaterial(fallbackName, color);
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

    private static int EnsurePlayerOnlyLayer()
    {
        int existing = LayerMask.NameToLayer("PlayerOnly");
        if (existing >= 0) return existing;

        UnityEngine.Object[] tagManagers = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagers.Length == 0) return 0;

        SerializedObject serialized = new SerializedObject(tagManagers[0]);
        SerializedProperty layers = serialized.FindProperty("layers");
        if (layers == null) return 0;

        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = "PlayerOnly";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }

        return 0;
    }

    private static void SetFloat(UnityEngine.Object target, string property, float value)
    {
        SetSerialized(target, property, serialized => serialized.floatValue = value);
    }

    private static void SetInt(UnityEngine.Object target, string property, int value)
    {
        SetSerialized(target, property, serialized => serialized.intValue = value);
    }

    private static void SetBool(UnityEngine.Object target, string property, bool value)
    {
        SetSerialized(target, property, serialized => serialized.boolValue = value);
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
        GameObject oldObject = FindInScene(scene, oldName);
        if (oldObject != null) oldObject.name = newName;
    }

    private static void SetText(Scene scene, string path, string value)
    {
        GameObject go = FindInScene(scene, path);
        if (go == null) return;
        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }

    private static Transform FindChildAny(Transform root, params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            Transform result = root.Find(paths[i]);
            if (result != null) return result;
        }

        return null;
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
        {
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
