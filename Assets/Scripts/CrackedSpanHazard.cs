using UnityEngine;

/// <summary>
/// A full-width separated-road crossing. The lead uses CharacterController gravity;
/// visual runners stay lightweight and inherit that shared motion until their own
/// world-space crossing can no longer clear the far lip, then use the crowd
/// manager's pooled falling-runner path.
/// </summary>
public class CrackedSpanHazard : Level4HazardBase
{
    private enum SpanState
    {
        Armed,
        Crossing,
        Failed,
        Passed
    }

    [Header("Cracked Span")]
    [SerializeField, Min(1f)] private float spanLength = 4f;
    [SerializeField, Min(0.25f)] private float requiredClearance = 1.25f;
    [SerializeField, Min(0.5f)] private float stopperWidth = 9.2f;
    [SerializeField, Min(0.1f)] private float stopperHeight = 1.15f;
    [SerializeField, Min(0.05f)] private float stopperDepth = 0.25f;
    [SerializeField, Min(0.5f)] private float warningDistance = 10f;

    [Header("Crossing Mode")]
    [SerializeField] private bool usePhysicalGap = true;
    [SerializeField, Min(0.5f)] private float pitDepth = 2.5f;
    [SerializeField, Min(0f)] private float roadSurfaceOffset;
    [SerializeField, Min(0.01f)] private float farEdgeClearance = 0.15f;
    [SerializeField, Min(0f)] private float runnerLandingTolerance = 0.05f;

    [Header("Failed Crossing Loss")]
    [SerializeField, Min(0)] private int failedLossCap = 10;
    [SerializeField, Range(0f, 1f)] private float failedLossPercent = 0.15f;
    [SerializeField] private Color hazardColor = new Color(0.86f, 0.16f, 0.98f);
    [SerializeField, Min(0)] private int leadStopperLayer;

    private Transform visual;
    private Transform leadStopper;
    private Transform bridge;
    private SpanState state = SpanState.Armed;
    private bool failedLossApplied;
    private bool gapProcessingStarted;
    private bool gapProcessingComplete;
    private bool gapCrowdComplete;
    private bool leadJumpCommitted;
    private bool leadCrossingResolved;
    private int gapSessionId;

    protected override void Awake()
    {
        base.Awake();
        CacheSpanParts();
        ApplyGeometry();
        ResetSpan();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetSpan();
    }

    protected override void Start()
    {
        base.Start();
        CacheSpanParts();
        ApplyGeometry();
        ResetSpan();
    }

    private void LateUpdate()
    {
        if (player == null || crowd == null || !UIManager.IsGameActive) return;

        if (crowd.IsGameOver) return;

        UpdateTelegraph();

        if (usePhysicalGap)
        {
            UpdatePhysicalGap();
        }
        else
        {
            UpdateLegacyGap();
        }
    }

    private void UpdateLegacyGap()
    {
        float relativeZ = player.transform.position.z - transform.position.z;
        float nearEdgeThreshold = -Mathf.Max(0.05f, stopperDepth);
        float crossingPlane = spanLength * 0.5f;

        if (state != SpanState.Passed &&
            relativeZ >= nearEdgeThreshold &&
            relativeZ < crossingPlane &&
            !player.IsAirborne)
        {
            ResolveFailedAttempt();
        }

        if (state != SpanState.Passed && relativeZ >= crossingPlane)
        {
            if (player.IsAirborne && player.HasCleared(requiredClearance, 0f))
            {
                ResolveSuccessfulCrossing();
            }
            else
            {
                ResolveFailedAttempt();
            }
        }
    }

    private void UpdatePhysicalGap()
    {
        Vector3 nearEdge = transform.TransformPoint(Vector3.zero);
        Vector3 farEdge = transform.TransformPoint(Vector3.forward * spanLength);
        float startZ = Mathf.Min(nearEdge.z, farEdge.z);
        float endZ = Mathf.Max(nearEdge.z, farEdge.z);
        float relativeZ = player.transform.position.z - startZ;

        // Start the bounded runner session before the lead reaches the lip so the
        // front and rear positions are captured before the formation spans the gap.
        if (!gapProcessingStarted && player.transform.position.z >= startZ - warningDistance)
        {
            crowd.BeginRoadGap(gapSessionId, startZ, endZ);
            gapProcessingStarted = true;
            state = SpanState.Crossing;
        }

        if (!gapProcessingStarted || gapProcessingComplete) return;

        float crossingPlane = spanLength * 0.5f;
        if (!leadCrossingResolved && relativeZ >= crossingPlane)
        {
            if (player.IsAirborne && player.HasCleared(requiredClearance, 0f))
            {
                // This is only a lead commitment. The crowd keeps being evaluated
                // until every participating visual runner resolves its own crossing.
                leadJumpCommitted = true;
            }
            else
            {
                leadCrossingResolved = true;
                ResolveFailedAttempt();
            }
        }

        if (!leadCrossingResolved && relativeZ >= spanLength)
        {
            if (leadJumpCommitted && IsLeadSafeBeyondFarEdge())
            {
                ResolveSuccessfulCrossing();
            }
            else
            {
                leadCrossingResolved = true;
                ResolveFailedAttempt();
            }
        }

        ProcessGapRunners(startZ, endZ);

        if (crowd.IsLeadFallGameOver)
        {
            if (!leadCrossingResolved)
            {
                leadCrossingResolved = true;
                ResolveFailedAttempt();
            }

            // The lead is terminal and no longer advances the formation. Runners
            // currently over the gap were processed above; near-side runners are
            // not fabricated into additional losses.
            gapCrowdComplete = true;
        }

        if (leadCrossingResolved && gapCrowdComplete)
        {
            crowd.EndRoadGap(gapSessionId);
            gapProcessingComplete = true;
        }
    }

