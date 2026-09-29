using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CollisionAuditValidator
{
    [MenuItem("SpiralSquad/Collision/Report Active Scene")]
    private static void ReportActive() => Debug.Log(Report(SceneManager.GetActiveScene()));

    public static string Report(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded scene is required.");
        var report = new StringBuilder("COLLISION MANIFEST " + scene.path + " guid=" + AssetDatabase.AssetPathToGUID(scene.path) + " dirty=" + scene.isDirty + "\n");
        foreach (var root in scene.GetRootGameObjects())
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var component in transform.GetComponents<Component>())
            {
                if (component == null) { report.AppendLine("ERROR missing script: " + Path(transform)); continue; }
                if (component is Collider collider)
                {
                    report.Append(Path(transform)).Append(" id=").Append(GlobalObjectId.GetGlobalObjectIdSlow(collider))
                        .Append(" prefab=").Append(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collider.gameObject))
                        .Append(" enabledOverride=").Append(new SerializedObject(collider).FindProperty("m_Enabled").prefabOverride)
                        .Append(" enabled=").Append(collider.enabled).Append(" activeSelf=").Append(collider.gameObject.activeSelf)
                        .Append(" activeHierarchy=").Append(collider.gameObject.activeInHierarchy)
                        .Append(" trigger=").Append(collider.isTrigger).Append(" layer=").Append(collider.gameObject.layer)
                        .Append(" body=").Append(collider.attachedRigidbody != null ? Path(collider.attachedRigidbody.transform) : "none")
                        .Append(" local=").Append(CollisionAuthoringGenerator.ShapeText(collider))
                        .Append(" world=").Append(WorldBounds(collider)).AppendLine();
                }
                else if (component is MonoBehaviour && IsConsumer(component))
                {
                    report.Append(Path(transform)).Append(" consumer=").Append(component.GetType().Name)
                        .Append(" id=").Append(GlobalObjectId.GetGlobalObjectIdSlow(component));
                    var property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.Float) report.Append(' ').Append(property.name).Append('=').Append(property.floatValue);
                        else if (property.propertyType == SerializedPropertyType.Integer) report.Append(' ').Append(property.name).Append('=').Append(property.intValue);
                    }
                    report.AppendLine();
                }
            }
        }
        foreach (string error in Validate(scene)) report.AppendLine("ERROR " + error);
        report.AppendLine("NOTE Level4 lossCap/warningLead are legacy metadata; immediate overlap and disabled telegraphs remain authoritative.");
        return report.ToString();
    }

    private static bool IsConsumer(Component component)
    {
        return component is FinishGate || component is Gate || component is ConeHazard || component is CircularSaw
            || component is FallingHazard || component is PulsePlateHazard || component is SpikeSweepHazard
            || component is SwingHammerHazard || component is Level4HazardBase || component is CrackedSpanHazard;
    }

    public static List<string> Validate(Scene scene)
    {
        var errors = new List<string>();
        errors.AddRange(ValidateBuilderContracts());
        var components = Components<Component>(scene);
        foreach (Component component in components)
        {
            if (component == null) { errors.Add("Missing script reference"); continue; }
            if (component is BoxCollider box && (!CollisionAuthoringGenerator.Positive(box.size) || !CollisionAuthoringGenerator.Finite(box.center))) errors.Add(Path(box.transform) + " invalid box");
            if (component is SphereCollider sphere && !CollisionAuthoringGenerator.Positive(sphere.radius)) errors.Add(Path(sphere.transform) + " invalid sphere");
            if (component is CapsuleCollider capsule && (!CollisionAuthoringGenerator.Positive(capsule.radius) || !CollisionAuthoringGenerator.Positive(capsule.height))) errors.Add(Path(capsule.transform) + " invalid capsule");
            if (component is MeshCollider mesh && mesh.attachedRigidbody != null && !mesh.attachedRigidbody.isKinematic && !mesh.convex) errors.Add(Path(mesh.transform) + " moving concave collider");
            if (component is FinishGate finish)
            {
                var volume = new SerializedObject(finish).FindProperty("finishVolume").objectReferenceValue as BoxCollider;
                if (volume == null || !volume.enabled || !volume.isTrigger || !volume.gameObject.activeInHierarchy) errors.Add(Path(finish.transform) + " missing active authored finish trigger");
                else if ((volume.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f) errors.Add(Path(volume.transform) + " finish owner must be unscaled");
            }
            if (scene.name == "Level2" && component is ConeHazard cone)
                foreach (var collider in cone.GetComponentsInChildren<Collider>(true))
                    if (collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy) errors.Add(Path(cone.transform) + " damage-only cone has solid collider");
        }
        if (scene.name == "Level1")
        {
            var saw = Find(scene, "Saw Trap 2 Z72");
            if (saw == null || Mathf.Abs(saw.transform.position.z - 72f) > 0.001f) errors.Add("Level1 Saw Trap 2 expected world Z72");
        }
        if (scene.name == "Level3")
        {
            if (Find(scene, "Level 3 - Pulsebound Foundry") == null || Find(scene, "Track 10m x 340m") == null) errors.Add("Level3 root/track identity mismatch");
            if (Components<PulsePlateHazard>(scene).Count != 14) errors.Add("Level3 expected 14 PulsePlateHazard instances");
            if (Components<SpikeSweepHazard>(scene).Count != 4) errors.Add("Level3 expected four spike shuttles");
            if (Components<SwingHammerHazard>(scene).Count == 0) errors.Add("Level3 missing swing hammers");
            foreach (var plate in Components<PulsePlateHazard>(scene))
                if (AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(plate.gameObject)) != "3857dd2ec61e66f4ba246095bb02bec2") errors.Add(Path(plate.transform) + " wrong PulsePlate prefab");
        }
        if (scene.name == "Level5") ValidateFractures(scene, errors);
        return errors;
    }

    /// <summary>
    /// Checks the serialized-field and prefab-path contracts used by the level
    /// builders. This is intentionally source-scoped and read-only: it catches a
    /// stale builder before a rebuild can silently write values Unity no longer
    /// serializes or substitute a collider-free fallback asset.
    /// </summary>
    public static List<string> ValidateBuilderContracts()
    {
        var errors = new List<string>();
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            errors.Add("Unable to resolve project root for builder contract validation");
            return errors;
        }

        CheckBuilderText(System.IO.Path.Combine(projectRoot, "Assets", "Editor", "Level1Section2Builder.cs"), errors,
            mustContain: null,
            mustNotContain: new[] { "runnerHitRadius", "useSineMotion" });
        CheckBuilderText(System.IO.Path.Combine(projectRoot, "Assets", "Editor", "BuildLevel3PulseboundFoundry.cs"), errors,
            mustContain: new[] { "Assets/Prefabs/Traps/Level3/PulsePlate.prefab", "3857dd2ec61e66f4ba246095bb02bec2", "carriageRadius", "armVerticalHitRange" },
            mustNotContain: new[] { "runnerHitRadius", "useSineMotion" });
        CheckBuilderText(System.IO.Path.Combine(projectRoot, "Assets", "Editor", "BuildLevel5FractureRelay.cs"), errors,
            mustContain: new[] { "Assets/Prefabs/Traps/Level3/PulsePlate.prefab", "3857dd2ec61e66f4ba246095bb02bec2", "carriageRadius", "armVerticalHitRange" },
            mustNotContain: new[] { "runnerHitRadius", "useSineMotion" });
        return errors;
    }

    private static void CheckBuilderText(string path, List<string> errors, string[] mustContain, string[] mustNotContain)
    {
        if (!File.Exists(path))
        {
            errors.Add("Missing builder source " + path);
            return;
        }

        string source = File.ReadAllText(path);
        if (mustContain != null)
            foreach (string token in mustContain)
                if (!source.Contains(token)) errors.Add(System.IO.Path.GetFileName(path) + " missing contract " + token);
        if (mustNotContain != null)
            foreach (string token in mustNotContain)
                if (source.Contains(token)) errors.Add(System.IO.Path.GetFileName(path) + " contains stale contract " + token);
    }

    private static void ValidateFractures(Scene scene, List<string> errors)
    {
        var segments = new List<Bounds>();
        foreach (var box in Components<BoxCollider>(scene))
        {
            if (!box.enabled || box.isTrigger || !box.gameObject.activeInHierarchy) continue;
            Bounds bounds = WorldBounds(box);
            if (bounds.min.x > 0f || bounds.max.x < 0f || bounds.min.y > 0.01f || bounds.max.y < -0.01f) continue;
            if (bounds.min.z < 308f - 0.001f && bounds.max.z > 304f + 0.001f || bounds.min.z < 390f - 0.001f && bounds.max.z > 386f + 0.001f)
                errors.Add(Path(box.transform) + " bridges intentional fracture");
            segments.Add(bounds);
        }
        foreach (Vector2 interval in new[] { new Vector2(0f, 304f), new Vector2(308f, 386f), new Vector2(390f, 420f) })
            if (!segments.Exists(b => Mathf.Abs(b.min.z - interval.x) < 0.001f && Mathf.Abs(b.max.z - interval.y) < 0.001f)) errors.Add("Missing solid road interval " + interval);
    }

    public static Bounds WorldBounds(Collider collider)
    {
        if (!(collider is BoxCollider box)) return collider.bounds;
        Bounds result = new Bounds(box.transform.TransformPoint(box.center), Vector3.zero);
        for (int i = 0; i < 8; i++) result.Encapsulate(box.transform.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f,
            new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        return result;
    }

    public static List<T> Components<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        foreach (var root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }
    public static GameObject Find(Scene scene, string name)
    {
        GameObject found = null;
        foreach (var transform in Components<Transform>(scene))
            if (transform.name == name)
            {
                if (found != null) throw new InvalidOperationException("Ambiguous scene name: " + name);
                found = transform.gameObject;
            }
        return found;
    }
    public static string Path(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
        return path;
    }

    // Deliberate, scene-scoped migration. Caller controls saving and dirty-state preservation.
    public static int ApplyLevelFixes(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        int changes = 0;
        if (scene.name == "Level1")
        {
            var saw = Find(scene, "Saw Trap 2 Z72");
            if (saw == null) throw new InvalidOperationException("Missing Level1 saw");
            if (Mathf.Abs(saw.transform.position.z - 72f) > 0.001f)
            {
                Undo.RecordObject(saw.transform, "Correct saw collision center");
                Vector3 position = saw.transform.position; position.z = 72f; saw.transform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(saw.transform);
                changes++;
            }
        }
        if (scene.name == "Level2")
            foreach (var cone in Components<ConeHazard>(scene))
            foreach (var collider in cone.GetComponentsInChildren<Collider>(true))
                if (collider.enabled && !collider.isTrigger)
                {
                    Undo.RecordObject(collider, "Disable damage-only cone body"); collider.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(collider); changes++;
                }
        foreach (var finish in Components<FinishGate>(scene)) changes += AuthorFinish(finish);
        if (changes > 0) EditorSceneManager.MarkSceneDirty(scene);
        return changes;
    }

    public static int AuthorFinish(FinishGate finish)
    {
        var serialized = new SerializedObject(finish);
        var reference = serialized.FindProperty("finishVolume");
        var box = reference.objectReferenceValue as BoxCollider;
        // Existing explicitly authored shapes are manual data; never overwrite them.
        if (box != null)
        {
            if (box.name != "Finish Collision Volume") return 0;
            Vector3 position = box.transform.position;
            Vector3 target = new Vector3(finish.transform.position.x, position.y, finish.transform.position.z);
            if ((target - position).sqrMagnitude < 0.000001f) return 0;
            Undo.RecordObject(box.transform, "Align authored finish line");
            box.transform.position = target;
            return 1;
        }
        Scene previousActive = SceneManager.GetActiveScene();
        GameObject owner;
        try
        {
            SceneManager.SetActiveScene(finish.gameObject.scene);
            owner = new GameObject("Finish Collision Volume");
        }
        finally { SceneManager.SetActiveScene(previousActive); }
        Undo.RegisterCreatedObjectUndo(owner, "Author finish volume");
        SceneManager.MoveGameObjectToScene(owner, finish.gameObject.scene);
        owner.transform.SetParent(finish.transform.parent, true);
        owner.transform.SetPositionAndRotation(new Vector3(finish.transform.position.x, 1.4f, finish.transform.position.z), Quaternion.identity);
        Vector3 parentScale = owner.transform.parent != null ? owner.transform.parent.lossyScale : Vector3.one;
        if (Mathf.Abs(parentScale.x) < 0.000001f || Mathf.Abs(parentScale.y) < 0.000001f || Mathf.Abs(parentScale.z) < 0.000001f) throw new InvalidOperationException("Invalid finish parent scale");
        owner.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        box = Undo.AddComponent<BoxCollider>(owner);
        box.isTrigger = true;
        box.size = new Vector3(10f, 3f, 0.2f);
        reference.objectReferenceValue = box;
        serialized.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(finish);
        var legacy = finish.GetComponent<BoxCollider>();
        if (legacy != null) { Undo.RecordObject(legacy, "Disable legacy finish shape"); legacy.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(legacy); }
        return 1;
    }
}
