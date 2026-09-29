using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit Edit Mode checks; never calls gameplay lifecycle methods or simulates physics.
public static class CollisionRegressionChecks
{
    [MenuItem("SpiralSquad/Collision/Run Pure Geometry Checks")]
    private static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (passed, name) => { if (!passed) throw new InvalidOperationException("Collision regression: " + name); checks++; };
        Vector3 from = new Vector3(0, 0, -2), to = new Vector3(0, 0, 2);
        Vector3 center = new Vector3(0, 1.4f, 0), size = new Vector3(10, 3, 0.2f);
        check(FinishGate.IsFinishCrossing(true, true, false, false, from, to, center, size), "finish skips neither endpoint-outside crossing");
        check(!FinishGate.IsFinishCrossing(false, true, false, false, from, to, center, size), "inactive run");
        check(!FinishGate.IsFinishCrossing(true, false, false, false, from, to, center, size), "empty crowd");
        check(!FinishGate.IsFinishCrossing(true, true, true, false, from, to, center, size), "terminal run");
        check(!FinishGate.IsFinishCrossing(true, true, false, true, from, to, center, size), "lead fall terminal");
        check(!FinishGate.IsFinishCrossing(true, true, false, false, to, from, center, size), "backward crossing");
        check(!FinishGate.IsFinishCrossing(true, true, false, false, from + Vector3.right * 5.01f, to + Vector3.right * 5.01f, center, size), "outside finish width");
        check(!FinishGate.IsFinishCrossing(true, true, false, false, from + Vector3.down, to + Vector3.down, center, size), "below finish");
        check(FinishGate.IsFinishCrossing(true, true, false, false, from + Vector3.up * 2, to + Vector3.up * 2, center, size), "airborne finish");
        check(FinishGate.IsFinishCrossing(true, true, false, false, from + Vector3.right * 5, to + Vector3.right * 5, center, size), "finish boundary");
        check(!FinishGate.IsFinishCrossing(true, true, false, false, from, to, center, Vector3.zero), "invalid finish shape");
        check(!FinishGate.IsFinishCrossing(true, true, false, false, new Vector3(float.NaN, 0, -1), to, center, size), "nonfinite finish endpoint");
        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(17, 4, 28), Quaternion.Euler(12, 37, 8), new Vector3(2, 0.7f, 3));
        check(FinishGate.IsFinishCrossing(true, true, false, false, matrix.inverse.MultiplyPoint3x4(matrix.MultiplyPoint3x4(from)), matrix.inverse.MultiplyPoint3x4(matrix.MultiplyPoint3x4(to)), center, size), "rotated nonuniform finish transform");
        check(TrackingProjectile.TrySweptHit(Vector3.left * 0.7f, Vector3.right * 0.7f, 0.6f, out float t) && Mathf.Abs(t - 1f / 14f) < 0.0001f, "projectile earliest entry, endpoints outside");
        check(!TrackingProjectile.TrySweptHit(new Vector3(-2, 1, 0), new Vector3(2, 1, 0), 0.6f, out _), "projectile near miss");
        check(TrackingProjectile.TrySweptHit(new Vector3(-2, 0.6f, 0), new Vector3(2, 0.6f, 0), 0.6f, out t) && Mathf.Abs(t - 0.5f) < 0.001f, "projectile tangent");
        check(TrackingProjectile.TrySweptHit(Vector3.zero, Vector3.zero, 0.6f, out t) && t == 0f, "projectile initially overlapping");
        check(!TrackingProjectile.TrySweptHit(Vector3.one, Vector3.one, 0.6f, out _), "stationary miss");
        // Stationary projectile, runner crosses from -1 to +1: relative motion is +1 to -1.
        check(TrackingProjectile.TrySweptHit(Vector3.right, Vector3.left, 0.6f, out t) && Mathf.Abs(t - 0.2f) < 0.0001f, "moving runner relative sweep");
        check(TrackingProjectile.TrySweptHit(new Vector3(-3, 0, 0), new Vector3(3, 0, 0), 0.6f, out float early)
            && TrackingProjectile.TrySweptHit(new Vector3(-4, 0, 0), new Vector3(2, 0, 0), 0.6f, out float late) && early < late, "earliest candidate order");
        check(!TrackingProjectile.TrySweptHit(Vector3.left, Vector3.right, float.NaN, out _), "invalid projectile radius");
        Vector3 sawCenter = new Vector3(0, 0.55f, 0);
        Vector3 bodyPoint = CircularSaw.ClosestPointOnRunnerVerticalSegment(sawCenter, new Vector3(0.75f, 0, 0), 1.6f);
        check((sawCenter - bodyPoint).sqrMagnitude <= 0.8f * 0.8f, "saw touches runner body at disc edge");
        bodyPoint = CircularSaw.ClosestPointOnRunnerVerticalSegment(sawCenter, new Vector3(0.81f, 0, 0), 1.6f);
        check((sawCenter - bodyPoint).sqrMagnitude > 0.8f * 0.8f, "saw does not enlarge authored horizontal radius");
        bodyPoint = CircularSaw.ClosestPointOnRunnerVerticalSegment(sawCenter, new Vector3(0, 1.4f, 0), 1.6f);
        check((sawCenter - bodyPoint).sqrMagnitude > 0.8f * 0.8f, "saw airborne clearance");
        return "PASS " + checks + " pure collision geometry/guard checks; no Play Mode or physics simulation.";
    }

    public static string RunAuthoring()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
        string prefix = "Assets/Editor/CollisionFixture_" + Guid.NewGuid().ToString("N");
        string prefabPath = prefix + ".prefab", profilePath = prefix + ".asset";
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("CollisionFixture");
            SceneManager.MoveGameObjectToScene(root, preview);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(visual, preview);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.localPosition = new Vector3(1, 2, 3);
            visual.transform.localRotation = Quaternion.Euler(0, 45, 0);
            visual.transform.localScale = new Vector3(2, 1, 4);
            Bounds fit = CollisionAuthoringGenerator.FitMeshBounds(root.transform, new[] { "Visual" });
            if ((fit.center - new Vector3(1, 2, 3)).sqrMagnitude > 0.000001f || Mathf.Abs(fit.size.x - 3f * Mathf.Sqrt(2)) > 0.0001f)
                throw new InvalidOperationException("Rotated mesh corner fit failed.");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            var profile = ScriptableObject.CreateInstance<CollisionAuthoringProfile>();
            profile.entries = new[] { new CollisionAuthoringProfile.Entry { prefab = prefab, role = CollisionAuthoringProfile.Role.Support, fitMeshes = true, meshPaths = new[] { "Visual" } } };
            AssetDatabase.CreateAsset(profile, profilePath);
            byte[] before = File.ReadAllBytes(prefabPath);
            CollisionAuthoringGenerator.Run(profile, false);
            AssertEqual(before, File.ReadAllBytes(prefabPath), "dry run changed prefab");
            CollisionAuthoringGenerator.Run(profile, true);
            byte[] first = File.ReadAllBytes(prefabPath);
            string second = CollisionAuthoringGenerator.Run(profile, true);
            AssertEqual(first, File.ReadAllBytes(prefabPath), "second apply changed prefab");
            if (!second.Contains("UNCHANGED")) throw new InvalidOperationException("second apply did not report unchanged");
            profile.entries[0].manualOverride = true;
            profile.entries[0].exceptionReason = "Regression fixture: preserve manually approved shape";
            profile.entries[0].size = Vector3.one * 20;
            CollisionAuthoringGenerator.Run(profile, true);
            AssertEqual(first, File.ReadAllBytes(prefabPath), "manual override changed prefab");
            return "PASS rotated mesh fit, dry-run purity, byte-identical second apply, and manual override preservation.";
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
            // Only these uniquely named fixtures were created by this check.
            if (File.Exists(prefabPath)) AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(profilePath)) AssetDatabase.DeleteAsset(profilePath);
        }
    }

    private static void AssertEqual(byte[] a, byte[] b, string message)
    {
        if (a.Length != b.Length) throw new InvalidOperationException(message);
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) throw new InvalidOperationException(message);
    }
}
