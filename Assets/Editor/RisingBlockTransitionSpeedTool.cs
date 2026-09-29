using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RisingBlockTransitionSpeedTool
{
    private const float TransitionDuration = 0.225f;

    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Traps/Level4/RisingBlockBase.prefab",
        "Assets/Prefabs/Traps/Level4/RisingBlock_Single.prefab",
        "Assets/Prefabs/Traps/Level4/RisingBlock_Double.prefab"
    };

    [MenuItem("Tools/Spiral Squad/Rising Blocks/Set 2x Rise and Lower Speed")]
    private static void ApplyFastTransitionDurations()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            EditorUtility.DisplayDialog(
                "Close Prefab Mode",
                "Close the current Prefab Stage before changing the Rising Block prefab assets.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Update Rising Block Timing",
                "Set only the rise and lower durations to 0.225 seconds in the three shared Rising Block prefabs?\n\nCycle duration and raised hold are preserved. Scene files and other hazard components are not directly modified.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        ApplyDurationsToSharedPrefabs(TransitionDuration, TransitionDuration);
    }

    public static int SharedPrefabCount => PrefabPaths.Length;

    public static string GetSharedPrefabPath(int index) => PrefabPaths[index];

    public static void ApplyDurationsToSharedPrefabs(float riseDuration, float lowerDuration)
    {
        if (float.IsNaN(riseDuration) || float.IsInfinity(riseDuration) ||
            float.IsNaN(lowerDuration) || float.IsInfinity(lowerDuration) ||
            riseDuration < 0.05f || lowerDuration < 0.05f)
        {
            EditorUtility.DisplayDialog("Invalid Rising Block Timing", "Rise and lower durations must be finite values of at least 0.05 seconds.", "OK");
            return;
        }

        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            EditorUtility.DisplayDialog(
                "Close Prefab Mode",
                "Close the current Prefab Stage before changing the Rising Block prefab assets.",
                "OK");
            return;
        }

        try
        {
            foreach (string prefabPath in PrefabPaths)
            {
                ValidatePrefab(prefabPath, riseDuration, lowerDuration);
            }

            int prefabCount = 0;
            int componentCount = 0;
            foreach (string prefabPath in PrefabPaths)
            {
                int changedComponents = UpdatePrefab(prefabPath, riseDuration, lowerDuration);
                if (changedComponents > 0)
                {
                    componentCount += changedComponents;
                    prefabCount++;
                }
            }

            Debug.Log($"Updated rise/lower timing on {componentCount} RisingBlockHazard component(s) in {prefabCount} prefab asset(s). Scene files and other hazard components were not directly modified.");
        }
        catch (Exception exception)
        {
            Debug.LogError("Rising Block timing update failed: " + exception);
            EditorUtility.DisplayDialog("Rising Block Update Failed", exception.Message, "OK");
        }
    }

    private static void ValidatePrefab(string prefabPath, float riseValue, float lowerValue)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            throw new InvalidOperationException("Rising Block prefab was not found: " + prefabPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            throw new InvalidOperationException("Could not load Rising Block prefab: " + prefabPath);
        }

        try
        {
            RisingBlockHazard[] hazards = root.GetComponentsInChildren<RisingBlockHazard>(true);
            if (hazards.Length == 0)
            {
                throw new InvalidOperationException("No RisingBlockHazard component was found in: " + prefabPath);
            }

            foreach (RisingBlockHazard hazard in hazards)
            {
                SerializedObject serializedHazard = new SerializedObject(hazard);
                SerializedProperty riseDuration = serializedHazard.FindProperty("riseDuration");
                SerializedProperty lowerDuration = serializedHazard.FindProperty("lowerDuration");
                SerializedProperty cycleDuration = serializedHazard.FindProperty("cycleDuration");
                if (riseDuration == null || lowerDuration == null || cycleDuration == null)
                {
                    throw new InvalidOperationException("The rise/lower timing fields were not found in: " + prefabPath);
                }

                float maxTransitionDuration = cycleDuration.floatValue * 0.5f;
                if (riseValue > maxTransitionDuration || lowerValue > maxTransitionDuration)
                {
                    throw new InvalidOperationException(
                        $"Rise/lower durations cannot exceed half of the cycle ({maxTransitionDuration:0.###}s) in: {prefabPath}");
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int UpdatePrefab(string prefabPath, float riseValue, float lowerValue)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            throw new InvalidOperationException("Could not load Rising Block prefab: " + prefabPath);
        }

        try
        {
            RisingBlockHazard[] hazards = root.GetComponentsInChildren<RisingBlockHazard>(true);
            int changedComponents = 0;
            foreach (RisingBlockHazard hazard in hazards)
            {
                SerializedObject serializedHazard = new SerializedObject(hazard);
                SerializedProperty riseDuration = serializedHazard.FindProperty("riseDuration");
                SerializedProperty lowerDuration = serializedHazard.FindProperty("lowerDuration");

                bool changed = false;
                if (Mathf.Abs(riseDuration.floatValue - riseValue) > 0.0001f)
                {
                    riseDuration.floatValue = riseValue;
                    changed = true;
                }

                if (Mathf.Abs(lowerDuration.floatValue - lowerValue) > 0.0001f)
                {
                    lowerDuration.floatValue = lowerValue;
                    changed = true;
                }

                if (changed)
                {
                    serializedHazard.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(hazard);
                    changedComponents++;
                }
            }

            if (changedComponents > 0 && PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
            {
                throw new InvalidOperationException("Could not save Rising Block prefab: " + prefabPath);
            }

            return changedComponents;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
