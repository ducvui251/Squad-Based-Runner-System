using UnityEngine;

public class SweepBeamHazard : Level4HazardBase
{
    [SerializeField] private float startX = -3.6f;
    [SerializeField] private float endX = 3.6f;
    [SerializeField, Min(0.25f)] private float beamHeight = 0.65f;
    [SerializeField, Min(0.1f)] private float beamWidth = 1.4f;
    [SerializeField, Min(0.25f)] private float travelDuration = 2.8f;
    [SerializeField, Min(0f)] private float endpointPause = 0.5f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField] private Color hazardColor = new Color(1f, 0.12f, 0.08f);

    private Transform beamVisual;
    private float elapsed;
    private float beamX;

    protected override void Awake()
    {
        base.Awake();
        beamVisual = transform.Find("Visual");
        elapsed = phaseOffset;
        ApplyVisuals();
    }

    private void Update()
    {
        if (!CanProcess()) return;

        elapsed += Time.deltaTime;
        beamX = EvaluateBeamX(elapsed);
        ApplyVisuals();
        RemoveOverlappingRunners(beamX, beamWidth, beamHeight, hazardColor);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        startX = Mathf.Clamp(startX, -4.25f, 4.25f);
        endX = Mathf.Clamp(endX, -4.25f, 4.25f);
        beamHeight = Mathf.Clamp(beamHeight, 0.25f, 1.1f);
        beamWidth = Mathf.Clamp(beamWidth, 0.5f, 2.5f);
        travelDuration = Mathf.Clamp(travelDuration, 0.25f, 8f);
        endpointPause = Mathf.Clamp(endpointPause, 0f, 2f);
        phaseOffset = Mathf.Max(0f, phaseOffset);
        if (!Application.isPlaying) ApplyVisuals();
    }

    private float EvaluateBeamX(float time)
    {
        float travel = Mathf.Max(0.25f, travelDuration);
        float pause = Mathf.Max(0f, endpointPause);
        float cycle = travel * 2f + pause * 2f;
        float t = Mathf.Repeat(time, cycle);
        if (t < travel) return Mathf.Lerp(startX, endX, t / travel);
        t -= travel;
        if (t < pause) return endX;
        t -= pause;
        if (t < travel) return Mathf.Lerp(endX, startX, t / travel);
        return startX;
    }

    private void ApplyVisuals()
    {
        if (beamVisual == null) beamVisual = transform.Find("Visual");
        if (beamVisual == null) return;
        beamVisual.localPosition = new Vector3(beamX, 0f, 0f);
        beamVisual.localScale = new Vector3(beamWidth, 0.25f, 0.9f);
    }
}
