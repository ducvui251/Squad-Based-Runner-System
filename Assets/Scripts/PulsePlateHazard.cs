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
    [SerializeField, Min(0.05f)] private float killRadius = 1.05f;
    [SerializeField, Min(1)] private int maxRunnersPerDischarge = 2;
    [SerializeField] private float runnerHeight = 1.6f;

    [Header("Presentation")]
    [SerializeField] private Transform chargeRing;
    [SerializeField] private Transform dischargeRing;

    private PlayerCrowdManager cachedCrowdManager;
    private Renderer[] chargeRenderers;
    private Renderer[] dischargeRenderers;
    private float cycleClock;
    private int chargeLosses;
    private int dischargeLosses;

    private void OnValidate()
    {
        cycleDuration = Mathf.Max(cycleDuration, chargeDuration + dischargeDuration + 0.05f);
        chargeDuration = Mathf.Clamp(chargeDuration, 0.05f, cycleDuration - 0.05f);
        dischargeDuration = Mathf.Clamp(dischargeDuration, 0.05f, cycleDuration - chargeDuration);
        killRadius = Mathf.Clamp(killRadius, 0.05f, 2f);
        maxRunnersPerDischarge = Mathf.Clamp(maxRunnersPerDischarge, 1, 8);
        runnerHeight = Mathf.Clamp(runnerHeight, 0.25f, 3f);
    }

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        chargeRenderers = chargeRing != null
            ? chargeRing.GetComponentsInChildren<Renderer>(true)
            : new Renderer[0];
        dischargeRenderers = dischargeRing != null
            ? dischargeRing.GetComponentsInChildren<Renderer>(true)
            : new Renderer[0];
        cycleClock = Mathf.Repeat(phaseOffset, cycleDuration);
        chargeLosses = 0;
        dischargeLosses = 0;
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

        float previousClock = cycleClock;
        cycleClock += Time.deltaTime;
        if (cycleClock >= cycleDuration)
        {
            cycleClock = Mathf.Repeat(cycleClock, cycleDuration);
            chargeLosses = 0;
            dischargeLosses = 0;
        }

        if (previousClock > cycleClock)
        {
            chargeLosses = 0;
            dischargeLosses = 0;
        }

        bool inCharge = cycleClock < chargeDuration;
        bool inDischarge = cycleClock >= chargeDuration &&
            cycleClock < chargeDuration + dischargeDuration;

        UpdatePresentation();

        if (inCharge && chargeLosses < maxRunnersPerDischarge)
        {
            CheckRunnerCollisions(chargeRing, chargeRenderers, true);
        }

        if (inDischarge && dischargeLosses < maxRunnersPerDischarge)
        {
            CheckRunnerCollisions(dischargeRing, dischargeRenderers, false);
        }
    }

    private void CheckRunnerCollisions(Transform activeRing, Renderer[] activeRenderers, bool isChargePhase)
    {
        var runners = cachedCrowdManager.ActiveRunners;
        int losses = 0;
        float radius = GetCurrentRingRadius(activeRing, activeRenderers);
        float radiusSqr = radius * radius;

        for (int i = runners.Count - 1;
             i >= 0 && losses < maxRunnersPerDischarge &&
             (isChargePhase ? chargeLosses : dischargeLosses) < maxRunnersPerDischarge;
             i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;

            GameObject runner = runners[i];
            Vector3 delta = runner.transform.position - transform.position;
            bool insideRadius = new Vector2(delta.x, delta.z).sqrMagnitude <= radiusSqr;
            bool insideHeight = Mathf.Abs(delta.y) <= runnerHeight;
            if (!insideRadius || !insideHeight) continue;

            bool removed = cachedCrowdManager.RemoveRunnerByHazard(
                runner,
                runner.transform.position + Vector3.up * 0.5f,
                GetRunnerColor(runner));
            if (removed)
            {
                losses++;
                if (isChargePhase) chargeLosses++;
                else dischargeLosses++;
            }
        }
    }

    private float GetCurrentRingRadius(Transform activeRing, Renderer[] activeRenderers)
    {
        if (activeRing == null || activeRenderers == null || activeRenderers.Length == 0)
            return killRadius;

        float radius = 0f;
        Vector3 plateCenter = transform.position;
        for (int i = 0; i < activeRenderers.Length; i++)
        {
            Renderer renderer = activeRenderers[i];
            if (renderer == null) continue;

            Bounds bounds = renderer.bounds;
            float xRadius = Mathf.Abs(bounds.center.x - plateCenter.x) + bounds.extents.x;
            float zRadius = Mathf.Abs(bounds.center.z - plateCenter.z) + bounds.extents.z;
            radius = Mathf.Max(radius, Mathf.Max(xRadius, zRadius));
        }

        return radius > 0f ? radius : killRadius;
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
                chargeRing.localScale = Vector3.one * Mathf.Lerp(0.7f, 2.5f, charge01);
            }
        }

        if (dischargeRing != null)
        {
            dischargeRing.gameObject.SetActive(discharging);
            if (discharging)
            {
                float discharge01 = (cycleClock - chargeDuration) / dischargeDuration;
                dischargeRing.localScale = Vector3.one * Mathf.Lerp(2.5f, 0.7f, discharge01);
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
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}
