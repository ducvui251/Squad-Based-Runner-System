using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A lane-focused overhead hammer that swings through an authored horizontal arc.
/// The visual and collision point share the same deterministic phase.
/// </summary>
public class SwingHammerHazard : MonoBehaviour
{
    [Header("Swing")]
    [SerializeField, Min(0.1f)] private float oscillationDuration = 2.6f;
    [SerializeField, Range(5f, 80f)] private float angleRange = 55f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField, Min(0f)] private float activationDistance = 22f;
    [SerializeField, Min(0.1f)] private float armLength = 2f;

    [Header("Collision")]
    [SerializeField, Min(0.05f)] private float killRadius = 0.8f;
    [SerializeField, Min(0.1f)] private float verticalHitRange = 1.25f;
    [FormerlySerializedAs("maxRunnersPerCycle")]
#pragma warning disable CS0414 // Preserve old scene values without using them as damage limits.
    [SerializeField, HideInInspector] private int maxHeadRunnersPerCycle = 8;
    [SerializeField, HideInInspector] private int maxArmRunnersPerCycle = 3;
#pragma warning restore CS0414
    // The authored arm is 0.25m thick (0.125m half-thickness); add 0.25m
    // deliberate clearance so the collision envelope is 0.375m vertically.
    [SerializeField, Min(0.1f)] private float armVerticalHitRange = 0.375f;
    [Tooltip("Authored vertical body height used when testing the hammer handle against a runner.")]
    [SerializeField, Min(0.1f)] private float runnerHeight = 1.6f;

    [Header("Presentation")]
    [SerializeField] private Transform armVisual;
    [SerializeField] private Transform hammerHead;
    [SerializeField] private BoxCollider armHitVolume;

    private PlayerCrowdManager cachedCrowdManager;
    private float swingClock;

    private void OnValidate()
    {
        oscillationDuration = Mathf.Max(0.1f, oscillationDuration);
        angleRange = Mathf.Clamp(angleRange, 5f, 80f);
        activationDistance = Mathf.Clamp(activationDistance, 2f, 50f);
        armLength = Mathf.Clamp(armLength, 0.5f, 4f);
        killRadius = Mathf.Clamp(killRadius, 0.05f, 1.5f);
        verticalHitRange = Mathf.Clamp(verticalHitRange, 0.1f, 2.5f);
        armVerticalHitRange = Mathf.Clamp(armVerticalHitRange, 0.1f, 3f);
        runnerHeight = Mathf.Clamp(runnerHeight, 0.1f, 3f);
        if (armHitVolume != null) armHitVolume.isTrigger = true;
    }

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        swingClock = Mathf.Repeat(phaseOffset, oscillationDuration);
        UpdateHammerVisual();
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        if (cachedCrowdManager.transform.position.z < transform.position.z - activationDistance) return;

        swingClock += Time.deltaTime;
        UpdateHammerVisual();
        CheckRunnerCollisions();
    }

    private float CurrentAngle()
    {
        return Mathf.Sin((swingClock / oscillationDuration) * Mathf.PI * 2f) * angleRange;
    }

    private Vector3 CurrentHeadPosition()
    {
        float angleRadians = CurrentAngle() * Mathf.Deg2Rad;
        return transform.position + new Vector3(Mathf.Sin(angleRadians) * armLength, -2.2f, 0f);
    }

    private void UpdateHammerVisual()
    {
        float angle = CurrentAngle();
        if (armVisual != null)
        {
            armVisual.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        if (hammerHead != null)
        {
            hammerHead.position = CurrentHeadPosition();
        }
    }

    private void CheckRunnerCollisions()
    {
        CheckHeadCollisions();
        CheckArmCollisions();
    }

    private void CheckHeadCollisions()
    {
        Vector3 hitCenter = hammerHead != null ? hammerHead.position : CurrentHeadPosition();
        var runners = cachedCrowdManager.ActiveRunners;
        float radiusSqr = killRadius * killRadius;

        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;

            GameObject runner = runners[i];
            Vector3 delta = runner.transform.position - hitCenter;
            bool insideRadius = new Vector2(delta.x, delta.z).sqrMagnitude <= radiusSqr;
            bool insideHeight = Mathf.Abs(delta.y) <= verticalHitRange;
            if (!insideRadius || !insideHeight) continue;

            cachedCrowdManager.RemoveRunnerByHazard(
                runner,
                runner.transform.position + Vector3.up * 0.5f,
                GetRunnerColor(runner));
        }
    }

    private void CheckArmCollisions()
    {
        Transform collisionTransform = armHitVolume != null ? armHitVolume.transform : armVisual;
        Vector3 armCenter = collisionTransform != null
            ? collisionTransform.position
            : transform.position + Vector3.down * 1.1f;
        Vector3 armDirection = collisionTransform != null
            ? collisionTransform.right
            : Quaternion.Euler(0f, CurrentAngle(), 0f) * Vector3.right;
        float collisionHalfLength = armLength;
        float collisionHalfHeight = armVerticalHitRange;
        if (armHitVolume != null)
        {
            Vector3 scale = armHitVolume.transform.lossyScale;
            collisionHalfLength = Mathf.Max(0.05f, armHitVolume.size.x * Mathf.Abs(scale.x) * 0.5f);
            collisionHalfHeight = Mathf.Max(collisionHalfHeight,
                armHitVolume.size.y * Mathf.Abs(scale.y) * 0.5f);
        }
        Vector3 armStart = armCenter - armDirection * collisionHalfLength;
        Vector3 armEnd = armCenter + armDirection * collisionHalfLength;
        var runners = cachedCrowdManager.ActiveRunners;
        float radiusSqr = killRadius * killRadius;

        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;

            GameObject runner = runners[i];
            Vector3 runnerPosition = runner.transform.position;
            bool insideArm = DistanceToSegmentSqrXZ(runnerPosition, armStart, armEnd) <= radiusSqr;
            bool insideHeight = VerticalRangesOverlap(
                runnerPosition.y, runnerPosition.y + runnerHeight,
                armCenter.y - collisionHalfHeight, armCenter.y + collisionHalfHeight);
            if (!insideArm || !insideHeight) continue;

            cachedCrowdManager.RemoveRunnerByHazard(
                runner,
                runnerPosition + Vector3.up * 0.5f,
                GetRunnerColor(runner));
        }
    }

    private static float DistanceToSegmentSqrXZ(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector2 pointXZ = new Vector2(point.x, point.z);
        Vector2 startXZ = new Vector2(start.x, start.z);
        Vector2 segment = new Vector2(end.x - start.x, end.z - start.z);
        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= Mathf.Epsilon) return (pointXZ - startXZ).sqrMagnitude;

        float t = Mathf.Clamp01(Vector2.Dot(pointXZ - startXZ, segment) / segmentLengthSqr);
        Vector2 closest = startXZ + segment * t;
        return (pointXZ - closest).sqrMagnitude;
    }

    private static bool VerticalRangesOverlap(float minA, float maxA, float minB, float maxB)
    {
        return maxA >= minB && minA <= maxB;
    }

    private static Color GetRunnerColor(GameObject runner)
    {
        Renderer renderer = runner != null ? runner.GetComponentInChildren<Renderer>() : null;
        if (renderer != null && renderer.sharedMaterial != null)
        {
            if (renderer.sharedMaterial.HasProperty("_Color")) return renderer.sharedMaterial.color;
            if (renderer.sharedMaterial.HasProperty("_BaseColor")) return renderer.sharedMaterial.GetColor("_BaseColor");
        }

        return Color.blue;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(CurrentHeadPosition(), killRadius);
        Gizmos.DrawLine(transform.position, CurrentHeadPosition());

        Vector3 armCenter = armVisual != null ? armVisual.position : transform.position + Vector3.down * 1.1f;
        Vector3 armDirection = armVisual != null
            ? armVisual.right
            : Quaternion.Euler(0f, CurrentAngle(), 0f) * Vector3.right;
        Gizmos.DrawLine(armCenter - armDirection * armLength, armCenter + armDirection * armLength);
    }
}
