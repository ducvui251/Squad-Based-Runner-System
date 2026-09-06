using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Level1Section2Builder
{
    private const string ScenePath = "Assets/Scenes/Level1.unity";
    private const string LevelRootName = "Level 1 - Multiplier Ramp-Up";
    private const string SectionName = "Section 2 - The Sawmill Bottleneck";
    private const string ConeModelPath = "Assets/Models/Objects/Cone.fbx";
    private const string SawModelPath = "Assets/Models/Objects/Circular Saw.fbx";

    static Level1Section2Builder()
    {
        EditorApplication.delayCall += BuildIfMissing;
    }

    [MenuItem("Spiral Squad/Levels/Rebuild Level1 Section 2")]
    public static void RebuildFromMenu()
    {
        BuildSection(overwriteExisting: true);
    }

    private static void BuildIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            return;
        }

        if (GameObject.Find(LevelRootName + "/" + SectionName) != null)
        {
            return;
        }

        BuildSection(overwriteExisting: false);
    }

    private static void BuildSection(bool overwriteExisting)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject levelRoot = GameObject.Find(LevelRootName);
        if (levelRoot == null)
        {
            Debug.LogWarning("Level1Section2Builder: Level root not found: " + LevelRootName);
            return;
        }

        GameObject existing = GameObject.Find(LevelRootName + "/" + SectionName);
        if (existing != null)
        {
            if (!overwriteExisting)
            {
                return;
            }

            Object.DestroyImmediate(existing);
        }

        GameObject section = new GameObject(SectionName);
        section.transform.SetParent(levelRoot.transform, false);
        section.transform.position = new Vector3(0f, 0f, 40f);

        GameObject funnel = new GameObject("Funnel Cones Z45-Z55");
        funnel.transform.SetParent(section.transform, false);

        float[] leftX = { -4.5f, -3.5f, -2.5f, -1.5f };
        float[] rightX = { 4.5f, 3.5f, 2.5f, 1.5f };
        float[] z = { 45f, 48.333f, 51.667f, 55f };
        for (int i = 0; i < 4; i++)
        {
            CreateCone(funnel.transform, "Left Funnel Cone " + (i + 1), leftX[i], z[i]);
            CreateCone(funnel.transform, "Right Funnel Cone " + (i + 1), rightX[i], z[i]);
        }

        GameObject saws = new GameObject("Saw Traps Z62-Z72");
        saws.transform.SetParent(section.transform, false);
        CreateSaw(saws.transform, "Saw Trap 1 Z62", 62f);
        CreateSaw(saws.transform, "Saw Trap 2 Z72", 72f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Level1Section2Builder: Added Section 2 - The Sawmill Bottleneck to Level1 using object model assets.");
    }

    private static void CreateCone(Transform parent, string name, float x, float z)
    {
        GameObject cone = new GameObject(name);
        cone.transform.SetParent(parent, false);
        cone.transform.position = new Vector3(x, 0f, z);
        SetTagIfExists(cone, "Cone");

        GameObject visual = InstantiateModelVisual(ConeModelPath, "Cone Model", cone.transform);
        if (visual != null)
        {
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            FitVisualToLocalBounds(visual, new Vector3(0f, 0.65f, 0f), 1.3f);
        }

        CapsuleCollider capsule = cone.AddComponent<CapsuleCollider>();
        capsule.radius = 0.35f;
        capsule.height = 1.3f;
        capsule.center = new Vector3(0f, 0.65f, 0f);

        Rigidbody rigidbody = cone.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        ConeHazard hazard = cone.AddComponent<ConeHazard>();
        SerializedObject serializedHazard = new SerializedObject(hazard);
        serializedHazard.FindProperty("killRadius").floatValue = 0.85f;
        serializedHazard.FindProperty("boundaryX").floatValue = 5f;
        serializedHazard.FindProperty("boundaryEliminationMargin").floatValue = 0.25f;
        serializedHazard.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateSaw(Transform parent, string name, float z)
    {
        GameObject saw = new GameObject(name);
        saw.transform.SetParent(parent, false);
        saw.transform.position = new Vector3(0f, 0.3f, z);
        SetTagIfExists(saw, "Saw");

        Rigidbody rigidbody = saw.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        // Kill-volume collider that covers the whole blade disc (radius ~1.5, thickness ~0.04).
        // A BoxCollider is used here so the hazard covers every part of the saw.
        BoxCollider killVolume = saw.AddComponent<BoxCollider>();
        killVolume.size = new Vector3(3.3f, 3.3f, 0.25f);
        killVolume.isTrigger = true;

        CircularSaw circularSaw = saw.AddComponent<CircularSaw>();
        SerializedObject serializedSaw = new SerializedObject(circularSaw);
        serializedSaw.FindProperty("moveSpeed").floatValue = 3f;
        serializedSaw.FindProperty("spinSpeed").floatValue = 720f;
        serializedSaw.FindProperty("killRadius").floatValue = 0.85f;
        serializedSaw.FindProperty("runnerHitRadius").floatValue = 0.35f;
        serializedSaw.FindProperty("useExplicitXLimits").boolValue = true;
        serializedSaw.FindProperty("leftLimitOverride").floatValue = -2f;
        serializedSaw.FindProperty("rightLimitOverride").floatValue = 2f;
        serializedSaw.FindProperty("useSineMotion").boolValue = true;
        serializedSaw.ApplyModifiedPropertiesWithoutUndo();

        GameObject blade = InstantiateModelVisual(SawModelPath, "Blade", saw.transform);
        if (blade != null)
        {
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            FitVisualToLocalBounds(blade, Vector3.zero, 1.7f);
        }
    }

    private static GameObject InstantiateModelVisual(string assetPath, string name, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Level1Section2Builder: Model not found: " + assetPath);
            return null;
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        StripGameplayComponents(visual);
        return visual;
    }

    private static void StripGameplayComponents(GameObject visual)
    {
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rigidbody);
        }
    }

    private static void FitVisualToLocalBounds(GameObject visual, Vector3 desiredLocalCenter, float targetMaxSize)
    {
        if (!TryGetRendererBounds(visual, out Bounds bounds))
        {
            return;
        }

        float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxSize > 0.0001f)
        {
            float scaleFactor = targetMaxSize / maxSize;
            visual.transform.localScale *= scaleFactor;
        }

        if (TryGetRendererBounds(visual, out bounds))
        {
            Vector3 currentLocalCenter = visual.transform.parent.InverseTransformPoint(bounds.center);
            visual.transform.localPosition += desiredLocalCenter - currentLocalCenter;
        }
    }

    private static bool TryGetRendererBounds(GameObject gameObject, out Bounds bounds)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(gameObject.transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void SetTagIfExists(GameObject gameObject, string tag)
    {
        try
        {
            gameObject.tag = tag;
        }
        catch (UnityException)
        {
            gameObject.tag = "Untagged";
        }
    }
}
