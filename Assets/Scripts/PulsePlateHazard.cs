using UnityEngine;

/// <summary>
/// A scene-authored floor plate that warns, discharges once, and then recovers.
/// The hazard uses a bounded crowd scan during the discharge window instead of
/// per-runner physics, keeping the behaviour predictable in WebGL.
/// </summary>
public class PulsePlateHazard : MonoBehaviour
{
    [Header("Pulse Timing")]
    [SerializeField, Min(0.1f)] private float cycleDuration = 3.5f;
    [SerializeField, Min(0.05f)] private float chargeDuration = 1.5f;
    [SerializeField, Min(0.05f)] private float dischargeDuration = 0.5f;
    [SerializeField, Min(0f)] private float phaseOffset;

    [Header("Collision")]
    [Tooltip("World-space radius of the rendered charge ring at phase scale 1.0. The runtime applies the 0.7x-2.5x charge multiplier.")]
    [SerializeField, Min(0.05f)] private float chargeRingBaseRadius = 0.5f;
    [Tooltip("World-space radius of the rendered discharge ring at phase scale 1.0. The runtime applies the 2.5x-0.7x discharge multiplier.")]
    [SerializeField, Min(0.05f)] private float dischargeRingBaseRadius = 0.5f;
#pragma warning disable CS0414 // Preserve old scene values without using them as damage limits.
    [SerializeField, HideInInspector] private int maxRunnersPerDischarge = 2;
#pragma warning restore CS0414
    [SerializeField] private float runnerHeight = 1.6f;

    [Header("Presentation")]
    [SerializeField] private Transform chargeRing;
    [SerializeField] private Transform dischargeRing;

    private const float MIN_RING_SCALE = 0.7f;
    private const float MAX_RING_SCALE = 2.5f;

    private PlayerCrowdManager cachedCrowdManager;
    private float cycleClock;

    private void OnValidate()
    {
        cycleDuration = Mathf.Max(cycleDuration, chargeDuration + dischargeDuration + 0.05f);
        chargeDuration = Mathf.Clamp(chargeDuration, 0.05f, cycleDuration - 0.05f);
        dischargeDuration = Mathf.Clamp(dischargeDuration, 0.05f, cycleDuration - chargeDuration);
        chargeRingBaseRadius = Mathf.Clamp(chargeRingBaseRadius, 0.05f, 4f);
        dischargeRingBaseRadius = Mathf.Clamp(dischargeRingBaseRadius, 0.05f, 4f);
        runnerHeight = Mathf.Clamp(runnerHeight, 0.25f, 3f);
    }

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        cycleClock = Mathf.Repeat(phaseOffset, cycleDuration);
        UpdatePresentation();
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        cycleClock += Time.deltaTime;
        if (cycleClock >= cycleDuration)
        {
            cycleClock = Mathf.Repeat(cycleClock, cycleDuration);
        }

        bool inCharge = cycleClock < chargeDuration;
        bool inDischarge = cycleClock >= chargeDuration &&
            cycleClock < chargeDuration + dischargeDuration;

        UpdatePresentation();

        if (inCharge)
        {
            CheckRunnerCollisions(true);
        }

        if (inDischarge)
        {
            CheckRunnerCollisions(false);
        }
    }

    private void CheckRunnerCollisions(bool isChargePhase)
    {
        var runners = cachedCrowdManager.ActiveRunners;
        float radius = GetCurrentRingRadius(isChargePhase);
        float radiusSqr = radius * radius;

        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;

            GameObject runner = runners[i];
            Vector3 delta = runner.transform.position - transform.position;
            bool insideRadius = new Vector2(delta.x, delta.z).sqrMagnitude <= radiusSqr;
            bool insideHeight = Mathf.Abs(delta.y) <= runnerHeight;
            if (!insideRadius || !insideHeight) continue;

            cachedCrowdManager.RemoveRunnerByHazard(
                runner,
                runner.transform.position + Vector3.up * 0.5f,
                GetRunnerColor(runner));
        }
    }

    private float GetCurrentRingRadius(bool isChargePhase)
    {
        if (isChargePhase)
        {
            float charge01 = Mathf.Clamp01(cycleClock / chargeDuration);
            return chargeRingBaseRadius * Mathf.Lerp(MIN_RING_SCALE, MAX_RING_SCALE, charge01);
        }

        float discharge01 = Mathf.Clamp01((cycleClock - chargeDuration) / dischargeDuration);
        return dischargeRingBaseRadius * Mathf.Lerp(MAX_RING_SCALE, MIN_RING_SCALE, discharge01);
    }

    private void UpdatePresentation()
    {
        bool charging = cycleClock < chargeDuration;
        bool discharging = cycleClock >= chargeDuration &&
            cycleClock < chargeDuration + dischargeDuration;

        if (chargeRing != null)
        {
            chargeRing.gameObject.SetActive(charging);
            if (charging)
            {
                float charge01 = Mathf.Clamp01(cycleClock / chargeDuration);
                chargeRing.localScale = Vector3.one * Mathf.Lerp(MIN_RING_SCALE, MAX_RING_SCALE, charge01);
            }
        }

        if (dischargeRing != null)
        {
            dischargeRing.gameObject.SetActive(discharging);
            if (discharging)
            {
                float discharge01 = Mathf.Clamp01((cycleClock - chargeDuration) / dischargeDuration);
                dischargeRing.localScale = Vector3.one * Mathf.Lerp(MAX_RING_SCALE, MIN_RING_SCALE, discharge01);
            }
        }
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
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.3f);
        float maximumRadius = Mathf.Max(chargeRingBaseRadius, dischargeRingBaseRadius) * MAX_RING_SCALE;
        Gizmos.DrawWireSphere(transform.position, maximumRadius);
    }
}
