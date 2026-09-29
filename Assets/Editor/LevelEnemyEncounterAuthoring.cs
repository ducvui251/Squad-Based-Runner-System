using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Owns the authored red-enemy spawn points shared by the existing level scenes.
/// Each encounter uses a separate GroupSpawner object because Enemy.AlertGroup
/// alerts enemies under their immediate parent.
/// </summary>
public static class LevelEnemyEncounterAuthoring
{
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
    private const string GroupsRootName = "Enemy Groups";

    private struct EncounterSpec
    {
        public string name;
        public float x;
        public float z;
        public int spawnAmount;

        public EncounterSpec(string name, float x, float z, int spawnAmount)
        {
            this.name = name;
            this.x = x;
            this.z = z;
            this.spawnAmount = spawnAmount;
        }
    }

    public static void RebuildForLevel(GameObject levelRoot, int levelNumber)
    {
        if (levelRoot == null)
            throw new ArgumentNullException(nameof(levelRoot));

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (enemyPrefab == null || enemyPrefab.GetComponent<Enemy>() == null)
            throw new InvalidOperationException("Regular Enemy prefab with Enemy component not found at " + EnemyPrefabPath + ".");

        EncounterSpec[] encounters = GetEncounters(levelNumber);
        Transform groupsRoot = levelRoot.transform.Find(GroupsRootName);
        if (groupsRoot == null)
        {
            GameObject groupsObject = new GameObject(GroupsRootName);
            groupsObject.transform.SetParent(levelRoot.transform, false);
            groupsRoot = groupsObject.transform;
        }

        for (int i = groupsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = groupsRoot.GetChild(i);
            if (child.name.StartsWith("Red Enemy Encounter ", StringComparison.Ordinal) ||
                child.name.StartsWith("Red Enemy Spawn Point ", StringComparison.Ordinal))
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        for (int i = 0; i < encounters.Length; i++)
            CreateEncounter(groupsRoot, enemyPrefab, encounters[i], i + 1);

        EditorUtility.SetDirty(levelRoot);
        Debug.Log("ENEMY_ENCOUNTERS_LEVEL_" + levelNumber + ": authored " + encounters.Length + " encounter(s).");
    }

    private static EncounterSpec[] GetEncounters(int levelNumber)
    {
        switch (levelNumber)
        {
            // Keep Level 1's final encounter just ahead of its saved finish trigger.
            case 1: return new[] { new EncounterSpec("Before Finish Trigger", 3f, 68.5f, 15) };
            // After the second falling trap; an optional left-side encounter ahead of Gate B.
            case 2: return new[] { new EncounterSpec("After Falling Trap Relay", -3f, 96f, 20) };
            // Recovery lane after the second tutorial shuttle and before Gate B.
            case 3: return new[] { new EncounterSpec("Shuttle Recovery before Gate B", -3f, 118f, 20) };
            // Clear interval between the first sweep beams and the shutter yard.
            case 4: return new[] { new EncounterSpec("Sweep Beam Recovery", 3f, 181f, 25) };
            // After the opening cone weave and before Gate B.
            case 5: return new[] { new EncounterSpec("Opening Relay Recovery before Gate B", -3f, 138f, 25) };
            default: throw new ArgumentOutOfRangeException(nameof(levelNumber), "Only Levels 1-5 have approved encounter positions.");
        }
    }

    private static void CreateEncounter(Transform groupsRoot, GameObject enemyPrefab, EncounterSpec spec, int encounterNumber)
    {
        GameObject spawnPoint = new GameObject("Red Enemy Spawn Point " + encounterNumber.ToString("00") + " - " + spec.name);
        spawnPoint.transform.SetParent(groupsRoot, false);
        spawnPoint.transform.localPosition = new Vector3(spec.x, 0f, spec.z);

        GroupSpawner spawner = spawnPoint.AddComponent<GroupSpawner>();
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("objectToSpawn").objectReferenceValue = enemyPrefab;
        serializedSpawner.FindProperty("amount").intValue = spec.spawnAmount;
        serializedSpawner.FindProperty("parentFolder").objectReferenceValue = spawnPoint.transform;
        serializedSpawner.FindProperty("radiusFactor").floatValue = 0.3f;
        serializedSpawner.FindProperty("angleFactor").floatValue = 1f;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
    }
}
