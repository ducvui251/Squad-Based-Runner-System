using UnityEngine;

public class Enemy : MonoBehaviour
{
    private enum EnemyState { Idle, Running, Dead }
    private EnemyState state = EnemyState.Idle;

    [Header("Detection Settings")]
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private float combatDistance = 1.0f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private GameObject targetRunner;
    private Animator animator;
    private PlayerCrowdManager playerCrowd;
    private int lastPlaybackStateHash = 0; // Tracks the animation hash currently playing
    private bool isAlerted = false;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        playerCrowd = FindFirstObjectByType<PlayerCrowdManager>();

        // Set animator controller if cached from player
        if (animator != null && playerCrowd != null && animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController = playerCrowd.CachedAnimatorController;
        }

        // Configure Rigidbody if present to prevent physics issues
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Set kinematic to prevent sliding or gravity bugs
        }

        // Bắt đầu ở trạng thái Idle và tắt Animator để không chạy bất kỳ animation nào
        PlayIdleAnimation();
    }

    private void Update()
    {
        if (state == EnemyState.Dead) return;

        if (playerCrowd == null)
        {
            playerCrowd = FindFirstObjectByType<PlayerCrowdManager>();
            if (playerCrowd == null) return;
        }

        // Assign runtime animator controller if it wasn't ready at Start
        if (animator != null && animator.runtimeAnimatorController == null && playerCrowd.CachedAnimatorController != null)
        {
            animator.runtimeAnimatorController = playerCrowd.CachedAnimatorController;
            PlayIdleAnimation();
        }

        // Tìm lính xanh gần nhất không giới hạn khoảng cách khi đã kích hoạt đuổi bắt
        targetRunner = playerCrowd.GetClosestActiveRunner(transform.position, 9999f);

        if (targetRunner != null)
        {
            float distanceToRunner = Vector3.Distance(transform.position, targetRunner.transform.position);
            bool detectedSelf = distanceToRunner <= detectionDistance;

            // Nếu bản thân phát hiện thấy lính xanh hoặc đã nhận được báo động nhóm
            if (isAlerted || detectedSelf)
            {
                if (!isAlerted)
                {
                    AlertGroup(); // Báo động ngay lập tức cho các enemy khác
                }

                PlayRunAnimation();

                // Di chuyển hướng về phía lính xanh
                Vector3 targetPos = targetRunner.transform.position;
                targetPos.y = transform.position.y;

                Vector3 direction = (targetPos - transform.position).normalized;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                if (direction != Vector3.zero)
                {
                    transform.forward = direction;
                }

                // Kiểm tra khoảng cách để giao chiến
                if (distanceToRunner <= combatDistance)
                {
                    EngageCombat();
                }
            }
            else
            {
                PlayIdleAnimation();
            }
        }
        else
        {
            isAlerted = false; // Reset báo động khi không còn lính xanh nào trên màn chơi
            PlayIdleAnimation();
        }
    }

    public void TriggerAlert()
    {
        isAlerted = true;
    }

    private void AlertGroup()
    {
        isAlerted = true;

        // Tìm và báo động cho tất cả enemy cùng nhóm (có chung GameObject cha/thư mục cha)
        if (transform.parent != null)
        {
            Enemy[] siblings = transform.parent.GetComponentsInChildren<Enemy>();
            foreach (Enemy sibling in siblings)
            {
                if (sibling != null && !sibling.IsDead)
                {
                    sibling.TriggerAlert();
                }
            }
        }
        else
        {
            // Nếu không có nhóm cha, báo động cho toàn bộ enemy trong Scene
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy enemy in allEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TriggerAlert();
                }
            }
        }
    }

    private void PlayIdleAnimation()
    {
        if (animator == null) return;

        // Đảm bảo Animator hoạt động để giữ tư thế Idle tự nhiên thay vì T-pose
        animator.enabled = true;
        
        // Đặt tốc độ Animator về 0 để đóng băng chuyển động hoàn toàn
        animator.speed = 0f;

        // Cho chạy trạng thái Idle tại một thời điểm ngẫu nhiên để tư thế đứng của các enemy trông đa dạng
        if (animator.HasState(0, Animator.StringToHash("Idle")))
        {
            animator.Play("Idle", 0, Random.Range(0f, 1f));
        }
        else
        {
            animator.Play("Idle");
        }

        // Đặt lại các tham số bool trong Animator Controller nếu có
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                if (param.name == "IsRunning" || param.name == "isRunning" || param.name == "Run" || param.name == "run")
                {
                    animator.SetBool(param.name, false);
                }
            }
        }
    }

    private void PlayRunAnimation()
    {
        if (animator == null) return;

        // Bật lại Animator và đặt tốc độ bình thường (1) để enemy chạy
        animator.enabled = true;
        animator.speed = 1f;

        // Thử chạy các state Run hoặc Fast Run
        int runHash = Animator.StringToHash("Run");
        int fastRunHash = Animator.StringToHash("Fast Run");
        
        if (animator.HasState(0, fastRunHash))
        {
            animator.Play(fastRunHash);
        }
        else if (animator.HasState(0, runHash))
        {
            animator.Play(runHash);
        }

        // Đồng thời thiết lập các tham số bool trong Animator Controller nếu có
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                if (param.name == "IsRunning" || param.name == "isRunning" || param.name == "Run" || param.name == "run")
                {
                    animator.SetBool(param.name, true);
                }
            }
        }
    }

    public bool IsDead => state == EnemyState.Dead;

    public void KillByPlayer(Color playerColor)
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Dead;

        // Get enemy skin/material color to spawn a matching colored pop
        Color enemyColor = Color.red; // default fallback
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null && r.sharedMaterial != null)
        {
            // Try getting material color
            if (r.sharedMaterial.HasProperty("_Color"))
            {
                enemyColor = r.sharedMaterial.color;
            }
            else if (r.sharedMaterial.HasProperty("_BaseColor"))
            {
                enemyColor = r.sharedMaterial.GetColor("_BaseColor");
            }
        }

        Vector3 midPoint = transform.position + Vector3.up * 0.5f; // offset slightly off ground
        DeathPopEffect.Create(midPoint, playerColor);
        DeathPopEffect.Create(midPoint, enemyColor);

        Destroy(gameObject);
    }

    private void EngageCombat()
    {
        if (state == EnemyState.Dead) return;

        Color playerColor = Color.blue;
        if (playerCrowd != null && playerCrowd.GetLeadAnimator() != null)
        {
            Renderer pr = playerCrowd.GetLeadAnimator().GetComponentInChildren<Renderer>();
            if (pr != null && pr.sharedMaterial != null)
            {
                if (pr.sharedMaterial.HasProperty("_Color"))
                {
                    playerColor = pr.sharedMaterial.color;
                }
                else if (pr.sharedMaterial.HasProperty("_BaseColor"))
                {
                    playerColor = pr.sharedMaterial.GetColor("_BaseColor");
                }
            }
        }

        if (playerCrowd != null && targetRunner != null)
        {
            playerCrowd.RemoveRunner(targetRunner);
        }

        KillByPlayer(playerColor);
    }

    private void OnDrawGizmos()
    {
        // Draw a faint yellow circle on the ground for detection range
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range in yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        // Draw combat range in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatDistance);
    }
}
