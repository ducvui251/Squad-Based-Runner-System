using System;
using UnityEditor;
using UnityEngine;

public sealed class RisingBlockPreviewWindow : EditorWindow
{
    private PreviewRenderUtility preview;
    private GameObject previewRoot;
    private GameObject previewFloor;
    private int prefabIndex = 1;
    private float width = 1.8f;
    private float raisedHeight = 1.05f;
    private float cycleDuration = 3.2f;
    private float riseDuration = 0.225f;
    private float raisedHold = 1.1f;
    private float lowerDuration = 0.225f;
    private float phaseOffset;
    private float phaseSeconds;
    private float playbackSpeed = 1f;
    private double lastUpdateTime;
    private bool isPlaying;
    private string loadError;

    [MenuItem("Tools/Spiral Squad/Rising Blocks/Preview & Tune")]
    private static void Open()
    {
        RisingBlockPreviewWindow window = GetWindow<RisingBlockPreviewWindow>();
        window.titleContent = new GUIContent("Rising Block Preview");
        window.minSize = new Vector2(390f, 500f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
        LoadSelectedPrefab();
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
        CleanupPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Rising Block Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This window animates a temporary mesh-only copy in Unity's preview scene. It does not run hazard logic or change the open scene.",
            MessageType.Info);

        string[] prefabNames = new string[RisingBlockTransitionSpeedTool.SharedPrefabCount];
        for (int i = 0; i < prefabNames.Length; i++)
        {
            string path = RisingBlockTransitionSpeedTool.GetSharedPrefabPath(i);
            prefabNames[i] = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        EditorGUI.BeginChangeCheck();
        int selected = EditorGUILayout.Popup("Preview prefab", prefabIndex, prefabNames);
        if (EditorGUI.EndChangeCheck() && selected != prefabIndex)
        {
            prefabIndex = selected;
            LoadSelectedPrefab();
        }

        if (!string.IsNullOrEmpty(loadError))
        {
            EditorGUILayout.HelpBox(loadError, MessageType.Error);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Height(24f)))
            {
                isPlaying = !isPlaying;
                lastUpdateTime = EditorApplication.timeSinceStartup;
            }

            if (GUILayout.Button("Reset Phase", GUILayout.Height(24f)))
            {
                phaseSeconds = phaseOffset;
            }
        }

        playbackSpeed = EditorGUILayout.Slider("Preview playback", playbackSpeed, 0.1f, 2f);

        float effectiveCycle = Mathf.Max(cycleDuration, riseDuration + raisedHold + lowerDuration + 0.1f);
        phaseSeconds = EditorGUILayout.Slider("Cycle time", phaseSeconds, 0f, effectiveCycle);
        float raised01 = RisingBlockHazard.EvaluateRaised01(
            phaseSeconds, cycleDuration, riseDuration, raisedHold, lowerDuration);
        EditorGUILayout.LabelField($"Raised: {raised01:P0}    Cycle: {effectiveCycle:0.00}s");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Cycle {cycleDuration:0.00}s  |  Raised hold {raisedHold:0.00}s", EditorStyles.miniLabel);
        float maxTransition = Mathf.Max(0.05f, cycleDuration * 0.5f);
        riseDuration = EditorGUILayout.Slider("Rise duration", riseDuration, 0.05f, maxTransition);
        lowerDuration = EditorGUILayout.Slider("Lower duration", lowerDuration, 0.05f, maxTransition);

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(
            "Apply writes only riseDuration and lowerDuration on the three shared Rising Block prefab assets. Cycle, hold, phase, scene instances, and other traps are left alone.",
            MessageType.None);
        if (GUILayout.Button("Apply timings to all 3 shared Rising Block prefabs", GUILayout.Height(28f)))
        {
            if (EditorUtility.DisplayDialog(
                    "Apply Rising Block Timings",
                    $"Set rise to {riseDuration:0.###}s and lower to {lowerDuration:0.###}s on all three shared Rising Block prefabs?",
                    "Apply",
                    "Cancel"))
            {
                RisingBlockTransitionSpeedTool.ApplyDurationsToSharedPrefabs(riseDuration, lowerDuration);
                LoadSelectedPrefab();
            }
        }

