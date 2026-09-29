using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public sealed class TankBoss : MonoBehaviour
{
    private const string FireTrigger = "Fire";

    [Header("Boss Rules")]
    [SerializeField, Min(1)] private int minimumPlayersToDefeat = 60;
    [SerializeField, Min(1)] private int clonesKilledPerVolley = 15;
    [SerializeField, Min(1)] private int volleysToDefeat = 3;
    [SerializeField, Min(0.1f)] private float secondsBetweenShots = 1f;

    [Header("Presentation")]
    [SerializeField] private Animator firingAnimator;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private TMP_Text requirementLabel;
    [SerializeField] private Image healthBarFill;

    private PlayerCrowdManager crowdManager;
    private Collider encounterTrigger;
    private bool encounterStarted;
    private bool canDefeatBoss;
    private bool bossDefeated;
    private int volleysFired;
    private float nextShotTime;
    private Canvas healthBarCanvas;
    private Camera healthBarCamera;

    private void Awake()
    {
        if (firingAnimator == null) firingAnimator = GetComponent<Animator>();
        if (muzzleFlash == null) muzzleFlash = GetComponentInChildren<ParticleSystem>(true);
        encounterTrigger = GetComponent<Collider>();

        if (muzzleFlash != null)
        {
            ParticleSystemRenderer particleRenderer = muzzleFlash.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 0.025f);
            }
        }

        if (requirementLabel != null)
        {
            requirementLabel.text = "FINAL BOSS\n60 RUNNERS REQUIRED";
        }

        if (healthBarFill != null)
        {
            healthBarCanvas = healthBarFill.GetComponentInParent<Canvas>();
            healthBarFill.fillAmount = 1f;
        }

        healthBarCamera = Camera.main;
    }

    private void OnValidate()
    {
        minimumPlayersToDefeat = Mathf.Max(1, minimumPlayersToDefeat);
        clonesKilledPerVolley = Mathf.Max(1, clonesKilledPerVolley);
        volleysToDefeat = Mathf.Max(1, volleysToDefeat);
        secondsBetweenShots = Mathf.Max(0.1f, secondsBetweenShots);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBeginEncounter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBeginEncounter(other);
    }

    private void Update()
    {
        if (!encounterStarted || bossDefeated || crowdManager == null) return;

        if (crowdManager.LogicalRunnerCount <= 0)
        {
            encounterStarted = false;
            crowdManager.EndBossFight();
            return;
        }

        if (Time.time < nextShotTime) return;

        nextShotTime = Time.time + secondsBetweenShots;
        FireVolley();
    }

    private void LateUpdate()
    {
        if (healthBarCanvas == null || !healthBarCanvas.gameObject.activeInHierarchy) return;
        if (healthBarCamera == null) healthBarCamera = Camera.main;
        if (healthBarCamera == null) return;

        Transform canvasTransform = healthBarCanvas.transform;
        Vector3 toCamera = healthBarCamera.transform.position - canvasTransform.position;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            canvasTransform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }
    }

    private void TryBeginEncounter(Collider other)
    {
        if (encounterStarted || bossDefeated || other == null) return;

        PlayerCrowdManager playerCrowd = other.GetComponentInParent<PlayerCrowdManager>();
        if (playerCrowd == null || playerCrowd.LogicalRunnerCount <= 0) return;

        crowdManager = playerCrowd;
        canDefeatBoss = crowdManager.LogicalRunnerCount >= minimumPlayersToDefeat;
        volleysFired = 0;
        encounterStarted = true;
        nextShotTime = Time.time + secondsBetweenShots;
        if (healthBarCanvas != null) healthBarCanvas.gameObject.SetActive(true);
        UpdateHealthBar();
        crowdManager.BeginBossFight(transform.position.x);
    }

    private void FireVolley()
    {
        crowdManager.PlayTankShotSfx();

        if (firingAnimator != null)
        {
            firingAnimator.ResetTrigger(FireTrigger);
            firingAnimator.SetTrigger(FireTrigger);
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play(true);
        }

        var runners = crowdManager.ActiveRunners;
        for (int removed = 0; removed < clonesKilledPerVolley && crowdManager.LogicalRunnerCount > 0; removed++)
        {
            GameObject runner = FindLastActiveRunner(runners);
            if (runner == null) break;

            Vector3 effectPosition = runner.transform.position + Vector3.up * 0.5f;
            if (!crowdManager.RemoveRunnerByHazard(runner, effectPosition, new Color(1f, 0.25f, 0.1f)))
            {
                break;
            }
        }

        if (!canDefeatBoss || crowdManager.LogicalRunnerCount <= 0) return;

        volleysFired++;
        UpdateHealthBar();
        if (volleysFired >= volleysToDefeat)
        {
            DefeatBoss();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        healthBarFill.fillAmount = Mathf.Clamp01(1f - (float)volleysFired / volleysToDefeat);
    }

    private static GameObject FindLastActiveRunner(System.Collections.Generic.List<GameObject> runners)
    {
        for (int i = runners.Count - 1; i >= 0; i--)
        {
            if (runners[i] != null) return runners[i];
        }

        return null;
    }

    private void DefeatBoss()
    {
        bossDefeated = true;
        encounterStarted = false;
        if (healthBarFill != null) healthBarFill.fillAmount = 0f;
        if (encounterTrigger != null) encounterTrigger.enabled = false;
        if (requirementLabel != null) requirementLabel.text = "BOSS DEFEATED";
        if (crowdManager != null) crowdManager.EndBossFight();
        Destroy(gameObject);
    }
}
