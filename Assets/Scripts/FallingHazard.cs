using UnityEngine;

/// <summary>
/// A one-shot surprise gravity-drop trap. It stays hidden until the player crosses
/// its forward activation line, then appears above the track and drops with
/// accelerating gravity. Runner hits remain distance-based and need no physics body.
/// </summary>
public class FallingHazard : MonoBehaviour
{
    private enum HazardState { Hidden, Falling, Landed, Spent }

    [Header("Gravity Drop Trap Settings")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float killRadius = 1.2f;
    [SerializeField] private float floatingHeight = 5f;
    [SerializeField] private float resetDelay = 1.5f;
    [SerializeField] private float activationOffset = 7f;

    private HazardState state = HazardState.Hidden;
    private PlayerCrowdManager cachedCrowdManager;
    private Renderer[] cachedRenderers;
    private float groundY;
    private Vector3 originPos;
    private float yVelocity;
    private float stateTimer;
    private bool hasHitThisDrop;
    private bool hasTriggeredThisRun;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
    }

    private void Start()
    {
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        DetectGround();

        // Auto-raise the trap above the road so it can be placed at floor level in the editor.
        originPos = new Vector3(transform.position.x, groundY + floatingHeight, transform.position.z);
        transform.position = originPos;
        state = HazardState.Hidden;
        hasTriggeredThisRun = false;
        SetVisible(false);
    }

    private void DetectGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 30f))
        {
            Collider roadCol = hit.collider;
            if (roadCol != null && !roadCol.isTrigger)
            {
                groundY = roadCol.bounds.min.y;
                return;
            }
        }

        groundY = 0f;
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        switch (state)
        {
            case HazardState.Hidden:
                UpdateHidden();
                break;
            case HazardState.Falling:
                UpdateFalling();
                break;
            case HazardState.Landed:
                UpdateLanded();
                break;
            case HazardState.Spent:
                break;
        }
    }

    private void UpdateHidden()
    {
        if (hasTriggeredThisRun)
        {
            state = HazardState.Spent;
            return;
        }

        // The visual is hidden until the player crosses the forward trigger line.
        // Using Z rather than 3D distance makes timing consistent across lanes.
        float activationZ = originPos.z - activationOffset;
        if (cachedCrowdManager.transform.position.z >= activationZ)
        {
            BeginFalling();
        }
    }

    private void BeginFalling()
    {
        if (hasTriggeredThisRun) return;

        hasTriggeredThisRun = true;
        transform.position = originPos;
        SetVisible(true);
        state = HazardState.Falling;
        yVelocity = 0f;
        hasHitThisDrop = false;
    }

    private void UpdateFalling()
    {
        // Accelerated gravity drop.
        yVelocity += gravity * Time.deltaTime;
        transform.position += Vector3.up * (yVelocity * Time.deltaTime);

        // Crush any runner inside the kill radius on the way down (once per drop).
        if (!hasHitThisDrop)
        {
            CheckRunnerCollisions();
        }

        // Land on the road.
        if (transform.position.y <= groundY + 0.05f)
        {
            state = HazardState.Landed;
            stateTimer = 0f;
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }
    }

    private void UpdateLanded()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= resetDelay)
        {
            SetVisible(false);
            state = HazardState.Spent;
        }
    }

    private void SetVisible(bool visible)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].enabled = visible;
            }
        }
    }

    private void CheckRunnerCollisions()
    {
        var runners = cachedCrowdManager.ActiveRunners;
        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;
            Transform child = runners[i].transform;

            float distance = Vector3.Distance(child.position, transform.position);
            if (distance <= killRadius)
            {
                hasHitThisDrop = true; // One crush per drop so a single trap can't wipe the whole crowd.
                Color popColor = GetRunnerColor(child);
                Vector3 effectPos = child.position + Vector3.up * 0.5f;
                cachedCrowdManager.RemoveRunnerByHazard(child.gameObject, effectPos, popColor);
                break;
            }
        }
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

    private void OnDrawGizmos()
    {
        // Kill radius
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, killRadius);

        // Forward activation line. This is authoring/debug visualization only.
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
        Vector3 triggerCenter = new Vector3(transform.position.x, 1f, transform.position.z - activationOffset);
        Gizmos.DrawWireCube(triggerCenter, new Vector3(10f, 2f, 1f));
    }
}