        EditorGUILayout.Space(8f);
        Rect previewRect = GUILayoutUtility.GetRect(100f, 245f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint && preview != null)
        {
            UpdatePreviewVisual(raised01);
            preview.BeginPreview(previewRect, GUIStyle.none);
            preview.camera.Render();
            Texture renderedPreview = preview.EndPreview();
            GUI.DrawTexture(previewRect, renderedPreview, ScaleMode.StretchToFill, false);
        }
        else
        {
            GUI.Box(previewRect, "Preparing preview...");
        }
    }

    private void EditorUpdate()
    {
        if (!isPlaying)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Max(0f, (float)(now - lastUpdateTime));
        lastUpdateTime = now;
        float effectiveCycle = Mathf.Max(cycleDuration, riseDuration + raisedHold + lowerDuration + 0.1f);
        phaseSeconds = Mathf.Repeat(phaseSeconds + deltaTime * playbackSpeed, effectiveCycle);
        Repaint();
    }

    private void LoadSelectedPrefab()
    {
        CleanupPreview();
        loadError = null;
        string prefabPath = RisingBlockTransitionSpeedTool.GetSharedPrefabPath(prefabIndex);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            loadError = "Could not load Rising Block prefab: " + prefabPath;
            return;
        }

        try
        {
            RisingBlockHazard hazard = root.GetComponentInChildren<RisingBlockHazard>(true);
            Transform visual = root.transform.Find("Visual");
            if (hazard == null || visual == null)
            {
                loadError = "The selected prefab must contain a RisingBlockHazard and a root child named Visual.";
                return;
            }

            SerializedObject serializedHazard = new SerializedObject(hazard);
            width = serializedHazard.FindProperty("width").floatValue;
            raisedHeight = serializedHazard.FindProperty("raisedHeight").floatValue;
            cycleDuration = serializedHazard.FindProperty("cycleDuration").floatValue;
            riseDuration = serializedHazard.FindProperty("riseDuration").floatValue;
            raisedHold = serializedHazard.FindProperty("raisedHold").floatValue;
            lowerDuration = serializedHazard.FindProperty("lowerDuration").floatValue;
            phaseOffset = serializedHazard.FindProperty("phaseOffset").floatValue;
            phaseSeconds = phaseOffset;

            MeshFilter meshFilter = visual.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer meshRenderer = visual.GetComponentInChildren<MeshRenderer>(true);
            if (meshFilter == null || meshFilter.sharedMesh == null || meshRenderer == null)
            {
                loadError = "The Visual hierarchy must contain a MeshFilter with a mesh and a MeshRenderer.";
                return;
            }

            CreatePreviewObjects(visual, meshFilter, meshRenderer);
        }
        catch (Exception exception)
        {
            loadError = "Could not prepare preview: " + exception.Message;
            Debug.LogException(exception);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Repaint();
    }

    private void CreatePreviewObjects(Transform sourceVisual, MeshFilter sourceFilter, MeshRenderer sourceRenderer)
    {
        preview = new PreviewRenderUtility();
        preview.camera.orthographic = true;
        preview.camera.orthographicSize = Mathf.Max(width * 0.8f, raisedHeight * 1.5f, 1.5f);
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = 100f;
        preview.camera.backgroundColor = new Color(0.16f, 0.18f, 0.21f, 1f);
        preview.camera.clearFlags = CameraClearFlags.Color;
        preview.camera.transform.position = new Vector3(width * 1.35f, raisedHeight * 1.1f, -5f);
        preview.camera.transform.LookAt(new Vector3(0f, 0f, 0f));
        preview.lights[0].intensity = 1.15f;
        preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
        preview.lights[1].intensity = 0.7f;

        previewRoot = new GameObject("Rising Block Preview (visual only)")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        GameObject previewMesh = new GameObject("Mesh")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        previewMesh.transform.SetParent(previewRoot.transform, false);
        MeshFilter previewFilter = previewMesh.AddComponent<MeshFilter>();
        previewFilter.sharedMesh = sourceFilter.sharedMesh;
        MeshRenderer previewRenderer = previewMesh.AddComponent<MeshRenderer>();
        previewRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        previewRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
        previewRenderer.receiveShadows = sourceRenderer.receiveShadows;

        Vector3 sourceScale = sourceVisual.lossyScale;
        Vector3 meshScale = sourceFilter.transform.lossyScale;
        previewRoot.transform.localScale = Vector3.one;
        previewRoot.transform.position = Vector3.zero;
        previewRoot.transform.localRotation = sourceVisual.localRotation;
        previewMesh.transform.localPosition = sourceVisual.InverseTransformPoint(sourceFilter.transform.position);
        previewMesh.transform.localRotation = Quaternion.Inverse(sourceVisual.rotation) * sourceFilter.transform.rotation;
        previewMesh.transform.localScale = new Vector3(
            SafeDivide(meshScale.x, sourceScale.x),
            SafeDivide(meshScale.y, sourceScale.y),
            SafeDivide(meshScale.z, sourceScale.z));
        preview.AddSingleGO(previewRoot);

        previewFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        previewFloor.name = "Rising Block Preview Road Surface";
        previewFloor.hideFlags = HideFlags.HideAndDontSave;
        Collider floorCollider = previewFloor.GetComponent<Collider>();
        if (floorCollider != null)
        {
            UnityEngine.Object.DestroyImmediate(floorCollider);
        }
        previewFloor.transform.position = new Vector3(0f, -0.06f, 0f);
        previewFloor.transform.localScale = new Vector3(Mathf.Max(4f, width * 2.5f), 0.12f, 2f);
        preview.AddSingleGO(previewFloor);
    }

    private void UpdatePreviewVisual(float raised01)
    {
        if (previewRoot == null)
        {
            return;
        }

        previewRoot.transform.localScale = new Vector3(width, raisedHeight, 0.9f);
        previewRoot.transform.localPosition = new Vector3(
            0f,
            Mathf.Lerp(-raisedHeight * 0.5f, raisedHeight * 0.5f, raised01),
            0f);
    }

    private void CleanupPreview()
    {
        if (preview != null)
        {
            preview.Cleanup();
            preview = null;
            previewRoot = null;
            previewFloor = null;
        }
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
    }
}
