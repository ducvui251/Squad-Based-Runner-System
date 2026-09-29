using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A homing projectile that tracks the nearest runner using vector interpolation:
/// each frame its velocity is lerped toward the desired heading, producing smooth,
/// arcing pursuit. Pooled via <see cref="ObjectPool{T}"/> — no Instantiate/Destroy.
/// </summary>
public class TrackingProjectile : MonoBehaviour
{
    private const int MaxImpactRunnerBufferSize = 10;

    [Header("Projectile Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float steerFactor = 6f;
    [SerializeField] private float killRadius = 0.6f;
    [SerializeField] private float lifetime = 5f;

    private Vector3 currentVelocity;
    private float remainingLifetime;
    private PlayerCrowdManager cachedCrowdManager;
    private Action<TrackingProjectile> onExpire;
    private int maxRunnersPerImpact = MaxImpactRunnerBufferSize;
    private readonly GameObject[] impactRunnerBuffer = new GameObject[MaxImpactRunnerBufferSize];
    private readonly float[] impactRunnerDistanceBuffer = new float[MaxImpactRunnerBufferSize];
    private struct RunnerSample
    {
        public Vector3 position;
        public uint activation;
        public int frame;
    }
    private readonly Dictionary<GameObject, RunnerSample> runnerSamples = new Dictionary<GameObject, RunnerSample>(501);
    private int sampleFrame = -1;

    /// <summary>Registers the callback used to return this projectile to its pool.</summary>
    public void SetExpireCallback(Action<TrackingProjectile> callback)
    {
        onExpire = callback;
    }

    /// <summary>Sets the bounded runner loss for this projectile's single impact.</summary>
    public void ConfigureImpactDamage(int maxRunners)
    {
        maxRunnersPerImpact = Mathf.Clamp(maxRunners, 1, MaxImpactRunnerBufferSize);
    }

    /// <summary>Starts the projectile from <paramref name="position"/> along <paramref name="direction"/>.</summary>
    public void Launch(Vector3 position, Vector3 direction)
    {
        transform.position = position;
        currentVelocity = direction.normalized * moveSpeed;
        remainingLifetime = lifetime;
        runnerSamples.Clear();
        if (cachedCrowdManager == null) cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        sampleFrame = Time.frameCount;
        if (cachedCrowdManager != null)
        {
            foreach (GameObject runner in cachedCrowdManager.ActiveRunners)
            {
                if (runner != null) StoreSample(runner);
            }
        }
        gameObject.SetActive(true);
    }

    // Crowd placement runs in Update; sweep against its completed movement.
    private void LateUpdate()
    {
        if (!UIManager.IsGameActive)
        {
            Expire();
            return;
        }

        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Expire();
            return;
        }

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null)
            {
                Expire();
                return;
            }
        }

