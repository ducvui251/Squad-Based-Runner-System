using UnityEngine;

public class RisingBlockHazard : Level4HazardBase
{
    [SerializeField, Min(0.5f)] private float width = 1.8f;
    [SerializeField, Min(0.1f)] private float raisedHeight = 1.05f;
    [SerializeField, Min(0.1f)] private float cycleDuration = 3.2f;
    [SerializeField, Min(0.05f)] private float riseDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float raisedHold = 1.1f;
    [SerializeField, Min(0.05f)] private float lowerDuration = 0.45f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField] private Color hazardColor = new Color(1f, 0.42f, 0.08f);

    private Transform visual;
    private float elapsed;
    private float raised01;

    protected override void Awake()
    {
        base.Awake();
        visual = transform.Find("Visual");
        ApplyVisuals();
        elapsed = phaseOffset;
    }

    private void Update()
    {
        if (!CanProcess()) return;

        elapsed += Time.deltaTime;
        float cycle = Mathf.Max(cycleDuration, riseDuration + raisedHold + lowerDuration + 0.1f);
        float time = Mathf.Repeat(elapsed, cycle);
        float retractedDuration = cycle - riseDuration - raisedHold - lowerDuration;
        if (time < retractedDuration)
        {
            raised01 = 0f;
        }
        else if (time < retractedDuration + riseDuration)
        {
            raised01 = Mathf.InverseLerp(retractedDuration, retractedDuration + riseDuration, time);
        }
        else if (time < retractedDuration + riseDuration + raisedHold)
        {
            raised01 = 1f;
        }
        else
        {
            raised01 = 1f - Mathf.InverseLerp(retractedDuration + riseDuration + raisedHold, cycle, time);
        }

        ApplyVisuals();
        if (raised01 > 0.75f)
        {
            RemoveOverlappingRunners(transform.position.x, width, raisedHeight, hazardColor);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        width = Mathf.Clamp(width, 0.5f, 5f);
        raisedHeight = Mathf.Clamp(raisedHeight, 0.1f, 1.25f);
        cycleDuration = Mathf.Clamp(cycleDuration, 0.5f, 12f);
        riseDuration = Mathf.Clamp(riseDuration, 0.05f, cycleDuration * 0.5f);
        raisedHold = Mathf.Clamp(raisedHold, 0.05f, cycleDuration * 0.75f);
        lowerDuration = Mathf.Clamp(lowerDuration, 0.05f, cycleDuration * 0.5f);
        phaseOffset = Mathf.Repeat(phaseOffset, Mathf.Max(0.1f, cycleDuration));
        if (!Application.isPlaying) ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (visual == null) visual = transform.Find("Visual");
        if (visual == null) return;
        visual.localScale = new Vector3(width, raisedHeight, 0.9f);
        visual.localPosition = new Vector3(0f, Mathf.Lerp(-raisedHeight * 0.5f, raisedHeight * 0.5f, raised01), 0f);
    }
}
