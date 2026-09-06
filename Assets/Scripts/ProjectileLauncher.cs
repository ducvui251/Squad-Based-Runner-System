using UnityEngine;

/// <summary>
/// Turret that periodically launches pooled homing projectiles at the nearest
/// runner. Projectiles are created once and reused via <see cref="ObjectPool{T}"/>,
/// with a shared material + MaterialPropertyBlock so there are no per-shot
/// allocations, material clones, or Instantiate/Destroy calls.
/// </summary>
public class ProjectileLauncher : MonoBehaviour
{
    [Header("Projectile Launcher Settings")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float triggerRadius = 15f;
    [SerializeField] private Color projectileColor = Color.red;
    [SerializeField] private float initialFireDelay = 0f;

    private ObjectPool<TrackingProjectile> projectilePool;
    private PlayerCrowdManager cachedCrowdManager;
    private float fireTimer = 0f;

    private static Material sharedProjectileMaterial;
    private static MaterialPropertyBlock propBlock;

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();

        if (sharedProjectileMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            sharedProjectileMaterial = new Material(shader);
        }

        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        projectilePool = new ObjectPool<TrackingProjectile>(
            createFunc: CreateProjectile,
            onGet: projectile => projectile.gameObject.SetActive(true),
            onRelease: projectile => projectile.gameObject.SetActive(false),
            maxSize: poolSize);

        fireTimer = Mathf.Max(0f, initialFireDelay);

        // Pre-warm so the first shot doesn't pay a creation spike.
        projectilePool.Prewarm(4);
    }

    private TrackingProjectile CreateProjectile()
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = "TrackingProjectile";

        // Distance-based hit detection — no physics collider needed or wanted.
        Collider col = g.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        g.transform.localScale = Vector3.one * 0.4f;
        g.transform.SetParent(transform, false);

        Renderer r = g.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = sharedProjectileMaterial;
        }

        TrackingProjectile projectile = g.AddComponent<TrackingProjectile>();
        projectile.SetExpireCallback(ReleaseProjectile);

        g.SetActive(false);
        return projectile;
    }

    private void ReleaseProjectile(TrackingProjectile projectile)
    {
        if (projectilePool != null)
        {
            projectilePool.Release(projectile);
        }
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        // Only fire while the crowd is within range.
        GameObject target = cachedCrowdManager.GetClosestActiveRunner(transform.position, triggerRadius);
        if (target == null) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            FireAt(target);
        }
    }

    private void FireAt(GameObject target)
    {
        TrackingProjectile projectile = projectilePool.Get();

        Vector3 direction = (target.transform.position - transform.position).normalized;
        projectile.Launch(transform.position, direction);
        ApplyProjectileColor(projectile);
    }

    private void ApplyProjectileColor(TrackingProjectile projectile)
    {
        Renderer r = projectile != null ? projectile.GetComponentInChildren<Renderer>() : null;
        if (r == null) return;

        // Set color via MaterialPropertyBlock to avoid cloning the shared material.
        propBlock.SetColor("_Color", projectileColor);
        propBlock.SetColor("_BaseColor", projectileColor);
        r.SetPropertyBlock(propBlock);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
