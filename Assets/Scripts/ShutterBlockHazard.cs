using UnityEngine;

public class ShutterBlockHazard : Level4HazardBase
{
    [SerializeField, Min(0.5f)] private float panelWidth = 1.8f;
    [SerializeField, Min(0.1f)] private float panelHeight = 1f;
    [SerializeField, Min(0.1f)] private float openDuration = 1.1f;
    [SerializeField, Min(0.05f)] private float closeDuration = 0.35f;
    [SerializeField, Min(0.1f)] private float raisedHold = 1.25f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField] private Color hazardColor = new Color(0.92f, 0.18f, 0.08f);

    private Transform visual;
    private float elapsed;
    private float openness;

    protected override void Awake()
    {
        base.Awake();
        visual = transform.Find("Visual");
        elapsed = phaseOffset;
        ApplyVisuals();
    }

    private void Update()
    {
        if (!CanProcess()) return;

        elapsed += Time.deltaTime;
        float cycle = openDuration + closeDuration + raisedHold;
        float t = Mathf.Repeat(elapsed, cycle);
        if (t < openDuration)
        {
            openness = 1f;
        }
        else if (t < openDuration + closeDuration)
        {
            openness = 1f - Mathf.InverseLerp(openDuration, openDuration + closeDuration, t);
        }
        else
        {
            openness = 0f;
        }

        ApplyVisuals();
        if (openness < 0.5f)
        {
            RemoveOverlappingRunners(transform.position.x, panelWidth, panelHeight, hazardColor);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        panelWidth = Mathf.Clamp(panelWidth, 0.5f, 3f);
        panelHeight = Mathf.Clamp(panelHeight, 0.1f, 1.25f);
        openDuration = Mathf.Clamp(openDuration, 0.1f, 4f);
        closeDuration = Mathf.Clamp(closeDuration, 0.05f, 2f);
        raisedHold = Mathf.Clamp(raisedHold, 0.1f, 4f);
        phaseOffset = Mathf.Max(0f, phaseOffset);
        if (!Application.isPlaying) ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (visual == null) visual = transform.Find("Visual");
        if (visual == null) return;
        visual.localScale = new Vector3(panelWidth, panelHeight, 0.9f);
        visual.localPosition = new Vector3(0f, Mathf.Lerp(panelHeight * 0.5f, -panelHeight * 0.5f, openness), 0f);
    }
}
