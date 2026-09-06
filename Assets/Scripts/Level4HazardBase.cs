using System.Collections.Generic;
using UnityEngine;

public abstract class Level4HazardBase : MonoBehaviour
{
    [Header("Level 4 Hazard")]
    [SerializeField, Min(0f)] protected float warningLead = 1f;
    [SerializeField, Min(0)] protected int lossCap = 6;
    [SerializeField, Min(0f)] protected float hitPadding = 0.25f;

    protected PlayerController player;
    protected PlayerCrowdManager crowd;
    protected Transform telegraph;
    protected bool crossingResolved;

    protected virtual void Awake()
    {
        telegraph = transform.Find("Telegraph");
        if (telegraph != null)
        {
            telegraph.gameObject.SetActive(false);
        }
        CacheDependencies();
    }

    protected virtual void Start()
    {
        CacheDependencies();
    }

    protected virtual void OnEnable()
    {
        crossingResolved = false;
    }

    protected void CacheDependencies()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (crowd == null) crowd = FindFirstObjectByType<PlayerCrowdManager>();
    }

    protected bool CanProcess()
    {
        return player != null && crowd != null && !crowd.IsGameOver && UIManager.IsGameActive;
    }

    protected bool IsInLane(float centerX, float width)
    {
        return Mathf.Abs(player.transform.position.x - centerX) <= width * 0.5f + hitPadding;
    }

    protected int RemoveOverlappingRunners(float centerX, float width, float height, Color effectColor)
    {
        if (crowd == null || player == null) return 0;
        if (player.IsAirborne && player.HasCleared(height, 0f)) return 0;

        int removed = 0;
        float halfWidth = width * 0.5f + hitPadding;
        float halfDepth = 0.45f + hitPadding;
        float playerY = player.transform.position.y;
        List<GameObject> runners = crowd.ActiveRunners;

        for (int i = runners.Count - 1; i >= 0; i--)
        {
            GameObject runner = runners[i];
            if (runner == null) continue;

            Vector3 position = runner.transform.position;
            if (Mathf.Abs(position.x - centerX) > halfWidth) continue;
            if (Mathf.Abs(position.z - transform.position.z) > halfDepth) continue;
            if (position.y - playerY > height + hitPadding) continue;

            if (crowd.RemoveRunnerByHazard(runner, position + Vector3.up * 0.5f, effectColor))
            {
                removed++;
            }
        }

        return removed;
    }

    protected int RemoveCappedRunners(int cap, Vector3 impactPosition, Color effectColor)
    {
        if (crowd == null || cap <= 0) return 0;

        int removed = 0;
        for (int i = crowd.ActiveRunners.Count - 1; i >= 0 && removed < cap; i--)
        {
            GameObject runner = crowd.ActiveRunners[i];
            if (runner != null && crowd.RemoveRunnerByHazard(runner, impactPosition, effectColor))
            {
                removed++;
            }
        }

        return removed;
    }

    protected virtual void OnValidate()
    {
        warningLead = Mathf.Clamp(warningLead, 0f, 30f);
        lossCap = Mathf.Clamp(lossCap, 0, 35);
        hitPadding = Mathf.Clamp(hitPadding, 0f, 2f);
    }
}
