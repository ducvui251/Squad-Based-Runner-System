using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private static bool gameActive;
    private static bool externalGameActive;

    public static bool IsGameActive
    {
        get => Instance == null ? externalGameActive : gameActive;
        private set => gameActive = value;
    }

    public static void SetExternalGameActive(bool active)
    {
        if (Instance == null)
        {
            externalGameActive = active;
        }
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject youWinPanel;

    [Header("HUD Elements")]
    [SerializeField] private TMP_Text cloneCountText;
    [SerializeField] private Slider progressBar;

    [Header("Game Over Elements")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button restartButton;

    [Header("You Win Elements")]
    [SerializeField] private TMP_Text winScoreText;

    [Header("Level Progress Settings")]
    [SerializeField] private float trackLength = 150f; // Distance from player start to level end

    private PlayerCrowdManager playerCrowd;
    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        IsGameActive = false;
        externalGameActive = false;
    }

    private void Start()
    {
        playerCrowd = FindFirstObjectByType<PlayerCrowdManager>();
        if (playerCrowd != null)
        {
            playerTransform = playerCrowd.transform;
        }

        // Initialize panel states
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (youWinPanel != null) youWinPanel.SetActive(false);

        // Bind restart button callback
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        // Tự động căn lề chỉnh sửa UI tránh bị lệch/mất chữ trên các màn hình khác nhau
        ConfigureLayoutResponsive();

        // Try to automatically find track length if there's a finish line or level end
        FindTrackLengthDynamic();
    }

    private void ConfigureLayoutResponsive()
    {
        if (cloneCountText != null)
        {
            RectTransform rect = cloneCountText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f); // Anchored to top-center
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -40f); // 40px down from the top edge
                rect.sizeDelta = new Vector2(300f, 60f);

                cloneCountText.text = "1"; // Giá trị mặc định ban đầu
                cloneCountText.alignment = TextAlignmentOptions.Center;
                cloneCountText.fontSize = 42f;
                cloneCountText.color = Color.white;
            }
        }

        if (progressBar != null)
        {
            RectTransform progressRect = progressBar.GetComponent<RectTransform>();
            if (progressRect != null)
            {
                progressRect.anchorMin = new Vector2(0.5f, 1f); // Anchored to top-center
                progressRect.anchorMax = new Vector2(0.5f, 1f);
                progressRect.pivot = new Vector2(0.5f, 1f);
                progressRect.anchoredPosition = new Vector2(0f, -110f); // 110px down from top (below clone count)
                progressRect.sizeDelta = new Vector2(400f, 25f);
            }
        }

        if (winScoreText != null)
        {
            RectTransform rect = winScoreText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(400f, 60f);

                winScoreText.alignment = TextAlignmentOptions.Center;
                winScoreText.fontSize = 32f;
                winScoreText.color = Color.white;
            }
        }
    }

    private void FindTrackLengthDynamic()
    {
        // Look for any objects that signify the end of the track to adjust trackLength automatically
        GameObject endMarker = GameObject.Find("FinishLine");
        if (endMarker == null)
        {
            endMarker = GameObject.Find("Finish");
        }

        if (endMarker != null)
        {
            trackLength = endMarker.transform.position.z;
        }
        else
        {
            // Scan for any object at a very far Z position that contains road or floor names
            float maxZ = 100f;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                string name = r.gameObject.name.ToLower();
                if (r.transform.position.z > maxZ && (name.Contains("road") || name.Contains("floor") || name.Contains("platform")))
                {
                    maxZ = r.transform.position.z;
                }
            }
            trackLength = maxZ;
        }

        Debug.Log("UIManager: Track length dynamically set to: " + trackLength);
    }

    private void Update()
    {
        if (!IsGameActive)
        {
            // Transition from Menu to Game on click/tap using new Input System
            bool clicked = false;
            
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                clicked = true;
            }

            if (clicked && mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                StartGame();
            }
            return;
        }

        // Update HUD elements during active gameplay
        UpdateHUD();
    }

    private void StartGame()
    {
        IsGameActive = true;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void UpdateHUD()
    {
        if (playerCrowd != null && cloneCountText != null)
        {
            int visualCount = playerCrowd.ActiveRunnerCount;
            cloneCountText.text = visualCount.ToString();
        }

        if (playerTransform != null && progressBar != null)
        {
            float progress = playerTransform.position.z / trackLength;
            progressBar.value = Mathf.Clamp01(progress);

            // Kiểm tra chiến thắng khi đi hết 99% chiều dài đường đua
            if (progress >= 0.99f && !gameOverTriggered)
            {
                gameOverTriggered = true;
                int finalScore = playerCrowd.ActiveRunnerCount;
                ShowYouWin(finalScore);
            }
        }
    }

    private bool gameOverTriggered = false; // Prevents double-triggering

    public void ShowGameOver(int finalScore)
    {
        // If the game is already inactive, don't show the panel again
        if (!IsGameActive)
        {
            return;
        }

        IsGameActive = false;
        Time.timeScale = 1f; // Restore normal time in case slow-mo was applied

        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (finalScoreText != null)
        {
            finalScoreText.text = "Final Score: " + finalScore;
        }
    }

    public void ShowYouWin(int finalScore)
    {
        if (!IsGameActive)
        {
            return;
        }

        IsGameActive = false;
        Time.timeScale = 1f; // Restore normal time in case slow-mo was applied

        if (hudPanel != null) hudPanel.SetActive(false);
        if (youWinPanel != null) youWinPanel.SetActive(true);

        if (winScoreText != null)
        {
            winScoreText.text = "Clones Saved: " + finalScore;
        }
    }


    public void RestartGame()
    {
        Time.timeScale = 1f; // Ensure time is restored before reload
        gameOverTriggered = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TriggerLeadFallGameOver(Transform leadTransform)
    {
        if (gameOverTriggered) return; // Prevent double-trigger
        gameOverTriggered = true;
        StartCoroutine(LeadFallRoutine(leadTransform));
    }

    private System.Collections.IEnumerator LeadFallRoutine(Transform leadTransform)
    {
        // Don't immediately stop the game — apply slow-motion for dramatic effect
        Time.timeScale = 0.5f;

        // Find Cinemachine Virtual Camera
        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null && leadTransform != null)
        {
            vcam.Follow = leadTransform;
            vcam.Target.TrackingTarget = leadTransform; // Set tracking target directly just in case

            float duration = 2f; // Real-time seconds (will feel longer with slow-mo)
            float elapsed = 0f;
            float startFov = vcam.Lens.FieldOfView;

            CinemachineOrbitalFollow orbitalFollow = vcam.GetComponent<CinemachineOrbitalFollow>();
            float startRadius = orbitalFollow != null ? orbitalFollow.Radius : 5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Lerp field of view to zoom in
                var lens = vcam.Lens;
                lens.FieldOfView = Mathf.Lerp(startFov, 25f, t);
                vcam.Lens = lens;

                // Lerp orbital follow radius to zoom in closer
                if (orbitalFollow != null)
                {
                    orbitalFollow.Radius = Mathf.Lerp(startRadius, 2f, t);
                }

                yield return null;
            }
        }
        else
        {
            // No vcam or lead transform — just wait briefly
            yield return new WaitForSecondsRealtime(1.5f);
        }

        // Now fully stop the game and show the panel
        Time.timeScale = 1f;
        ShowGameOver(0);
    }
}
