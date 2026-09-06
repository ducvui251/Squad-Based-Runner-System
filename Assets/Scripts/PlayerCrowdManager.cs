using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrowdManager : MonoBehaviour
{
    [Header("Runner Settings")]
    [SerializeField] private GameObject runnerPrefab;
    [SerializeField] private float spacingFactor = 0.75f;
    [SerializeField] private float lerpSpeed = 7f;
    [SerializeField] private int poolSize = 200;

    [Header("Crowd Representation")]
    [SerializeField, Min(1)] private int oneToOneLimit = 60;
    [SerializeField, Min(61)] private int compressionEndLogicalCount = 200;
    [SerializeField, Min(1)] private int maxVisualClones = 100;
    [SerializeField, Range(0.4f, 1f)] private float maxCompressionSpacingMultiplier = 0.7f;

    private List<GameObject> runnerPool = new List<GameObject>();
    private List<GameObject> activeRunners = new List<GameObject>();
    private List<GameObject> fallingRunners = new List<GameObject>();
    private Dictionary<GameObject, float> runnerSpawnTimes = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> fallingRunnerVelocities = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, int> edgeFrameCounters = new Dictionary<GameObject, int>(); // Consecutive frames a clone is detected off-edge
    private RuntimeAnimatorController cachedAnimatorController;
    private RaycastHit[] raycastResults = new RaycastHit[64];     // Large Raycast results cache
    private bool isStickmanAnimator;                              // Cached name check result
    private float leftLimit = -2.5f;                              // Dynamic track left boundary
    private float rightLimit = 2.5f;                             // Dynamic track right boundary
    private CharacterController playerCc;                         // Cached CharacterController reference
    private float trackDetectTimer = 0f;                          // Cooldown timer for DetectTrackWidth
    private const float TRACK_DETECT_INTERVAL = 0.25f;            // Only detect track width every 0.25 seconds
    private const int EDGE_CONFIRM_FRAMES = 3;                    // Clone must be off-edge for this many consecutive frames before falling
    private const float CLONE_GRACE_PERIOD = 0.5f;                // Seconds after spawn before a clone can be checked for edge falling
    private const float EDGE_MARGIN = 0.3f;                       // How far outside the track boundary before considering a clone "off-edge"
    private const int MAX_SAFE_POOL_SIZE = 500;
    private const int MAX_SAFE_VISUAL_CLONES = MAX_SAFE_POOL_SIZE;

    private bool isFighting = false;
    private float enemyTargetX = 0f;
    private float currentSpacingFactor = 0.75f;
    private Collider[] overlapResults = new Collider[32];
    private bool hasTriggeredGameOver = false;
    private float gameActiveTimer = 0f;                        // Time elapsed since game became active
    private const float GAME_OVER_GRACE_PERIOD = 1.5f;        // Seconds after game starts before game over can trigger
    private float lastGroundedY = 0f;                          // Tracks the last Y coordinate where the player was grounded

    public bool IsFighting => isFighting;
    public float EnemyTargetX => enemyTargetX;
    public bool IsGameOver => hasTriggeredGameOver;
    public List<GameObject> ActiveRunners => activeRunners;
    public int LogicalRunnerCount { get; private set; }
    public int VisualRunnerCount => activeRunners.Count;
    public int ActiveRunnerCount => LogicalRunnerCount;


    private void OnValidate()
    {
        poolSize = Mathf.Clamp(poolSize, 1, MAX_SAFE_POOL_SIZE);
        oneToOneLimit = Mathf.Clamp(oneToOneLimit, 1, MAX_SAFE_POOL_SIZE);
        compressionEndLogicalCount = Mathf.Clamp(
            compressionEndLogicalCount,
            oneToOneLimit + 1,
            int.MaxValue);
        maxVisualClones = Mathf.Clamp(maxVisualClones, oneToOneLimit, MAX_SAFE_VISUAL_CLONES);
        poolSize = Mathf.Clamp(Mathf.Max(poolSize, maxVisualClones), 1, MAX_SAFE_POOL_SIZE);
    }

    private void Start()
    {
        OnValidate();
        hasTriggeredGameOver = false;
        gameActiveTimer = 0f;
        playerCc = GetComponent<CharacterController>();
        lastGroundedY = transform.position.y;

        if (!ValidateRunnerPrefab())
        {
            enabled = false;
            return;
        }

        CountBadge.Attach(transform, () => ActiveRunnerCount, CountBadge.PlayerBlue, heightAbove: 2.2f);

        InitializePool();
        InitializeLeadPlayer();
        LogicalRunnerCount = activeRunners.Count;
        SyncVisualCrowdToLogicalCount();
        DetectTrackWidth();
        currentSpacingFactor = spacingFactor;
    }

    private bool ValidateRunnerPrefab()
    {
        if (runnerPrefab == null)
        {
            Debug.LogError("PlayerCrowdManager: runnerPrefab is not assigned. Assign a visual-only runner prefab.", this);
            return false;
        }

        if (runnerPrefab == gameObject || runnerPrefab.GetComponentInChildren<PlayerCrowdManager>(true) != null)
        {
            Debug.LogError("PlayerCrowdManager: runnerPrefab must be a visual-only clone prefab. It cannot be the player prefab or contain PlayerCrowdManager.", runnerPrefab);
            return false;
        }

        if (runnerPrefab.GetComponentInChildren<PlayerController>(true) != null)
        {
            Debug.LogError("PlayerCrowdManager: runnerPrefab contains PlayerController. Use a stripped-down visual runner prefab instead.", runnerPrefab);
            return false;
        }

        if (runnerPrefab.GetComponentInChildren<CharacterController>(true) != null)
        {
            Debug.LogError("PlayerCrowdManager: runnerPrefab contains CharacterController. Use a mesh/animator-only runner prefab instead.", runnerPrefab);
            return false;
        }

        return true;
    }

    private void DetectTrackWidth()
    {
        RaycastHit hit;
        // Check if we hit a road collider below the player (extended range for jumps/slopes)
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 15f))
        {
            Collider roadCol = hit.collider;
            if (roadCol != null && !roadCol.isTrigger)
            {
                // Filter out non-road geometry by the collider name so missing project tags
                // cannot generate runtime warnings.
                if (!IsObstacleCollider(roadCol))
                {
                    Bounds bounds = roadCol.bounds;
                    float width = bounds.size.x;
                    // Accept any valid road width without arbitrary small upper bounds
                    if (width > 2f)
                    {
                        leftLimit = bounds.min.x + 0.4f;
                        rightLimit = bounds.max.x - 0.4f;
                        return;
                    }
                }
            }
        }
        
        // Fallback to checking left and right boundaries with a wider raycast (50 units)
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.left, out hit, 50f))
        {
            leftLimit = hit.point.x + 0.4f;
        }
        else
        {
            leftLimit = transform.position.x - 10f; // Dynamic fallback relative to player
        }

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.right, out hit, 50f))
        {
            rightLimit = hit.point.x - 0.4f;
        }
        else
        {
            rightLimit = transform.position.x + 10f; // Dynamic fallback relative to player
        }
    }

    private static readonly string[] ObstacleNameTokens = { "saw", "cone", "gate", "door", "obstacle", "spike" };

    /// <summary>
    /// Returns true if the collider belongs to non-road geometry (saws, cones, gates, doors,
    /// or spikes) based on its name. Project-specific tags are intentionally not required.
    /// </summary>
    private bool IsObstacleCollider(Collider col)
    {
        string name = col.name;
        foreach (string token in ObstacleNameTokens)
        {
            if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return false;
    }


    private GameObject CreateNewPoolObject()
    {
        GameObject obj = Instantiate(runnerPrefab, transform);
        
        // Clean up any components causing physics conflicts on root/children immediately
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            Destroy(rb);
        }
        foreach (var col in obj.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
            Destroy(col);
        }
        foreach (var cc in obj.GetComponentsInChildren<CharacterController>())
        {
            cc.enabled = false;
            Destroy(cc);
        }

        obj.SetActive(false);
        runnerPool.Add(obj);
        return obj;
    }

    private void InitializePool()
    {
        // Pre-warm a small number of clones to avoid startup lag
        int initialWarmup = Mathf.Min(20, poolSize);
        for (int i = 0; i < initialWarmup; i++)
        {
            CreateNewPoolObject();
        }
    }

    private void InitializeLeadPlayer()
    {
        Animator rootAnim = GetComponent<Animator>();
        if (rootAnim != null && rootAnim.runtimeAnimatorController != null)
        {
            cachedAnimatorController = rootAnim.runtimeAnimatorController;
            isStickmanAnimator = cachedAnimatorController.name.Contains("Stickman");
        }

        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null && !child.name.Contains("Camera"))
            {
                activeRunners.Add(child.gameObject);
                runnerSpawnTimes[child.gameObject] = Time.time;

                if (cachedAnimatorController == null)
                {
                    Animator startingAnim = child.GetComponentInChildren<Animator>();
                    if (startingAnim != null)
                    {
                        cachedAnimatorController = startingAnim.runtimeAnimatorController;
                        if (cachedAnimatorController != null)
                        {
                            isStickmanAnimator = cachedAnimatorController.name.Contains("Stickman");
                        }
                    }
                }
                break;
            }
        }

        if (activeRunners.Count == 0)
        {
            TryActivatePooledRunner();
        }
    }

    private void Update()
    {
        // Track how long the game has been active to enforce a grace period
        if (UIManager.IsGameActive)
        {
            gameActiveTimer += Time.deltaTime;
        }
        else
        {
            // Reset timer when game is not active (e.g. in menu)
            gameActiveTimer = 0f;
        }

        // Update last grounded Y when player is grounded to support sloped tracks
        if (playerCc != null && playerCc.isGrounded)
        {
            lastGroundedY = transform.position.y;
        }

        // --- Game Over checks (only after grace period to avoid false triggers at startup) ---
        if (UIManager.IsGameActive && !hasTriggeredGameOver && gameActiveTimer > GAME_OVER_GRACE_PERIOD)
        {
            // Check 1: Player fell off the track (Y coordinate dropped by more than 10 meters below last grounded height)
            if (transform.position.y < lastGroundedY - 10f)
            {
                hasTriggeredGameOver = true;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.TriggerLeadFallGameOver(activeRunners.Count > 0 ? activeRunners[0].transform : transform);
                }
                return;
            }

            // Check 2: All logical runners have been lost
            if (LogicalRunnerCount == 0)
            {
                hasTriggeredGameOver = true;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOver(0);
                }
                return;
            }
        }

        // Detect track width on a cooldown to adapt to changing road geometry
        trackDetectTimer -= Time.deltaTime;
        if (trackDetectTimer <= 0f)
        {
            DetectTrackWidth();
            trackDetectTimer = TRACK_DETECT_INTERVAL;
        }

        ScanForEnemies();

        if (isFighting)
        {
            ProcessCombat();
        }

        // Smoothly lerp crowd compression spacing factor
        float targetSpacing = isFighting ? 0.35f : spacingFactor;
        currentSpacingFactor = Mathf.Lerp(currentSpacingFactor, targetSpacing, Time.deltaTime * 5f);

        RearrangeCrowd();
        HandleFallingRunners();
        UpdateLeadPlayerAnimation();
    }

    private void ScanForEnemies()
    {
        // Scan for colliders within 12 meters around the player
        int count = Physics.OverlapSphereNonAlloc(transform.position, 12f, overlapResults);
        
        Enemy closestEnemy = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapResults[i];
            if (col == null || col.isTrigger) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = enemy;
                }
            }
        }

        if (closestEnemy != null)
        {
            isFighting = true;
            enemyTargetX = closestEnemy.transform.position.x;
        }
        else
        {
            isFighting = false;
        }

        // Clean up overlap array to prevent stale references
        for (int i = 0; i < overlapResults.Length; i++)
        {
            overlapResults[i] = null;
        }
    }

    private void ProcessCombat()
    {
        if (activeRunners.Count == 0) return;

        // We scan for colliders within 6 meters around the player's crowd center
        int count = Physics.OverlapSphereNonAlloc(transform.position, 6f, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapResults[i];
            if (col == null || col.isTrigger) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                // Find the closest runner to this enemy
                GameObject closestRunner = null;
                float minRunnerDist = 1.3f; // combat range

                for (int j = 0; j < activeRunners.Count; j++)
                {
                    if (activeRunners[j] == null) continue;
                    float dist = Vector3.Distance(activeRunners[j].transform.position, enemy.transform.position);
                    if (dist < minRunnerDist)
                    {
                        minRunnerDist = dist;
                        closestRunner = activeRunners[j];
                    }
                }

                if (closestRunner != null)
                {
                    ResolveEnemyCombat(enemy, closestRunner);
                }
            }
        }

        // Clean up overlap array
        for (int i = 0; i < overlapResults.Length; i++)
        {
            overlapResults[i] = null;
        }
    }

    private void UpdateLeadPlayerAnimation()
    {
        if (activeRunners.Count == 0 || activeRunners[0] == null) return;

        Animator leadAnim = activeRunners[0].GetComponentInChildren<Animator>();
        if (leadAnim == null) return;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            if (!cc.isGrounded && cc.velocity.y < -1f)
            {
                if (!TryPlayAnimationState(leadAnim, "Falling"))
                {
                    TryPlayAnimationState(leadAnim, "Fall");
                }
            }
            else if (cc.isGrounded)
            {
                AnimatorStateInfo stateInfo = leadAnim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Falling") || stateInfo.IsName("Fall"))
                {
                    if (cachedAnimatorController != null && isStickmanAnimator)
                    {
                        TryPlayAnimationState(leadAnim, "Run");
                    }
                    else
                    {
                        TryPlayAnimationState(leadAnim, "Fast Run");
                    }
                }
            }
        }
    }

    private void HandleFallingRunners()
    {
        PlayerController pc = GetComponent<PlayerController>();
        float forwardSpeed = pc != null ? pc.ForwardSpeed : 6f;
        float gravity = pc != null ? pc.Gravity : -20f;

        for (int i = fallingRunners.Count - 1; i >= 0; i--)
        {
            if (fallingRunners[i] == null)
            {
                fallingRunners.RemoveAt(i);
                continue;
            }

            GameObject runner = fallingRunners[i];
            float yVelocity;
            if (!fallingRunnerVelocities.TryGetValue(runner, out yVelocity))
            {
                yVelocity = -2f;
            }

            yVelocity += gravity * Time.deltaTime;
            fallingRunnerVelocities[runner] = yVelocity;

            Vector3 movement = (Vector3.forward * forwardSpeed + Vector3.up * yVelocity) * Time.deltaTime;
            runner.transform.position += movement;
            runner.transform.Rotate(Vector3.right, 45f * Time.deltaTime);

            // Clean up when the runner has fallen far below the track level (25 meters)
            if (transform.position.y - runner.transform.position.y > 25f)
            {
                fallingRunnerVelocities.Remove(runner);
                edgeFrameCounters.Remove(runner);
                runner.SetActive(false);
                fallingRunners.RemoveAt(i);
            }
        }
    }

    public void AddLogicalRunners(int amount)
    {
        if (amount <= 0) return;

        long targetCount = (long)LogicalRunnerCount + amount;
        LogicalRunnerCount = targetCount > int.MaxValue ? int.MaxValue : (int)targetCount;
        SyncVisualCrowdToLogicalCount();
    }

    public void MultiplyLogicalRunners(int factor)
    {
        if (factor <= 0)
        {
            LogicalRunnerCount = 0;
        }
        else
        {
            long targetCount = (long)LogicalRunnerCount * factor;
            LogicalRunnerCount = targetCount > int.MaxValue ? int.MaxValue : (int)targetCount;
        }

        SyncVisualCrowdToLogicalCount();
    }

    public void RemoveLogicalRunners(int amount)
    {
        ApplyLogicalLoss(amount, false);
    }

    public void SpawnClones(int amount) => AddLogicalRunners(amount);

    public void MultiplyClones(int factor) => MultiplyLogicalRunners(factor);

    private void ApplyLogicalLoss(int amount, bool allowVisualReplenish, bool checkGameOver = true)
    {
        if (amount <= 0 || LogicalRunnerCount <= 0) return;

        LogicalRunnerCount = Mathf.Max(0, LogicalRunnerCount - amount);
        SyncVisualCrowdToLogicalCount(allowVisualReplenish || LogicalRunnerCount <= oneToOneLimit);
        if (checkGameOver)
        {
            CheckLogicalGameOver();
        }
    }

    private void CheckLogicalGameOver()
    {
        if (LogicalRunnerCount > 0 || hasTriggeredGameOver || !UIManager.IsGameActive || gameActiveTimer <= GAME_OVER_GRACE_PERIOD)
        {
            return;
        }

        hasTriggeredGameOver = true;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver(0);
        }
    }

    private int GetDesiredVisualRunnerCount(int logicalCount)
    {
        if (logicalCount <= 0) return 0;

        int visualCap = Mathf.Max(oneToOneLimit, maxVisualClones);
        if (logicalCount <= oneToOneLimit)
        {
            return Mathf.Clamp(logicalCount, 0, visualCap);
        }

        if (logicalCount >= compressionEndLogicalCount)
        {
            return visualCap;
        }

        float t = Mathf.InverseLerp(oneToOneLimit, compressionEndLogicalCount, logicalCount);
        int desiredCount = Mathf.RoundToInt(Mathf.Lerp(oneToOneLimit, visualCap, t));
        if (visualCap > oneToOneLimit)
        {
            desiredCount = Mathf.Max(oneToOneLimit + 1, desiredCount);
        }
        return Mathf.Clamp(desiredCount, 0, visualCap);
    }

    private void SyncVisualCrowdToLogicalCount(bool allowVisualGrowth = true)
    {
        int desiredVisualCount = Mathf.Min(GetDesiredVisualRunnerCount(LogicalRunnerCount), poolSize);

        while (activeRunners.Count > desiredVisualCount)
        {
            DeactivateVisualRunner(activeRunners[activeRunners.Count - 1]);
        }

        if (!allowVisualGrowth)
        {
            return;
        }

        while (activeRunners.Count < desiredVisualCount)
        {
            if (!TryActivatePooledRunner())
            {
                break;
            }
        }
    }

    private bool TryActivatePooledRunner()
    {
        for (int i = 0; i < runnerPool.Count; i++)
        {
            if (!runnerPool[i].activeSelf && !fallingRunners.Contains(runnerPool[i]))
            {
                ActivateRunnerFromPool(runnerPool[i]);
                return true;
            }
        }

        if (runnerPool.Count >= poolSize)
        {
            return false;
        }

        ActivateRunnerFromPool(CreateNewPoolObject());
        return true;
    }

    private void ActivateRunnerFromPool(GameObject runner)
    {
        runner.transform.SetParent(transform); // Ensure it is reparented to the crowd manager
        runner.transform.position = transform.position;
        runner.transform.localRotation = Quaternion.identity;
        runner.SetActive(true);

        Animator anim = runner.GetComponentInChildren<Animator>();
        if (anim != null && cachedAnimatorController != null)
        {
            anim.runtimeAnimatorController = cachedAnimatorController;
            if (activeRunners.Count > 0 && activeRunners[0] != null)
            {
                Animator leadAnim = activeRunners[0].GetComponentInChildren<Animator>();
                if (leadAnim != null)
                {
                    SyncAnimatorState(anim, leadAnim);
                }
            }
        }

        if (activeRunners.Count > 0 && activeRunners[0] != null)
        {
            SkinnedMeshRenderer leadRenderer = activeRunners[0].GetComponentInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer newRenderer = runner.GetComponentInChildren<SkinnedMeshRenderer>();
            if (leadRenderer != null && newRenderer != null)
            {
                newRenderer.sharedMaterial = leadRenderer.sharedMaterial;
            }
        }

        activeRunners.Add(runner);
        runnerSpawnTimes[runner] = Time.time;
        edgeFrameCounters[runner] = 0;
    }

    private void SyncAnimatorState(Animator targetAnim, Animator sourceAnim)
    {
        if (targetAnim == null || sourceAnim == null || targetAnim.runtimeAnimatorController == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = sourceAnim.GetCurrentAnimatorStateInfo(0);
        if (targetAnim.HasState(0, stateInfo.fullPathHash))
        {
            targetAnim.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
            return;
        }

        if (TryPlayAnimationState(targetAnim, "Run", stateInfo.normalizedTime))
        {
            return;
        }

        TryPlayAnimationState(targetAnim, "Fast Run", stateInfo.normalizedTime);
    }

    private bool TryPlayAnimationState(Animator anim, string stateName, float normalizedTime = 0f)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
        {
            return false;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!anim.HasState(0, stateHash))
        {
            return false;
        }

        anim.Play(stateHash, 0, normalizedTime);
        return true;
    }

    public RuntimeAnimatorController CachedAnimatorController => cachedAnimatorController;

    public void RemoveRunner(GameObject runner)
    {
        if (runner == null) return;

        Color runnerColor = GetRunnerColor(runner, Color.blue);
        RemoveRunnerByHazard(runner, runner.transform.position + Vector3.up * 0.5f, runnerColor);
    }

    public bool RemoveRunnerByHazard(GameObject visualRunner, Vector3 effectPosition, Color effectColor)
    {
        if (visualRunner == null || !activeRunners.Contains(visualRunner) || LogicalRunnerCount <= 0)
        {
            return false;
        }

        int logicalLoss = GetLogicalLossForVisualRunner();
        DeathPopEffect.Create(effectPosition, effectColor);
        DeactivateVisualRunner(visualRunner);
        ApplyLogicalLoss(logicalLoss, false);
        return true;
    }

    public bool ResolveEnemyCombat(Enemy enemy, GameObject visualRunner)
    {
        if (enemy == null || enemy.IsDead || visualRunner == null || !activeRunners.Contains(visualRunner) || LogicalRunnerCount <= 0)
        {
            return false;
        }

        Color playerColor = GetPlayerColor();
        int logicalLoss = GetLogicalLossForVisualRunner();
        DeactivateVisualRunner(visualRunner);
        ApplyLogicalLoss(logicalLoss, false);
        enemy.KillByPlayer(playerColor);
        return true;
    }

    private int GetLogicalLossForVisualRunner()
    {
        if (LogicalRunnerCount <= 0) return 0;
        if (activeRunners.Count <= 0) return 1;

        float representedRunners = LogicalRunnerCount / Mathf.Max(1f, activeRunners.Count);
        return Mathf.Clamp(Mathf.RoundToInt(representedRunners), 1, LogicalRunnerCount);
    }

    private void DeactivateVisualRunner(GameObject runner)
    {
        if (runner == null) return;

        activeRunners.Remove(runner);
        runnerSpawnTimes.Remove(runner);
        fallingRunnerVelocities.Remove(runner);
        edgeFrameCounters.Remove(runner);
        runner.transform.SetParent(transform);
        runner.SetActive(false);
    }

    private Color GetPlayerColor()
    {
        Animator leadAnim = GetLeadAnimator();
        if (leadAnim == null)
        {
            return Color.blue;
        }

        return GetRunnerColor(leadAnim.gameObject, Color.blue);
    }

    private Color GetRunnerColor(GameObject runner, Color fallback)
    {
        Renderer renderer = runner != null ? runner.GetComponentInChildren<Renderer>() : null;
        if (renderer != null && renderer.sharedMaterial != null)
        {
            if (renderer.sharedMaterial.HasProperty("_Color"))
            {
                return renderer.sharedMaterial.color;
            }

            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                return renderer.sharedMaterial.GetColor("_BaseColor");
            }
        }

        return fallback;
    }

    public GameObject GetClosestActiveRunner(Vector3 checkPosition, float maxDistance)
    {
        GameObject closest = null;
        float minDistance = maxDistance;
        for (int i = 0; i < activeRunners.Count; i++)
        {
            if (activeRunners[i] == null) continue;
            float dist = Vector3.Distance(activeRunners[i].transform.position, checkPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = activeRunners[i];
            }
        }
        return closest;
    }

    public Animator GetLeadAnimator()
    {
        if (activeRunners.Count > 0 && activeRunners[0] != null)
        {
            return activeRunners[0].GetComponentInChildren<Animator>();
        }
        return null;
    }

    private void MakeRunnerFall(GameObject runner)
    {
        int logicalLoss = GetLogicalLossForVisualRunner();
        activeRunners.Remove(runner);
        runnerSpawnTimes.Remove(runner);
        edgeFrameCounters.Remove(runner);

        runner.transform.SetParent(null);
        fallingRunners.Add(runner);
        fallingRunnerVelocities[runner] = -2f;

        Animator cloneAnim = runner.GetComponentInChildren<Animator>();
        if (cloneAnim != null)
        {
            if (!TryPlayAnimationState(cloneAnim, "Falling"))
            {
                TryPlayAnimationState(cloneAnim, "Fall");
            }
        }

        ApplyLogicalLoss(logicalLoss, false, false);

        // Trigger zoom game over if this was the last clone falling off the edge
        // Also respect the grace period to prevent false triggers at startup
        if (UIManager.IsGameActive && !hasTriggeredGameOver && gameActiveTimer > GAME_OVER_GRACE_PERIOD && LogicalRunnerCount == 0)
        {
            hasTriggeredGameOver = true;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.TriggerLeadFallGameOver(runner.transform);
            }
        }
    }

    private bool CheckGroundBelow(float runnerWorldX, out RaycastHit groundHit)
    {
        // Use the runner's X coordinate, but the player's Y and Z coordinates.
        // This prevents clones from falling off when road chunks behind the player are destroyed.
        Vector3 rayStart = new Vector3(runnerWorldX, transform.position.y + 5.0f, transform.position.z);
        float rayDistance = 15.0f;

        // Use RaycastNonAlloc to avoid heap allocation from RaycastAll
        int hitCount = Physics.RaycastNonAlloc(rayStart, Vector3.down, raycastResults, rayDistance);
        
        float minDistance = float.MaxValue;
        bool foundGround = false;
        groundHit = new RaycastHit();

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastResults[i];

            if (hit.collider.isTrigger) continue;
            if (hit.transform.root == transform.root) continue;
            if (hit.transform.GetComponentInParent<PlayerCrowdManager>() != null) continue;

            // Find the closest solid ground
            if (hit.distance < minDistance)
            {
                minDistance = hit.distance;
                groundHit = hit;
                foundGround = true;
            }
        }

        return foundGround;
    }

    private void RearrangeCrowd()
    {
        int count = activeRunners.Count;
        if (count == 0) return;

        // Skip edge-falling checks entirely when the player is airborne (jumping/falling)
        bool isPlayerGrounded = playerCc == null || playerCc.isGrounded;

        float goldenAngle = 137.5f * Mathf.Deg2Rad;
        float representationRatio = LogicalRunnerCount / Mathf.Max(1f, activeRunners.Count);
        float compressionT = representationRatio <= 1f
            ? 0f
            : Mathf.InverseLerp(oneToOneLimit, compressionEndLogicalCount, LogicalRunnerCount);
        float densityMultiplier = Mathf.Lerp(1f, maxCompressionSpacingMultiplier, compressionT);
        float formationSpacing = currentSpacingFactor * densityMultiplier;

        for (int i = count - 1; i >= 0; i--)
        {
            if (activeRunners[i] == null) continue;

            // --- Edge-falling check (only for non-lead clones OUTSIDE track boundaries) ---
            if (i > 0 && isPlayerGrounded)
            {
                float spawnTime;
                bool hasSpawnTime = runnerSpawnTimes.TryGetValue(activeRunners[i], out spawnTime);
                float age = hasSpawnTime ? (Time.time - spawnTime) : 999f;

                // Use the declared CLONE_GRACE_PERIOD constant for grace period
                if (age > CLONE_GRACE_PERIOD)
                {
                    float cloneWorldX = activeRunners[i].transform.position.x;

                    // ONLY check clones whose world X is OUTSIDE the track boundaries + margin.
                    // Clones inside the track boundaries are NEVER checked and NEVER fall.
                    bool isOutsideTrack = cloneWorldX < (leftLimit - EDGE_MARGIN) || cloneWorldX > (rightLimit + EDGE_MARGIN);

                    if (isOutsideTrack)
                    {
                        // Check ground below using the clone's X coordinate, but player's Y and Z
                        RaycastHit hit;
                        bool hitGround = CheckGroundBelow(cloneWorldX, out hit);

                        if (!hitGround)
                        {
                            // Increment consecutive frame counter — require EDGE_CONFIRM_FRAMES before falling
                            int frameCount;
                            if (!edgeFrameCounters.TryGetValue(activeRunners[i], out frameCount))
                            {
                                frameCount = 0;
                            }
                            frameCount++;
                            edgeFrameCounters[activeRunners[i]] = frameCount;

                            if (frameCount >= EDGE_CONFIRM_FRAMES)
                            {
                                GameObject fallingRunner = activeRunners[i];
                                MakeRunnerFall(fallingRunner);
                                continue;
                            }
                        }
                        else
                        {
                            // Ground was found — reset the counter, clone is safe
                            edgeFrameCounters[activeRunners[i]] = 0;
                        }
                    }
                    else
                    {
                        // Clone is inside track boundaries — reset counter if it had one
                        if (edgeFrameCounters.ContainsKey(activeRunners[i]))
                        {
                            edgeFrameCounters[activeRunners[i]] = 0;
                        }
                    }
                }
            }

            float distance = formationSpacing * Mathf.Sqrt(i);
            float angle = i * goldenAngle;

            float x = distance * Mathf.Cos(angle);
            float z = distance * Mathf.Sin(angle);

            Vector3 targetLocalPos = new Vector3(x, 0f, z);

            // Only clamp the lead runner (index 0) to ensure they stay on the track.
            // Clones are allowed to go outside and fall off naturally if the crowd is too wide.
            if (i == 0)
            {
                float localLeftLimit = leftLimit - transform.position.x;
                float localRightLimit = rightLimit - transform.position.x;
                targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, localLeftLimit, localRightLimit);
            }

            activeRunners[i].transform.localPosition = Vector3.Lerp(
                activeRunners[i].transform.localPosition,
                targetLocalPos,
                Time.deltaTime * lerpSpeed
            );

            activeRunners[i].transform.localRotation = Quaternion.identity;
        }
    }
}
