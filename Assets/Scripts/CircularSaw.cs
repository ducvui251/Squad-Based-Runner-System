using UnityEngine;

public class CircularSaw : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float spinSpeed = 360f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField] private bool startAtRight;

    [Header("Collision Settings")]
    [Tooltip("Bán kính phát hiện lính chạm cưa để tiêu diệt.")]
    [SerializeField] private float killRadius = 0.8f;
    [Tooltip("Authored vertical body height used when testing the saw sphere against a runner.")]
    [SerializeField, Min(0.1f)] private float runnerHeight = 1.6f;
    [SerializeField] private bool useExplicitXLimits;
    [SerializeField] private float leftLimitOverride = -2.1f;
    [SerializeField] private float rightLimitOverride = 2.1f;

    private float leftLimit;
    private float rightLimit;
    private Vector3 startPosition;
    private PlayerCrowdManager cachedCrowdManager;
    private float motionClock;

    private void OnValidate()
    {
        moveSpeed = Mathf.Clamp(moveSpeed, 0.1f, 12f);
        spinSpeed = Mathf.Clamp(spinSpeed, 0f, 1440f);
        phaseOffset = Mathf.Max(0f, phaseOffset);
        killRadius = Mathf.Clamp(killRadius, 0.05f, 1.5f);
        runnerHeight = Mathf.Clamp(runnerHeight, 0.1f, 3f);
        leftLimitOverride = Mathf.Clamp(leftLimitOverride, -4.5f, 4.5f);
        rightLimitOverride = Mathf.Clamp(rightLimitOverride, -4.5f, 4.5f);
        if (rightLimitOverride < leftLimitOverride + 0.5f)
        {
            rightLimitOverride = Mathf.Min(4.5f, leftLimitOverride + 0.5f);
        }
    }

    private void Start()
    {
        startPosition = transform.position;
        cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
        motionClock = phaseOffset;
        DetectRoadBoundaries();
    }

    private void Update()
    {
        if (!UIManager.IsGameActive) return;

        // 1. Tính toán di chuyển qua lại (Ping-Pong)
        float range = rightLimit - leftLimit;
        if (range > 0f)
        {
            motionClock += Time.deltaTime;
            float offset = Mathf.PingPong(motionClock * moveSpeed, range);
            if (startAtRight) offset = range - offset;
            float targetX = leftLimit + offset;
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        }

        // 2. Quay tròn lưỡi cưa xung quanh trục Oz toàn cục
        transform.Rotate(Vector3.forward * spinSpeed * Time.deltaTime, Space.World);

        // 3. Kiểm tra va chạm với đám lính bằng khoảng cách toán học
        CheckRunnerCollisionsDistance();
    }

    private void CheckRunnerCollisionsDistance()
    {
        if (!UIManager.IsGameActive) return;

        if (cachedCrowdManager == null)
        {
            cachedCrowdManager = FindFirstObjectByType<PlayerCrowdManager>();
            if (cachedCrowdManager == null) return;
        }

        // Duyệt qua danh sách lính đang hoạt động O(1) thay vì duyệt qua children
        var runners = cachedCrowdManager.ActiveRunners;
        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (i >= runners.Count || runners[i] == null) continue;
            Transform child = runners[i].transform;

            // Treat each runner as a vertical body segment instead of a point.
            // The saw remains a sphere with killRadius; this does not enlarge its XZ area.
            Vector3 nearestBodyPoint = ClosestPointOnRunnerVerticalSegment(
                transform.position, child.position, runnerHeight);
            float distanceSqr = (transform.position - nearestBodyPoint).sqrMagnitude;

            // Nếu lính chạm vào bán kính nguy hiểm của cưa
            if (distanceSqr <= killRadius * killRadius)
            {
                // Lấy màu sắc của lính để sinh nổ hạt tương ứng
                Color popColor = Color.blue;
                Renderer r = child.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty("_Color"))
                    {
                        popColor = r.sharedMaterial.color;
                    }
                    else if (r.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        popColor = r.sharedMaterial.GetColor("_BaseColor");
                    }
                }

                // Sinh hiệu ứng nổ tại vị trí lính
                Vector3 effectPos = child.position + Vector3.up * 0.5f;
                DeathPopEffect.Create(effectPos, popColor);

                // Tiêu diệt lính xanh (xóa khỏi hàng ngũ chạy)
                cachedCrowdManager.RemoveRunner(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Returns the nearest point on a runner's authored vertical body segment.
    /// This is pure geometry so collision boundaries can be checked without scene state.
    /// </summary>
    public static Vector3 ClosestPointOnRunnerVerticalSegment(
        Vector3 sphereCenter, Vector3 runnerBase, float runnerHeight)
    {
        float nonNegativeHeight = Mathf.Max(0f, runnerHeight);
        float topY = runnerBase.y + nonNegativeHeight;
        float nearestY = Mathf.Clamp(sphereCenter.y, runnerBase.y, topY);
        return new Vector3(runnerBase.x, nearestY, runnerBase.z);
    }

    private void DetectRoadBoundaries()
    {
        if (useExplicitXLimits)
        {
            leftLimit = leftLimitOverride;
            rightLimit = rightLimitOverride;
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 20f))
        {
            Collider roadCol = hit.collider;
            if (roadCol != null && !roadCol.isTrigger)
            {
                Bounds bounds = roadCol.bounds;
                float margin = 0.4f;
                leftLimit = bounds.min.x + margin;
                rightLimit = bounds.max.x - margin;
                return;
            }
        }

        leftLimit = -2.1f;
        rightLimit = 2.1f;
    }

    private void OnDrawGizmos()
    {
        // Vẽ đường giới hạn di chuyển trong Scene View
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.red;

        float drawLeft = Application.isPlaying ? leftLimit : origin.x - 2.1f;
        float drawRight = Application.isPlaying ? rightLimit : origin.x + 2.1f;

        Vector3 leftLimitPoint = new Vector3(drawLeft, origin.y, origin.z);
        Vector3 rightLimitPoint = new Vector3(drawRight, origin.y, origin.z);

        Gizmos.DrawLine(leftLimitPoint, rightLimitPoint);
        Gizmos.DrawSphere(leftLimitPoint, 0.15f);
        Gizmos.DrawSphere(rightLimitPoint, 0.15f);

        // Vẽ bán kính nguy hiểm bao quanh cưa để căn chỉnh
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}
