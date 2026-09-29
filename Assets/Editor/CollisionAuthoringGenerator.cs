using System;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CollisionAuthoringGenerator
{
    public const int Version = 1;

    [MenuItem("SpiralSquad/Collision/Analyze Selected Profile")]
    private static void AnalyzeSelected() => Debug.Log(Run(Selected(), false));

    [MenuItem("SpiralSquad/Collision/Apply Selected Profile")]
    private static void ApplySelected() => Debug.Log(Run(Selected(), true));

    private static CollisionAuthoringProfile Selected()
    {
        if (Selection.activeObject is CollisionAuthoringProfile profile) return profile;
        throw new InvalidOperationException("Select an explicit CollisionAuthoringProfile asset.");
    }

    public static string Run(CollisionAuthoringProfile profile, bool apply)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        if (profile == null || !AssetDatabase.Contains(profile)) throw new ArgumentException("A saved profile is required.");
        if (profile.entries == null) throw new ArgumentException("Collision authoring profile has no entries array.");
        // Validate every entry before any mutation.
        for (int i = 0; i < profile.entries.Length; i++) Process(profile, i, false);
        var report = new StringBuilder(apply ? "APPLY\n" : "DRY RUN\n");
        for (int i = 0; i < profile.entries.Length; i++) report.AppendLine(Process(profile, i, apply));
        return report.ToString();
    }

    private static string Process(CollisionAuthoringProfile profile, int index, bool apply)
    {
        var entry = profile.entries[index];
        if (entry == null) throw new ArgumentException("Null profile entry.");
        if (entry.manualOverride)
        {
            if (string.IsNullOrWhiteSpace(entry.exceptionReason)) throw new ArgumentException("Manual override needs a reason.");
            return index + ": MANUAL " + entry.exceptionReason;
        }
        if (entry.role == CollisionAuthoringProfile.Role.Unspecified) throw new ArgumentException("Unspecified collision role.");
        string path = AssetDatabase.GetAssetPath(entry.prefab);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only explicitly selected .prefab assets can be generated.");
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var owner = string.IsNullOrEmpty(entry.targetPath) ? root.transform : root.transform.Find(entry.targetPath);
            if (owner == null) throw new ArgumentException(path + ": missing target " + entry.targetPath);
            if (entry.role == CollisionAuthoringProfile.Role.Decoration) return path + ": decoration, no shape generated";
            Vector3 center = entry.center;
            Vector3 size = entry.size;
            float radius = entry.radius;
            float height = entry.height;
            if (entry.fitMeshes)
            {
                if (entry.role == CollisionAuthoringProfile.Role.ScriptedDamage)
                    throw new ArgumentException("Script consumers require explicit values in their documented units; automatic fitting is not supported.");
                Bounds bounds = FitMeshBounds(owner, entry.meshPaths);
                center = bounds.center;
                size = bounds.size;
                radius = entry.shape == CollisionAuthoringProfile.Shape.Sphere ? size.magnitude * 0.5f : Mathf.Max(size.x, size.z) * 0.5f;
                height = Mathf.Max(size.y, radius * 2f);
            }
            if (!Finite(center) || !Positive(size) || !Positive(radius) || !Positive(height) || entry.layer < 0 || entry.layer > 31)
                throw new ArgumentException(path + ": non-finite/zero dimensions or invalid layer.");
            if (entry.shape == CollisionAuthoringProfile.Shape.Capsule && height < radius * 2f)
                throw new ArgumentException("Capsule height must be at least its diameter.");
            bool changed;
            string detail;
            if (entry.role == CollisionAuthoringProfile.Role.ScriptedDamage)
            {
                if (entry.fitMeshes || string.IsNullOrEmpty(entry.consumerType)) throw new ArgumentException("Explicit scripted consumer required.");
                var consumer = owner.GetComponent(entry.consumerType);
                if (consumer == null) throw new ArgumentException(path + ": missing consumer " + entry.consumerType);
                if (string.IsNullOrEmpty(entry.centerProperty) && string.IsNullOrEmpty(entry.sizeProperty) && string.IsNullOrEmpty(entry.radiusProperty))
                    throw new ArgumentException("No serialized shape property is consumed.");
                var serialized = new SerializedObject(consumer);
                changed = Set(serialized, entry.centerProperty, center, apply)
                    | Set(serialized, entry.sizeProperty, size, apply)
                    | Set(serialized, entry.radiusProperty, radius, apply);
                if (apply && changed) serialized.ApplyModifiedPropertiesWithoutUndo();
                detail = "script=" + entry.consumerType + " center=" + center + " size=" + size + " radius=" + radius;
            }
            else
            {
                string generatedName = "Collision_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)) + "_" + index;
                var existing = owner.Find(generatedName);
                Type type = entry.shape == CollisionAuthoringProfile.Shape.Box ? typeof(BoxCollider)
                    : entry.shape == CollisionAuthoringProfile.Shape.Sphere ? typeof(SphereCollider) : typeof(CapsuleCollider);
                if (existing != null && (existing.GetComponent(type) == null || existing.GetComponents<Component>().Length != 2 || existing.childCount != 0))
                    throw new InvalidOperationException("Generated owner was manually modified; mark this entry manual before continuing.");
                bool trigger = entry.role == CollisionAuthoringProfile.Role.Trigger;
                Collider collider = existing != null ? existing.GetComponent<Collider>() : null;
                string before = collider == null ? "absent" : ShapeText(collider);
                string after = type.Name + " center=" + center + " size=" + size + " radius=" + radius + " height=" + height;
                changed = collider == null || !collider.enabled || collider.isTrigger != trigger || collider.gameObject.layer != entry.layer
                    || existing.localPosition != Vector3.zero || existing.localRotation != Quaternion.identity || existing.localScale != Vector3.one;
                if (collider is BoxCollider box) changed |= box.center != center || box.size != size;
                if (collider is SphereCollider sphere) changed |= sphere.center != center || sphere.radius != radius;
                if (collider is CapsuleCollider capsule) changed |= capsule.center != center || capsule.radius != radius || capsule.height != height || capsule.direction != 1;
                if (apply && changed)
                {
                    if (existing == null)
                    {
                        existing = new GameObject(generatedName).transform;
                        existing.SetParent(owner, false);
                        collider = (Collider)existing.gameObject.AddComponent(type);
                    }
                    existing.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    existing.localScale = Vector3.one;
                    collider.enabled = true;
                    collider.isTrigger = trigger;
                    collider.gameObject.layer = entry.layer;
                    if (collider is BoxCollider b) { b.center = center; b.size = size; }
                    if (collider is SphereCollider s) { s.center = center; s.radius = radius; }
                    if (collider is CapsuleCollider c) { c.center = center; c.radius = radius; c.height = height; c.direction = 1; }
                }
                detail = before + " -> " + after;
            }
            if (apply && changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            if (apply && entry.generationVersion != Version)
            {
                entry.generationVersion = Version;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            return path + "/" + entry.targetPath + ": " + (changed ? "CHANGE " : "UNCHANGED ") + detail;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static Bounds FitMeshBounds(Transform owner, string[] paths)
    {
        if (paths == null || paths.Length == 0) throw new ArgumentException("Choose mesh paths explicitly.");
        bool found = false;
        Bounds result = default;
        foreach (string path in paths)
        {
            var source = string.IsNullOrEmpty(path) ? owner : owner.Find(path);
            if (source == null) throw new ArgumentException("Missing mesh path " + path);
            var filter = source.GetComponent<MeshFilter>();
            var skin = source.GetComponent<SkinnedMeshRenderer>();
            if (filter == null && skin == null) throw new ArgumentException("No mesh at " + path);
            if (filter != null && filter.sharedMesh == null) throw new ArgumentException("Missing mesh at " + path);
            Bounds local = filter != null ? filter.sharedMesh.bounds : skin.localBounds;
            Matrix4x4 matrix = owner.worldToLocalMatrix * source.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 p = local.center + Vector3.Scale(local.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                p = matrix.MultiplyPoint3x4(p);
                if (!found) { result = new Bounds(p, Vector3.zero); found = true; }
                else result.Encapsulate(p);
            }
        }
        return result;
    }

    private static bool Set(SerializedObject owner, string name, Vector3 value, bool apply)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var property = owner.FindProperty(name);
        if (property == null || property.propertyType != SerializedPropertyType.Vector3) throw new ArgumentException("Unknown Vector3 property: " + name);
        bool changed = property.vector3Value != value;
        if (apply) property.vector3Value = value;
        return changed;
    }
    private static bool Set(SerializedObject owner, string name, float value, bool apply)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var property = owner.FindProperty(name);
        if (property == null || property.propertyType != SerializedPropertyType.Float) throw new ArgumentException("Unknown float property: " + name);
        bool changed = property.floatValue != value;
        if (apply) property.floatValue = value;
        return changed;
    }
    public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    public static bool Positive(float v) => Finite(v) && v > 0f;
    public static bool Positive(Vector3 v) => Positive(v.x) && Positive(v.y) && Positive(v.z);
    public static string ShapeText(Collider c)
    {
        if (c is BoxCollider b) return "Box center=" + b.center + " size=" + b.size;
        if (c is SphereCollider s) return "Sphere center=" + s.center + " radius=" + s.radius;
        if (c is CapsuleCollider p) return "Capsule center=" + p.center + " radius=" + p.radius + " height=" + p.height;
        return c.GetType().Name;
    }
}
