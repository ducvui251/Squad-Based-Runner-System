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

    [Header("Default Skin Crowd Color Override")]
    [SerializeField] private bool useCrowdTintOverride;
    [SerializeField] private Color crowdTintOverrideColor = new Color(0.34f, 0.62f, 0.95f, 1f);

    [Header("Crowd Representation")]
    [SerializeField, Min(1)] private int oneToOneLimit = 60;
    [SerializeField, Min(61)] private int compressionEndLogicalCount = 200;
    [SerializeField, Min(1)] private int maxVisualClones = 100;
    [SerializeField, Range(0.4f, 1f)] private float maxCompressionSpacingMultiplier = 0.7f;

    [Header("Jump Wave Presentation")]
    [SerializeField, Min(0f)] private float jumpWaveMaxDelay = 0.22f;
    [SerializeField, Range(0f, 1f)] private float jumpWaveBlend = 1f;

    private List<GameObject> runnerPool = new List<GameObject>();
    private List<GameObject> activeRunners = new List<GameObject>();
    private readonly List<Renderer> crowdTintRendererBuffer = new List<Renderer>(4);
    private MaterialPropertyBlock crowdTintPropertyBlock;
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorPropertyId = Shader.PropertyToID("_Color");
    private readonly Dictionary<GameObject, uint> runnerActivations = new Dictionary<GameObject, uint>(501);
    private uint nextRunnerActivation;

    public uint GetRunnerActivation(GameObject runner)
    {
        return runner != null && runnerActivations.TryGetValue(runner, out uint activation) ? activation : 0;
    }
    private List<GameObject> fallingRunners = new List<GameObject>();
    private Dictionary<GameObject, float> runnerSpawnTimes = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> fallingRunnerVelocities = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, int> edgeFrameCounters = new Dictionary<GameObject, int>(); // Consecutive frames a clone is detected off-edge
    private RuntimeAnimatorController cachedAnimatorController;
    private Avatar cachedAnimatorAvatar;
    private static SkinAvatarCatalog cachedSkinAvatarCatalog;
    private static bool hasLoadedSkinAvatarCatalog;
    private const string SkinAvatarCatalogResourcePath = "SkinAvatarCatalog";
    private const string EquippedSkinVisualPrefix = "EquippedSkinVisual_";
    private RaycastHit[] raycastResults = new RaycastHit[64];     // Large Raycast results cache
    private bool isStickmanAnimator;                              // Cached name check result
    private float leftLimit = -2.5f;                              // Dynamic track left boundary
    private float rightLimit = 2.5f;                             // Dynamic track right boundary
    private CharacterController playerCc;                         // Cached CharacterController reference
    private PlayerController playerController;                    // Cached lead movement authority
    private bool hasDetectedTrackWidth;                            // Preserve the last valid road width across physical gaps
    private float trackDetectTimer = 0f;                          // Cooldown timer for DetectTrackWidth
    private const float TRACK_DETECT_INTERVAL = 0.25f;            // Only detect track width every 0.25 seconds
    private const int EDGE_CONFIRM_FRAMES = 3;                    // Clone must be off-edge for this many consecutive frames before falling
    private const float CLONE_GRACE_PERIOD = 0.5f;                // Seconds after spawn before a clone can be checked for edge falling
    private const float EDGE_MARGIN = 0.3f;                       // How far outside the track boundary before considering a clone "off-edge"
    private const int MAX_SAFE_POOL_SIZE = 500;
    private const int MAX_SAFE_VISUAL_CLONES = MAX_SAFE_POOL_SIZE;

    private enum RoadGapRunnerState
    {
        SafeNearSide,
        OverGap,
        SafeFarSide,
        Failed
    }

    private sealed class RoadGapRunnerRecord
    {
        public RoadGapRunnerState state;
        public Vector3 previousPosition;
    }

    // One bounded session is enough because Level 5 fractures are sequential. The
    // runner keys preserve identity when the remaining spiral repacks after a loss.
    private readonly Dictionary<GameObject, RoadGapRunnerRecord> roadGapRunnerRecords =
        new Dictionary<GameObject, RoadGapRunnerRecord>(MAX_SAFE_VISUAL_CLONES);
    private GameObject roadGapLead;
    private int roadGapSessionId;
    private int roadGapParticipantCount;
    private int roadGapResolvedCount;
    private bool roadGapActive;
    private bool roadGapComplete;

    // A bounded history of the one authoritative player trajectory lets visual
    // runners display a delayed front-to-back jump wave without giving any runner
    // its own velocity, gravity, collider, or jump state.
    private const int JUMP_WAVE_SAMPLE_CAPACITY = 64;
    private readonly float[] jumpWaveSampleTimes = new float[JUMP_WAVE_SAMPLE_CAPACITY];
    private readonly float[] jumpWaveSampleHeights = new float[JUMP_WAVE_SAMPLE_CAPACITY];
    private int jumpWaveSampleHead = -1;
    private int jumpWaveSampleCount;
    private float jumpWaveBaseY;
    private bool jumpWaveActive;

    private bool isFighting = false;
    private float enemyTargetX = 0f;
    private float currentSpacingFactor = 0.75f;
    private Collider[] overlapResults = new Collider[32];
    private bool hasTriggeredGameOver = false;
    private bool hasTriggeredLeadFallGameOver = false;
    [SerializeField] private SfxPlayer sfxPlayer;
    private float gameActiveTimer = 0f;                        // Time elapsed since game became active
    private const float GAME_OVER_GRACE_PERIOD = 1.5f;        // Seconds after game starts before game over can trigger
    private float lastGroundedY = 0f;                          // Tracks the last Y coordinate where the player was grounded

    private bool bossFightActive;
    private float bossFightTargetX;

    public bool IsFighting => isFighting || bossFightActive;
    public bool IsBossFightActive => bossFightActive;
    public float EnemyTargetX => bossFightActive ? bossFightTargetX : enemyTargetX;
    public bool IsGameOver => hasTriggeredGameOver;
    public bool IsLeadFallGameOver => hasTriggeredLeadFallGameOver;
    public List<GameObject> ActiveRunners => activeRunners;
    public int LogicalRunnerCount { get; private set; }
    public int VisualRunnerCount => activeRunners.Count;
    public int ActiveRunnerCount => LogicalRunnerCount;

    public void PlayCoinPickupSfx()
    {
        sfxPlayer?.PlayCoinPickup();
    }

    public void PlayTankShotSfx()
    {
        sfxPlayer?.PlayTankShot();
    }

    public void BeginBossFight(float targetX)
    {
        bossFightTargetX = targetX;
        bossFightActive = true;
    }

    public void EndBossFight()
    {
        bossFightActive = false;
    }


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
        jumpWaveMaxDelay = Mathf.Clamp(jumpWaveMaxDelay, 0f, 0.5f);
        jumpWaveBlend = Mathf.Clamp01(jumpWaveBlend);
    }

    private void Start()
    {
        OnValidate();
        hasTriggeredGameOver = false;
        hasTriggeredLeadFallGameOver = false;
        gameActiveTimer = 0f;
        playerCc = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        lastGroundedY = transform.position.y;
        ResetJumpWave();

        if (!ValidateRunnerPrefab())
        {
            enabled = false;
            return;
        }

        CountBadge.Attach(transform, () => ActiveRunnerCount, CountBadge.PlayerBlue, heightAbove: 2.2f);

        ApplyEquippedSkinToCrowd(SkinShopController.EquippedSkinId);
        ApplyCrowdTint(runnerPrefab);
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
                        hasDetectedTrackWidth = true;
                        return;
                    }
                }
            }
        }
        
        // A physical road gap has no collider below the player. Keep the last
        // valid width instead of replacing it with player-relative fallback bounds.
        if (hasDetectedTrackWidth) return;

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

        SkinShopController.ApplyEquippedSkin(obj);
        ApplyCrowdTint(obj);
        obj.SetActive(false);
        runnerPool.Add(obj);
        return obj;
    }

    private void ApplyCrowdTint(GameObject runner)
    {
        if (!useCrowdTintOverride || runner == null)
        {
            return;
        }

        // Keep the selected avatar model on the same authored blue tint as the default skin.

        if (crowdTintPropertyBlock == null)
        {
            crowdTintPropertyBlock = new MaterialPropertyBlock();
        }

        crowdTintRendererBuffer.Clear();
        runner.GetComponentsInChildren(true, crowdTintRendererBuffer);
        for (int i = 0; i < crowdTintRendererBuffer.Count; i++)
        {
            Renderer renderer = crowdTintRendererBuffer[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(crowdTintPropertyBlock);
            crowdTintPropertyBlock.SetColor(BaseColorPropertyId, crowdTintOverrideColor);
            crowdTintPropertyBlock.SetColor(LegacyColorPropertyId, crowdTintOverrideColor);
            renderer.SetPropertyBlock(crowdTintPropertyBlock);
        }
        crowdTintRendererBuffer.Clear();
    }

    public static void ApplyEquippedSkinToActiveCrowds(int skinId)
    {
        PlayerCrowdManager[] managers = FindObjectsByType<PlayerCrowdManager>(FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null)
            {
                managers[i].ApplyEquippedSkinToCrowd(skinId);
            }
        }
    }

    private void ApplyEquippedSkinToCrowd(int skinId)
    {
        if (runnerPrefab == null)
        {
            return;
        }

        skinId = Mathf.Clamp(skinId, 0, SkinAvatarCatalog.SkinCount - 1);
        Animator playerAnimator = GetComponent<Animator>();
        RuntimeAnimatorController controller = playerAnimator != null
            ? playerAnimator.runtimeAnimatorController
            : cachedAnimatorController;

        ApplyEquippedSkinToRunner(runnerPrefab, skinId, controller);
        ApplyCrowdTint(runnerPrefab);

        for (int i = 0; i < runnerPool.Count; i++)
        {
            GameObject runner = runnerPool[i];
            if (runner == null)
            {
                continue;
            }

            ApplyEquippedSkinToRunner(runner, skinId, controller);
            ApplyCrowdTint(runner);
        }

        if (activeRunners.Count > 0 && activeRunners[0] != null)
        {
            Animator leadAnimator = GetRunnerAnimator(activeRunners[0]);
            if (leadAnimator != null)
            {
                for (int i = 0; i < activeRunners.Count; i++)
                {
                    GameObject runner = activeRunners[i];
                    if (runner == null || runner == activeRunners[0])
                    {
                        continue;
                    }

                    SyncAnimatorState(GetRunnerAnimator(runner), leadAnimator);
                }
            }
        }
    }

    public static void ApplyEquippedSkinToRunner(GameObject runner, int skinId, RuntimeAnimatorController animatorController = null)
    {
        if (runner == null)
        {
            return;
        }

        skinId = Mathf.Clamp(skinId, 0, SkinAvatarCatalog.SkinCount - 1);
        // Skin 00 uses the original runner already present on the prefab.
        if (skinId == 0)
        {
            RestoreDefaultRunnerAppearance(runner);
            return;
        }

        SkinAvatarCatalog catalog = GetSkinAvatarCatalog();
        if (catalog == null)
        {
            Debug.LogError("PlayerCrowdManager: Resources/SkinAvatarCatalog is missing; full model skins cannot be applied.", runner);
            return;
        }

        GameObject modelPrefab = catalog.GetModel(skinId);
        if (modelPrefab == null)
        {
            Debug.LogError("PlayerCrowdManager: SkinAvatarCatalog has no model assigned for skin ID " + skinId + ".", catalog);
            return;
        }

        Animator modelPrefabAnimator = modelPrefab.GetComponentInChildren<Animator>(true);
        if (modelPrefabAnimator == null || modelPrefabAnimator.avatar == null || !modelPrefabAnimator.avatar.isValid || !modelPrefabAnimator.avatar.isHuman)
        {
            Debug.LogError("PlayerCrowdManager: Skin " + skinId + " must contain a valid Humanoid Animator avatar.", modelPrefab);
            return;
        }

        string expectedName = EquippedSkinVisualPrefix + skinId.ToString("00");
        Transform existingVisual = FindEquippedSkinVisual(runner);
        if (existingVisual != null && existingVisual.name == expectedName)
        {
            ConfigureSkinAnimator(existingVisual, animatorController);
            return;
        }

        RuntimeAnimatorController controller = animatorController;
        if (controller == null)
        {
            Animator sourceAnimator = runner.GetComponent<Animator>();
            if (sourceAnimator == null)
            {
                sourceAnimator = runner.GetComponentInChildren<Animator>(true);
            }
            controller = sourceAnimator != null ? sourceAnimator.runtimeAnimatorController : null;
        }

        Renderer[] existingRenderers = runner.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < existingRenderers.Length; i++)
        {
            if (existingRenderers[i] != null)
            {
                existingRenderers[i].enabled = false;
            }
        }

        Animator[] existingAnimators = runner.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < existingAnimators.Length; i++)
        {
            if (existingAnimators[i] != null)
            {
                existingAnimators[i].enabled = false;
            }
        }

        for (int i = runner.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = runner.transform.GetChild(i);
            if (child.name.StartsWith(EquippedSkinVisualPrefix, StringComparison.Ordinal))
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        GameObject visual = Instantiate(modelPrefab, runner.transform, false);
        visual.name = expectedName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        ConfigureSkinAnimator(visual.transform, controller);
    }

    private static void RestoreDefaultRunnerAppearance(GameObject runner)
    {
        for (int i = runner.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = runner.transform.GetChild(i);
            if (child.name.StartsWith(EquippedSkinVisualPrefix, StringComparison.Ordinal))
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        Renderer[] renderers = runner.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !IsInsideEquippedSkinVisual(runner.transform, renderers[i].transform))
            {
                renderers[i].enabled = true;
            }
        }

        Animator[] animators = runner.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && !IsInsideEquippedSkinVisual(runner.transform, animators[i].transform))
            {
                animators[i].enabled = true;
            }
        }
    }

    private static bool IsInsideEquippedSkinVisual(Transform runnerRoot, Transform candidate)
    {
        for (Transform current = candidate; current != null && current != runnerRoot; current = current.parent)
        {
            if (current.name.StartsWith(EquippedSkinVisualPrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureSkinAnimator(Transform visual, RuntimeAnimatorController animatorController)
    {
        if (visual == null)
        {
            return;
        }

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            return;
        }

        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        animator.applyRootMotion = false;
        animator.enabled = true;
        if (animator.gameObject.activeInHierarchy)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private static SkinAvatarCatalog GetSkinAvatarCatalog()
    {
        if (!hasLoadedSkinAvatarCatalog)
        {
            cachedSkinAvatarCatalog = Resources.Load<SkinAvatarCatalog>(SkinAvatarCatalogResourcePath);
            hasLoadedSkinAvatarCatalog = true;
        }

        return cachedSkinAvatarCatalog;
    }

    private static Transform FindEquippedSkinVisual(GameObject runner)
    {
        if (runner == null)
        {
            return null;
        }

        for (int i = 0; i < runner.transform.childCount; i++)
        {
            Transform child = runner.transform.GetChild(i);
            if (child.gameObject.activeSelf && child.name.StartsWith(EquippedSkinVisualPrefix, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static Animator GetRunnerAnimator(GameObject runner)
    {
        Transform visual = FindEquippedSkinVisual(runner);
        return visual != null
            ? visual.GetComponentInChildren<Animator>(true)
            : runner != null ? runner.GetComponentInChildren<Animator>(true) : null;
    }

    private static SkinnedMeshRenderer GetRunnerRenderer(GameObject runner)
    {
        Transform visual = FindEquippedSkinVisual(runner);
        return visual != null
            ? visual.GetComponentInChildren<SkinnedMeshRenderer>(true)
            : runner != null ? runner.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
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
            cachedAnimatorAvatar = rootAnim.avatar;
            isStickmanAnimator = cachedAnimatorController.name.Contains("Stickman");
        }

        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (GetRunnerRenderer(child.gameObject) != null && !child.name.Contains("Camera"))
            {
                activeRunners.Add(child.gameObject);
                runnerActivations[child.gameObject] = ++nextRunnerActivation;
                runnerSpawnTimes[child.gameObject] = Time.time;

                if (cachedAnimatorController == null || cachedAnimatorAvatar == null)
                {
                    Animator startingAnim = GetRunnerAnimator(child.gameObject);
                    if (startingAnim != null)
                    {
                        if (cachedAnimatorController == null)
                        {
                            cachedAnimatorController = startingAnim.runtimeAnimatorController;
                        }
                        if (cachedAnimatorAvatar == null)
                        {
                            cachedAnimatorAvatar = startingAnim.avatar;
                        }
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
                hasTriggeredLeadFallGameOver = true;
                sfxPlayer?.PlayGameOver();
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.TriggerLeadFallGameOver(activeRunners.Count > 0 ? activeRunners[0].transform : transform);
                }
            }

            // Check 2: All logical runners have been lost
            if (LogicalRunnerCount == 0)
            {
                hasTriggeredGameOver = true;
                sfxPlayer?.PlayGameOver();
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

        UpdateJumpWaveHistory();
        RearrangeCrowd();
        HandleFallingRunners();
        UpdateLeadPlayerAnimation();
    }

    private void ResetJumpWave()
    {
        jumpWaveSampleHead = -1;
        jumpWaveSampleCount = 0;
        jumpWaveBaseY = transform.position.y;
        jumpWaveActive = false;
    }

    private void UpdateJumpWaveHistory()
    {
        bool isAirborne = playerController != null
            ? playerController.IsAirborne
            : playerCc != null && !playerCc.isGrounded;
        float currentY = transform.position.y;

        if (!isAirborne)
        {
            jumpWaveBaseY = currentY;
            jumpWaveActive = false;
            return;
        }

        if (!jumpWaveActive)
        {
            jumpWaveBaseY = currentY;
            jumpWaveSampleHead = -1;
            jumpWaveSampleCount = 0;
            jumpWaveActive = true;
        }

        jumpWaveSampleHead = (jumpWaveSampleHead + 1) % JUMP_WAVE_SAMPLE_CAPACITY;
        jumpWaveSampleTimes[jumpWaveSampleHead] = Time.time;
        jumpWaveSampleHeights[jumpWaveSampleHead] = Mathf.Max(0f, currentY - jumpWaveBaseY);
        jumpWaveSampleCount = Mathf.Min(jumpWaveSampleCount + 1, JUMP_WAVE_SAMPLE_CAPACITY);
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

        Animator leadAnim = GetRunnerAnimator(activeRunners[0]);
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
        PlayerController pc = playerController;
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

    /// <summary>
    /// Starts a bounded crossing session for one physical fracture. The lead visual
    /// is remembered by identity and excluded because the CharacterController owns
    /// the lead's real crossing; non-lead clones are classified independently.
    /// </summary>
    public void BeginRoadGap(int sessionId, float gapStartZ, float gapEndZ)
    {
        roadGapRunnerRecords.Clear();
        roadGapSessionId = sessionId;
        roadGapLead = activeRunners.Count > 0 ? activeRunners[0] : null;
        roadGapParticipantCount = 0;
        roadGapResolvedCount = 0;
        roadGapActive = true;
        roadGapComplete = false;

        for (int i = 1; i < activeRunners.Count; i++)
        {
            GameObject runner = activeRunners[i];
            if (runner == null) continue;

            roadGapRunnerRecords.Add(
                runner,
                new RoadGapRunnerRecord
                {
                    state = RoadGapRunnerState.SafeNearSide,
                    previousPosition = runner.transform.position
                });
            roadGapParticipantCount++;
        }

        roadGapComplete = roadGapParticipantCount == 0;
    }

    public bool IsRoadGapComplete(int sessionId)
    {
        return roadGapActive && roadGapSessionId == sessionId && roadGapComplete;
    }

    public void EndRoadGap(int sessionId)
    {
        if (!roadGapActive || roadGapSessionId != sessionId) return;
        ClearRoadGapSession();
    }

    public void CancelRoadGap(int sessionId)
    {
        EndRoadGap(sessionId);
    }

    /// <summary>
    /// Evaluates non-lead runners from actual world-space positions and the shared
    /// lead trajectory. It records failure only after a runner drops below the road;
    /// far-edge clearance or a grounded landing resolves a successful crossing.
    /// </summary>
    public int ProcessRoadGap(
        int sessionId,
        float gapStartZ,
        float gapEndZ,
        float roadSurfaceY,
        float farEdgeClearance,
        float landingTolerance)
    {
        if (!roadGapActive || roadGapSessionId != sessionId || roadGapComplete || LogicalRunnerCount <= 0)
        {
            return 0;
        }

        float startZ = Mathf.Min(gapStartZ, gapEndZ);
        float endZ = Mathf.Max(gapStartZ, gapEndZ);
        float surfaceY = roadSurfaceY;
        float clearance = Mathf.Max(0f, farEdgeClearance);
        float tolerance = Mathf.Max(0f, landingTolerance);
        float verticalVelocity = playerController != null ? playerController.VerticalVelocity : -2f;

        int fallenCount = 0;
        // Iterate the stable session identity map instead of activeRunners. A failed
        // visual can reduce the desired compressed visual count, and that cleanup
        // may remove another visual from activeRunners while this scan is running.
        // The session map is never structurally modified during the scan.
        foreach (KeyValuePair<GameObject, RoadGapRunnerRecord> entry in roadGapRunnerRecords)
        {
            GameObject runner = entry.Key;
            RoadGapRunnerRecord record = entry.Value;
            if (runner == null || runner == roadGapLead) continue;
            if (IsTerminalRoadGapState(record.state)) continue;

            Vector3 previousPosition = record.previousPosition;
            Vector3 currentPosition = runner.transform.position;
            record.previousPosition = currentPosition;

            if (record.state == RoadGapRunnerState.SafeNearSide)
            {
                if (currentPosition.z < startZ) continue;
                record.state = RoadGapRunnerState.OverGap;
            }

            if (currentPosition.z < endZ)
            {
                // A grounded runner can still follow the lead's jump over the far lip.
                // Defer failure until its actual position drops below the road.
                bool belowRoad = currentPosition.y < surfaceY - tolerance;
                if (belowRoad)
                {
                    if (MarkRoadGapState(record, RoadGapRunnerState.Failed))
                    {
                        MakeRunnerFall(runner);
                        fallenCount++;
                    }
                }

                continue;
            }
            float farEdgeY = GetCrossingHeight(previousPosition, currentPosition, endZ);
            bool crossedWithClearance = farEdgeY >= surfaceY + clearance;
            // A physics step can cross the lip by less than 0.25m.
            bool landedBeyondLip = currentPosition.z >= endZ &&
                currentPosition.y >= surfaceY - tolerance &&
                verticalVelocity <= 0f;

            if (crossedWithClearance || landedBeyondLip)
            {
                MarkRoadGapState(record, RoadGapRunnerState.SafeFarSide);
            }
            else if (MarkRoadGapState(record, RoadGapRunnerState.Failed))
            {
                MakeRunnerFall(runner);
                fallenCount++;
            }
        }

        roadGapComplete = roadGapResolvedCount >= roadGapParticipantCount;
        return fallenCount;
    }

    /// <summary>
    /// Compatibility overload for focused callers that only provide gap bounds.
    /// Level 5 uses the session-aware overload above.
    /// </summary>
    public int ProcessRoadGap(float gapStartZ, float gapEndZ)
    {
        const int compatibilitySessionId = 0;
        if (!roadGapActive || roadGapSessionId != compatibilitySessionId)
        {
            BeginRoadGap(compatibilitySessionId, gapStartZ, gapEndZ);
        }

        return ProcessRoadGap(
            compatibilitySessionId,
            gapStartZ,
            gapEndZ,
            lastGroundedY,
            0.15f,
            0.05f);
    }


    private static float GetCrossingHeight(Vector3 previousPosition, Vector3 currentPosition, float crossingZ)
    {
        float forwardDelta = currentPosition.z - previousPosition.z;
        if (previousPosition.z >= crossingZ || forwardDelta <= 0.0001f)
        {
            return currentPosition.y;
        }

        float t = Mathf.Clamp01((crossingZ - previousPosition.z) / forwardDelta);
        return Mathf.Lerp(previousPosition.y, currentPosition.y, t);
    }

    private static bool IsTerminalRoadGapState(RoadGapRunnerState state)
    {
        return state == RoadGapRunnerState.SafeFarSide || state == RoadGapRunnerState.Failed;
    }

    private bool MarkRoadGapState(RoadGapRunnerRecord record, RoadGapRunnerState newState)
    {
        if (record == null || IsTerminalRoadGapState(record.state)) return false;

        record.state = newState;
        roadGapResolvedCount++;
        if (roadGapResolvedCount >= roadGapParticipantCount)
        {
            roadGapComplete = true;
        }

        return true;
    }

    private void MarkRoadGapRunnerRemoved(GameObject runner)
    {
        if (!roadGapActive || runner == null) return;

        RoadGapRunnerRecord record;
        if (roadGapRunnerRecords.TryGetValue(runner, out record))
        {
            MarkRoadGapState(record, RoadGapRunnerState.Failed);
        }
    }

    private void ClearRoadGapSession()
    {
        roadGapRunnerRecords.Clear();
        roadGapLead = null;
        roadGapParticipantCount = 0;
        roadGapResolvedCount = 0;
        roadGapActive = false;
        roadGapComplete = false;
    }

    public void AddLogicalRunners(int amount)
    {
        if (amount <= 0) return;

        int previousCount = LogicalRunnerCount;
        long targetCount = (long)LogicalRunnerCount + amount;
        LogicalRunnerCount = targetCount > int.MaxValue ? int.MaxValue : (int)targetCount;
        SyncVisualCrowdToLogicalCount();
        if (LogicalRunnerCount > previousCount) sfxPlayer?.PlayCrowdGrowth();
    }

    public void MultiplyLogicalRunners(int factor)
    {
        int previousCount = LogicalRunnerCount;
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
        if (LogicalRunnerCount > previousCount) sfxPlayer?.PlayCrowdGrowth();
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

        sfxPlayer?.PlayCrowdLoss();
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
        sfxPlayer?.PlayGameOver();
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

        Animator anim = GetRunnerAnimator(runner);
        if (anim != null && cachedAnimatorController != null)
        {
            anim.runtimeAnimatorController = cachedAnimatorController;
            if (FindEquippedSkinVisual(runner) == null && cachedAnimatorAvatar != null)
            {
                anim.avatar = cachedAnimatorAvatar;
            }
            anim.Rebind();
            anim.Update(0f);
            if (activeRunners.Count > 0 && activeRunners[0] != null)
            {
                Animator leadAnim = GetRunnerAnimator(activeRunners[0]);
                if (leadAnim != null)
                {
                    SyncAnimatorState(anim, leadAnim);
                }
            }
        }

        // Preserve imported materials and apply the shared blue tint per renderer to every equipped skin.
        SkinShopController.ApplyEquippedSkin(runner);
        ApplyCrowdTint(runner);
        activeRunners.Add(runner);
        runnerActivations[runner] = ++nextRunnerActivation;
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

        if (TryPlayAnimationState(targetAnim, "Fast Run", stateInfo.normalizedTime))
        {
            return;
        }

        TryPlayAnimationState(targetAnim, "Running", stateInfo.normalizedTime);
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
        DeactivateVisualRunner(visualRunner);
        ApplyLogicalLoss(logicalLoss, false);

        // Removing a runner and its rounded logical share is gameplay state; the
        // death pop is optional presentation and must not interrupt that commit.
        try
        {
            DeathPopEffect.Create(effectPosition, effectColor);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("PlayerCrowdManager: death effect failed during hazard removal: " + exception.Message, this);
        }

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

        MarkRoadGapRunnerRemoved(runner);
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
        Renderer renderer = GetRunnerRenderer(runner);
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
            return GetRunnerAnimator(activeRunners[0]);
        }
        return null;
    }

    private void MakeRunnerFall(GameObject runner)
    {
        int logicalLoss = GetLogicalLossForVisualRunner();
        MarkRoadGapRunnerRemoved(runner);
        activeRunners.Remove(runner);
        runnerSpawnTimes.Remove(runner);
        edgeFrameCounters.Remove(runner);

        runner.transform.SetParent(null);
        fallingRunners.Add(runner);
        fallingRunnerVelocities[runner] = -2f;

        Animator cloneAnim = GetRunnerAnimator(runner);
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
            sfxPlayer?.PlayGameOver();
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