        GameObject target = cachedCrowdManager.GetClosestActiveRunner(transform.position, 9999f);
        if (target != null)
        {
            // Vector-interpolated steering toward the target.
            Vector3 desiredVelocity = (target.transform.position - transform.position).normalized * moveSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, steerFactor * Time.deltaTime);

        }

        Vector3 start = transform.position;
        Vector3 end = start + currentVelocity * Time.deltaTime;
        GameObject firstHit = null;
        float firstTime = float.PositiveInfinity;
        foreach (GameObject runner in cachedCrowdManager.ActiveRunners)
        {
            if (runner == null) continue;
            Vector3 current = runner.transform.position;
            Vector3 previous = current;
            uint activation = cachedCrowdManager.GetRunnerActivation(runner);
            if (runnerSamples.TryGetValue(runner, out RunnerSample sample)
                && sample.frame == sampleFrame && sample.activation == activation)
            {
                previous = sample.position;
            }
            if (TrySweptHit(start - previous, end - current, killRadius, out float hitTime)
                && hitTime < firstTime)
            {
                firstHit = runner;
                firstTime = hitTime;
            }
            StoreSample(runner);
        }
        sampleFrame = Time.frameCount;
        if (firstHit != null)
        {
            transform.position = Vector3.LerpUnclamped(start, end, firstTime);
            RemoveImpactRunners(firstHit, transform.position);
            Expire();
            return;
        }
        transform.position = end;

        if (currentVelocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(currentVelocity.normalized);
        }
    }

    private void RemoveImpactRunners(GameObject firstHit, Vector3 impactPosition)
    {
        var runners = cachedCrowdManager.ActiveRunners;
        int candidateCount = 0;
        int protectedTargetCount = 0;

        if (firstHit != null)
        {
            impactRunnerBuffer[0] = firstHit;
            impactRunnerDistanceBuffer[0] = (firstHit.transform.position - impactPosition).sqrMagnitude;
            candidateCount = 1;
            protectedTargetCount = 1;
        }

        // Snapshot the nearest targets before removing any runners from the active list.
        for (int i = 0; i < runners.Count; i++)
        {
            GameObject runner = runners[i];
            if (runner == null || runner == firstHit) continue;

            float distanceSqr = (runner.transform.position - impactPosition).sqrMagnitude;
            int insertionIndex = protectedTargetCount;
            while (insertionIndex < candidateCount
                && impactRunnerDistanceBuffer[insertionIndex] <= distanceSqr)
            {
                insertionIndex++;
            }

            if (candidateCount >= maxRunnersPerImpact && insertionIndex >= maxRunnersPerImpact) continue;
            if (candidateCount < maxRunnersPerImpact) candidateCount++;

            for (int moveIndex = candidateCount - 1; moveIndex > insertionIndex; moveIndex--)
            {
                impactRunnerBuffer[moveIndex] = impactRunnerBuffer[moveIndex - 1];
                impactRunnerDistanceBuffer[moveIndex] = impactRunnerDistanceBuffer[moveIndex - 1];
            }

            impactRunnerBuffer[insertionIndex] = runner;
            impactRunnerDistanceBuffer[insertionIndex] = distanceSqr;
        }

        for (int i = 0; i < candidateCount; i++)
        {
            GameObject runner = impactRunnerBuffer[i];
            impactRunnerBuffer[i] = null;
            if (runner == null) continue;

            cachedCrowdManager.RemoveRunnerByHazard(
                runner,
                runner.transform.position + Vector3.up * 0.5f,
                GetRunnerColor(runner.transform));
        }
    }

    private void StoreSample(GameObject runner)
    {
        runnerSamples[runner] = new RunnerSample
        {
            position = runner.transform.position,
            activation = cachedCrowdManager.GetRunnerActivation(runner),
            frame = Time.frameCount
        };
    }

    // Relative segment against a sphere at the origin; returns earliest contact.
    public static bool TrySweptHit(Vector3 start, Vector3 end, float radius, out float time)
    {
        time = 0f;
        if (!(radius > 0f) || float.IsInfinity(radius)) return false;
        float c = Vector3.Dot(start, start) - radius * radius;
        if (c <= 0f) return true;
        Vector3 delta = end - start;
        float a = Vector3.Dot(delta, delta);
        if (a <= 0.0000001f) return false;
        float b = Vector3.Dot(start, delta);
        float discriminant = b * b - a * c;
        if (discriminant < 0f) return false;
        time = (-b - Mathf.Sqrt(discriminant)) / a;
        return time >= 0f && time <= 1f;
    }

    private void Expire()
    {
        gameObject.SetActive(false);
        onExpire?.Invoke(this);
    }

    private Color GetRunnerColor(Transform runner)
    {
        Renderer r = runner != null ? runner.GetComponentInChildren<Renderer>() : null;
        if (r != null && r.sharedMaterial != null)
        {
            if (r.sharedMaterial.HasProperty("_Color"))
            {
                return r.sharedMaterial.color;
            }

            if (r.sharedMaterial.HasProperty("_BaseColor"))
            {
                return r.sharedMaterial.GetColor("_BaseColor");
            }
        }

        return Color.blue;
    }
}
