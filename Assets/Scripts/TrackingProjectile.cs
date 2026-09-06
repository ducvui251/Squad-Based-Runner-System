using System;
using UnityEngine;

/// <summary>
/// A homing projectile that tracks the nearest runner using vector interpolation:
/// each frame its velocity is lerped toward the desired heading, producing smooth,
/// arcing pursuit. Pooled via <see cref="ObjectPool{T}"/> — no Instantiate/Destroy.
/// </summary>
public class TrackingProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float steerFactor = 6f;
    [SerializeField] private float killRadius = 0.6f;
    [SerializeField] private float lifetime = 5f;

    private Vector3 currentVelocity;
    private float remainingLifetime;
    private PlayerCrowdManager cachedCrowdManager;
    private Action<TrackingProjectile> onExpire;

    /// <summary>Registers the callback used to return this projectile to its pool.</summary>
    public void SetExpireCallback(Action<TrackingProjectile> callback)
    {
        onExpire = callback;
    }

    /// <summary>Starts the projectile from <paramref name="position"/> along <paramref name="direction"/>.</summary>
    public void Launch(Vector3 position, Vector3 direction)
    {
        transform.position = position;
        currentVelocity = direction.normalized * moveSpeed;
        remainingLifetime = lifetime;
        gameObject.SetActive(true);
    }

    private void Update()
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

            if (Vector3.Distance(transform.position, target.transform.position) <= killRadius)
            {
                Color popColor = GetRunnerColor(target.transform);
                cachedCrowdManager.RemoveRunnerByHazard(target, transform.position, popColor);
                Expire();
                return;
            }
        }

        transform.position += currentVelocity * Time.deltaTime;

        if (currentVelocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(currentVelocity.normalized);
        }
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