    private bool IsLeadSafeBeyondFarEdge()
    {
        float roadSurfaceY = transform.position.y + roadSurfaceOffset;
        if (player.IsGrounded)
        {
            return player.transform.position.y >= roadSurfaceY - runnerLandingTolerance;
        }

        return player.transform.position.y >= roadSurfaceY + farEdgeClearance;
    }

    public void ResetSpan()
    {
        if (gapProcessingStarted && !gapProcessingComplete && crowd != null)
        {
            crowd.CancelRoadGap(gapSessionId);
        }

        state = SpanState.Armed;
        failedLossApplied = false;
        crossingResolved = false;
        gapProcessingStarted = false;
        gapProcessingComplete = false;
        gapCrowdComplete = false;
        leadJumpCommitted = false;
        leadCrossingResolved = false;
        gapSessionId = GetInstanceID();
        SetStopperActive(!usePhysicalGap);
        if (bridge != null) bridge.gameObject.SetActive(!usePhysicalGap);
        if (telegraph != null) telegraph.gameObject.SetActive(false);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        spanLength = Mathf.Clamp(spanLength, 1f, 12f);
        requiredClearance = Mathf.Clamp(requiredClearance, 0.25f, 3f);
        stopperWidth = Mathf.Clamp(stopperWidth, 0.5f, 9.6f);
        stopperHeight = Mathf.Clamp(stopperHeight, 0.1f, 2f);
        stopperDepth = Mathf.Clamp(stopperDepth, 0.05f, 1f);
        warningDistance = Mathf.Clamp(warningDistance, 0.5f, 30f);
        pitDepth = Mathf.Clamp(pitDepth, 0.5f, 10f);
        roadSurfaceOffset = Mathf.Clamp(roadSurfaceOffset, -2f, 2f);
        farEdgeClearance = Mathf.Clamp(farEdgeClearance, 0.01f, 1f);
        runnerLandingTolerance = Mathf.Clamp(runnerLandingTolerance, 0f, 0.25f);
        failedLossCap = Mathf.Clamp(failedLossCap, 0, 35);
        failedLossPercent = Mathf.Clamp01(failedLossPercent);
        leadStopperLayer = Mathf.Clamp(leadStopperLayer, 0, 31);

        if (!Application.isPlaying)
        {
            CacheSpanParts();
            ApplyGeometry();
        }
    }

    private void CacheSpanParts()
    {
        if (visual == null) visual = transform.Find("Visual");
        if (leadStopper == null) leadStopper = transform.Find("LeadStopper");
        if (bridge == null) bridge = transform.Find("Bridge");
        if (telegraph == null) telegraph = transform.Find("Telegraph");
    }

    private void ApplyGeometry()
    {
        CacheSpanParts();

        if (leadStopper != null)
        {
            leadStopper.localPosition = new Vector3(0f, 0f, -stopperDepth * 0.5f);
            leadStopper.gameObject.layer = leadStopperLayer;

            BoxCollider collider = leadStopper.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = false;
                collider.center = new Vector3(0f, stopperHeight * 0.5f, 0f);
                collider.size = new Vector3(stopperWidth, stopperHeight, stopperDepth);
            }

            leadStopper.gameObject.SetActive(!usePhysicalGap);
        }

        if (bridge != null)
        {
            bridge.localPosition = new Vector3(0f, -0.1f, spanLength * 0.5f);
            BoxCollider collider = bridge.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = false;
                collider.center = Vector3.zero;
                collider.size = new Vector3(stopperWidth, 0.2f, spanLength + 0.1f);
            }

