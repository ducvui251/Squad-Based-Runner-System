using UnityEngine;

public class JumpBarrierHazard : Level4HazardBase
{
    [SerializeField, Min(0.5f)] private float width = 1.8f;
    [SerializeField, Min(0.1f)] private float height = 0.85f;
    [SerializeField, Min(0f)] private float verticalClearanceMargin = 0.25f;
    [SerializeField] private Color hazardColor = new Color(1f, 0.25f, 0.08f);

    private Transform visual;

    protected override void Awake()
    {
        base.Awake();
        visual = transform.Find("Visual");
        ApplyVisuals();
    }

    private void Update()
    {
        if (!CanProcess() || crossingResolved) return;

        float crossingDepth = 0.45f + hitPadding;
        if (Mathf.Abs(player.transform.position.z - transform.position.z) > crossingDepth)
        {
            return;
        }

        if (player.HasCleared(height, verticalClearanceMargin))
        {
            crossingResolved = true;
            return;
        }

        RemoveOverlappingRunners(transform.position.x, width, height + verticalClearanceMargin, hazardColor);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        width = Mathf.Clamp(width, 0.5f, 9.5f);
        height = Mathf.Clamp(height, 0.1f, 1.25f);
        verticalClearanceMargin = Mathf.Clamp(verticalClearanceMargin, 0f, 0.75f);
        if (!Application.isPlaying) ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (visual == null) visual = transform.Find("Visual");
        if (visual == null) return;
        visual.localScale = new Vector3(width, height, 0.9f);
        visual.localPosition = new Vector3(0f, height * 0.5f, 0f);
    }
}
