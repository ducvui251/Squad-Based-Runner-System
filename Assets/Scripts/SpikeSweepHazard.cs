using UnityEngine;

/// <summary>
/// A predictable horizontal spike shuttle. The carriage travels from the start
/// endpoint to the end endpoint, pauses, then travels back to the original
/// endpoint before repeating. Each leg is interpolated over exactly
/// travelDuration seconds, independent of frame rate.
/// </summary>
public class SpikeSweepHazard : MonoBehaviour
{
    private enum MotionPhase { Dormant, AtStart, MovingToEnd, AtEnd, MovingToStart }

    [Header("Endpoints")]
    [SerializeField] private float startX = -3.8f;
    [SerializeField] private float endX = 3.8f;
    [SerializeField, Min(0.1f)] private float travelDuration = 3f;
    [SerializeField, Min(0f)] private float endpointPause = 0.75f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField, Min(0f)] private float approachDistance = 24f;

    [Header("Collision")]
    [SerializeField, Min(0.05f)] private float killRadius = 0.65f;
    [SerializeField, Range(0f, 0.75f)] private float runnerCollisionPadding = 0.25f;
    [SerializeField, Min(0.1f)] private float verticalHitRange = 0.8f;
    [SerializeField, Min(1)] private int maxRunnersPerLeg = 4;

    [Header("Presentation")]
    [SerializeField] private Transform carriageVisual;

    private PlayerCrowdManager cachedCrowdManager;
    private MotionPhase motionPhase = MotionPhase.Dormant;
    private MotionPhase previousMotionPhase = MotionPhase.Dormant;
    private float cycleClock;
    private float cycleDuration;
    private int legLosses;
    private Renderer[] carriageRenderers;
    private float cachedCollisionRadius;

    private void OnValidate()
    {
        startX = Mathf.Clamp(startX, -4.5f, 4.5f);
        endX = Mathf.Clamp(endX, -4.5f, 4.5f);
        if (Mathf.Abs(endX - startX) < 0.5f) endX = startX + (startX <= 0f ? 0.5f : -0.5f);
        travelDuration = Mathf.Max(0.1f, travelDuration);
        endpointPause = Mathf.Clamp(endpointPause, 0f, 3f);
        approachDistance = Mathf.Clamp(approachDistance, 1f, 50f);
        killRadius = Mathf.Clamp(killRadius, 0.05f, 1.5f);
        runnerCollisionPadding = Mathf.Clamp(runnerCollisionPadding, 0f, 0.75f);
        verticalHitRange = Mathf.Clamp(verticalHitRange, 0.1f, 2f);
        maxRunnersPerLeg = Mathf.Clamp(maxRunnersPerLeg, 1, 12);
    }

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        cycleDuration = endpointPause + travelDuration + endpointPause + travelDuration;
        cycleClock = 0f;
        SetCarriageX(startX);
        carriageRenderers = carriageVisual != null
            ? carriageVisual.GetComponentsInChildren<Renderer>(true)
            : new Renderer[0];
        cachedCollisionRadius = CalculateCollisionRadius();
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        if (motionPhase == MotionPhase.Dormant)
        {
            float approachZ = transform.position.z - approachDistance;
            if (cachedCrowdManager.transform.position.z < approachZ) return;

            cycleClock = Mathf.Repeat(phaseOffset, cycleDuration);
            motionPhase = MotionPhase.AtStart;
            previousMotionPhase = MotionPhase.Dormant;
            legLosses = 0;
        }

        cycleClock += Time.deltaTime;
        if (cycleClock >= cycleDuration)
        {
            cycleClock = Mathf.Repeat(cycleClock, cycleDuration);
        }

        Vector3 previousHitCenter = GetHitCenter();
        UpdateMotionPhase();
        if (motionPhase != previousMotionPhase)
        {
            if (motionPhase == MotionPhase.MovingToEnd || motionPhase == MotionPhase.MovingToStart)
            {
                legLosses = 0;
            }
            previousMotionPhase = motionPhase;
        }

        if (motionPhase != MotionPhase.Dormant)
        {
            CheckRunnerCollisions(previousHitCenter, GetHitCenter());
        }
    }

    private void UpdateMotionPhase()
    {
        float firstPauseEnd = endpointPause;
        float outboundEnd = firstPauseEnd + travelDuration;
        float inboundStart = outboundEnd + endpointPause;

        if (cycleClock < firstPauseEnd)
        {
            motionPhase = MotionPhase.AtStart;
            SetCarriageX(startX);
            return;
        }

        if (cycleClock < outboundEnd)
        {
            motionPhase = MotionPhase.MovingToEnd;
            SetCarriageX(GetLegPosition(startX, endX, cycleClock - firstPauseEnd));
            return;
        }

        if (cycleClock < inboundStart)
        {
            motionPhase = MotionPhase.AtEnd;
            SetCarriageX(endX);
            return;
        }

        motionPhase = MotionPhase.MovingToStart;
        SetCarriageX(GetLegPosition(endX, startX, cycleClock - inboundStart));
    }

    private float GetLegPosition(float legStart, float legEnd, float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / travelDuration);
        return Mathf.Lerp(legStart, legEnd, t);
    }

    private void SetCarriageX(float x)
    {
        if (carriageVisual != null)
        {
            Vector3 localPosition = carriageVisual.localPosition;
            carriageVisual.localPosition = new Vector3(x, localPosition.y, localPosition.z);
            return;
        }

        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    private Vector3 GetHitCenter()
    {
        return carriageVisual != null ? carriageVisual.position : transform.position;
    }

    private float CalculateCollisionRadius()
    {
        float radius = killRadius;
        if (carriageVisual == null || carriageRenderers == null) return radius + runnerCollisionPadding;

        Vector3 carriagePosition = carriageVisual.position;
        for (int i = 0; i < carriageRenderers.Length; i++)
        {
            Renderer renderer = carriageRenderers[i];
            if (renderer == null || !renderer.enabled) continue;

            Bounds bounds = renderer.bounds;
            float xRadius = Mathf.Abs(bounds.center.x - carriagePosition.x) + bounds.extents.x;
            float zRadius = Mathf.Abs(bounds.center.z - carriagePosition.z) + bounds.extents.z;
            radius = Mathf.Max(radius, Mathf.Max(xRadius, zRadius));
        }

        return radius + runnerCollisionPadding;
    }

    private void CheckRunnerCollisions(Vector3 previousHitCenter, Vector3 currentHitCenter)
    {
        if (legLosses >= maxRunnersPerLeg) return;

        var runners = cachedCrowdManager.ActiveRunners;
        float radius = cachedCollisionRadius > 0f ? cachedCollisionRadius : CalculateCollisionRadius();
        float radiusSqr = radius * radius;

        for (int i = runners.Count - 1; i >= 0 && legLosses < maxRunnersPerLeg; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;

            GameObject runner = runners[i];
            Vector3 runnerPosition = runner.transform.position;
            float distanceSqr = DistanceToSegmentSqrXZ(runnerPosition, previousHitCenter, currentHitCenter);
            bool insideRadius = distanceSqr <= radiusSqr;
            bool insideHeight = Mathf.Abs(runnerPosition.y - currentHitCenter.y) <= verticalHitRange;
            if (!insideRadius || !insideHeight) continue;

            bool removed = cachedCrowdManager.RemoveRunnerByHazard(
                runner,
                runner.transform.position + Vector3.up * 0.5f,
                GetRunnerColor(runner));
            if (removed) legLosses++;
        }
    }

    private static float DistanceToSegmentSqrXZ(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        float segmentX = segmentEnd.x - segmentStart.x;
        float segmentZ = segmentEnd.z - segmentStart.z;
        float segmentLengthSqr = segmentX * segmentX + segmentZ * segmentZ;
        if (segmentLengthSqr <= Mathf.Epsilon)
        {
            float pointX = point.x - segmentStart.x;
            float pointZ = point.z - segmentStart.z;
            return pointX * pointX + pointZ * pointZ;
        }

        float pointFromStartX = point.x - segmentStart.x;
        float pointFromStartZ = point.z - segmentStart.z;
        float t = Mathf.Clamp01((pointFromStartX * segmentX + pointFromStartZ * segmentZ) / segmentLengthSqr);
        float closestX = segmentStart.x + segmentX * t;
        float closestZ = segmentStart.z + segmentZ * t;
        float deltaX = point.x - closestX;
        float deltaZ = point.z - closestZ;
        return deltaX * deltaX + deltaZ * deltaZ;
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
        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.3f);
        Gizmos.DrawLine(new Vector3(startX, transform.position.y, transform.position.z),
            new Vector3(endX, transform.position.y, transform.position.z));
        Gizmos.DrawWireSphere(new Vector3(startX, transform.position.y, transform.position.z), killRadius);
        Gizmos.DrawWireSphere(new Vector3(endX, transform.position.y, transform.position.z), killRadius);
    }
}