            bridge.gameObject.SetActive(!usePhysicalGap);
        }

        if (visual != null)
        {
            Transform bed = visual.Find("CrackBed");
            if (bed != null)
            {
                bed.localPosition = new Vector3(0f, usePhysicalGap ? -pitDepth : -0.015f, spanLength * 0.5f);
                bed.localScale = new Vector3(stopperWidth, bed.localScale.y, spanLength);
            }

            Transform core = visual.Find("CrackCore");
            if (core != null)
            {
                core.localPosition = new Vector3(0f, usePhysicalGap ? -Mathf.Max(0.05f, pitDepth - 0.15f) : 0.03f, spanLength * 0.5f);
                core.localScale = new Vector3(core.localScale.x, core.localScale.y, Mathf.Max(0.2f, spanLength - 0.3f));
            }

            for (int i = 0; i < 4; i++)
            {
                Transform mark = visual.Find("Crack Mark " + (i + 1));
                if (mark != null)
                {
                    mark.localPosition = new Vector3(mark.localPosition.x, usePhysicalGap ? -Mathf.Max(0.05f, pitDepth - 0.3f) : 0.07f, mark.localPosition.z);
                }
            }

            Transform nearLip = visual.Find("NearLip");
            if (nearLip != null) nearLip.localPosition = new Vector3(0f, nearLip.localPosition.y, 0.05f);

            Transform farLip = visual.Find("FarLip");
            if (farLip != null) farLip.localPosition = new Vector3(0f, farLip.localPosition.y, spanLength - 0.05f);

            Transform leftEdge = visual.Find("LeftEdge");
            if (leftEdge != null)
            {
                leftEdge.localPosition = new Vector3(-stopperWidth * 0.5f, leftEdge.localPosition.y, spanLength * 0.5f);
                leftEdge.localScale = new Vector3(leftEdge.localScale.x, leftEdge.localScale.y, spanLength);
            }

            Transform rightEdge = visual.Find("RightEdge");
            if (rightEdge != null)
            {
                rightEdge.localPosition = new Vector3(stopperWidth * 0.5f, rightEdge.localPosition.y, spanLength * 0.5f);
                rightEdge.localScale = new Vector3(rightEdge.localScale.x, rightEdge.localScale.y, spanLength);
            }
        }
    }

    private void UpdateTelegraph()
    {
        if (telegraph == null) return;

        float distanceToNearEdge = transform.position.z - player.transform.position.z;
        bool show = state != SpanState.Passed &&
            distanceToNearEdge <= warningDistance &&
            distanceToNearEdge >= -spanLength;
        telegraph.gameObject.SetActive(show);
    }

    private void ResolveFailedAttempt()
    {
        state = SpanState.Failed;
        if (failedLossApplied) return;

        failedLossApplied = true;
        if (usePhysicalGap) return;

        int visualLossCap = CalculateVisualLossCap();
        if (visualLossCap > 0)
        {
            RemoveCappedRunners(visualLossCap, transform.position + Vector3.up * 0.5f, hazardColor);
        }
    }

    private int CalculateVisualLossCap()
    {
        if (crowd == null || crowd.ActiveRunnerCount <= 0 || crowd.VisualRunnerCount <= 0)
        {
            return 0;
        }

        int logicalBudget = Mathf.Min(
            failedLossCap,
            Mathf.FloorToInt(crowd.ActiveRunnerCount * failedLossPercent));
        if (logicalBudget <= 0) return 0;

        float logicalPerVisual = crowd.ActiveRunnerCount / Mathf.Max(1f, crowd.VisualRunnerCount);
        int visualBudget = Mathf.FloorToInt(logicalBudget / Mathf.Max(1f, logicalPerVisual));
        return Mathf.Clamp(visualBudget, 1, crowd.VisualRunnerCount);
    }

    private void ResolveSuccessfulCrossing()
    {
        state = SpanState.Passed;
        crossingResolved = true;
        leadCrossingResolved = true;
        SetStopperActive(false);
        if (bridge != null) bridge.gameObject.SetActive(false);
        if (telegraph != null) telegraph.gameObject.SetActive(false);
    }

    private void ProcessGapRunners(float startZ, float endZ)
    {
        if (!usePhysicalGap || crowd == null || spanLength <= 0f || gapCrowdComplete) return;

        crowd.ProcessRoadGap(
            gapSessionId,
            startZ,
            endZ,
            transform.position.y + roadSurfaceOffset,
            farEdgeClearance,
            runnerLandingTolerance);
        gapCrowdComplete = crowd.IsRoadGapComplete(gapSessionId);
    }

    private void SetStopperActive(bool active)
    {
        if (leadStopper != null)
        {
            leadStopper.gameObject.SetActive(active && !usePhysicalGap);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.86f, 0.16f, 0.98f, 0.35f);
        Vector3 center = transform.position + Vector3.forward * (spanLength * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(stopperWidth, 0.05f, spanLength));
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(
            transform.position + Vector3.forward * (-stopperDepth * 0.5f) + Vector3.up * (stopperHeight * 0.5f),
            new Vector3(stopperWidth, stopperHeight, stopperDepth));
    }
}
