using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// World-space crowd counter badge. It can use a prefab from Resources/UI/CounterBadge,
/// and falls back to procedural visuals when that prefab is missing.
/// </summary>
public class CountBadge : MonoBehaviour
{
    public static readonly Color PlayerBlue = new Color(0f, 0.729f, 1f);
    public static readonly Color EnemyRed = new Color(1f, 0.27f, 0.2f);

    [Header("Badge Settings")]
    [SerializeField] private float heightAbove = 1f;
    [SerializeField] private float heightFactor = 0.22f;
    [SerializeField] private float maxAdaptiveHeight = 1.4f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationLerpSpeed = 20f;
    [SerializeField] private float fontSize = 4.8f;
    [SerializeField] private float minBubbleWidthScale = 1.15f;
    [SerializeField] private float paddingScale = 0.1f;
    [SerializeField] private float backgroundAlpha = 0.92f;
    [SerializeField] private float shadowAlpha = 0.18f;
    [SerializeField] private Color badgeColor = Color.white;

    private TMP_Text label;
    private SpriteRenderer background;
    private SpriteRenderer shadow;
    private SpriteRenderer tailRenderer;
    private Transform host;
    private Func<int> getCount;
    private bool trackChildren;
    private bool selfDestructWhenZero;
    private Func<Transform> getAnchor;
    private Vector3 smoothVelocity;
    private bool hasSmoothedPosition;
    private PlayerCrowdManager playerCrowd;

    private static Sprite pillSprite;
    private static float baseCapsuleWidth;
    private static float baseCapsuleHeight;

    private struct CrowdMetrics
    {
        public Vector3 Center;
        public float TopY;
        public float HorizontalRadius;
        public int Count;
    }

    public static CountBadge Attach(
        Transform host,
        Func<int> getCount,
        Color color,
        bool trackChildren = false,
        bool selfDestructWhenZero = false,
        float heightAbove = 2f,
        Func<Transform> getAnchor = null)
    {
        if (host == null || getCount == null) return null;

        Transform existingBadge = host.Find("CountBadge");
        GameObject badgePrefab = existingBadge == null ? Resources.Load<GameObject>("UI/CounterBadge") : null;
        GameObject root = existingBadge != null
            ? existingBadge.gameObject
            : (badgePrefab != null ? Instantiate(badgePrefab) : new GameObject("CountBadge"));
        root.name = "CountBadge";
        root.transform.SetParent(host, false);

        CountBadge badge = root.GetComponent<CountBadge>();
        if (badge == null)
        {
            badge = root.AddComponent<CountBadge>();
        }

        badge.host = host;
        badge.getCount = getCount;
        badge.trackChildren = trackChildren;
        badge.selfDestructWhenZero = selfDestructWhenZero;
        badge.heightAbove = heightAbove;
        badge.getAnchor = getAnchor;
        badge.playerCrowd = host.GetComponent<PlayerCrowdManager>();
        badge.badgeColor = new Color(color.r, color.g, color.b, badge.backgroundAlpha);
        badge.hasSmoothedPosition = false;
        badge.smoothVelocity = Vector3.zero;

        bool hasVisualChildren = root.transform.Find("Background") != null || root.transform.Find("Label") != null;
        if (badgePrefab != null || hasVisualChildren)
        {
            badge.WirePrefabVisuals();
        }
        else
        {
            badge.BuildProceduralVisuals();
        }

        badge.ApplyColorToVisuals();
        return badge;
    }

    private void WirePrefabVisuals()
    {
        Transform bgTransform = transform.Find("Background");
        Transform shadowTransform = transform.Find("Shadow");
        Transform labelTransform = transform.Find("Label");
        Transform tailTransform = transform.Find("Tail");

        background = bgTransform != null ? bgTransform.GetComponent<SpriteRenderer>() : null;
        shadow = shadowTransform != null ? shadowTransform.GetComponent<SpriteRenderer>() : null;
        tailRenderer = tailTransform != null ? tailTransform.GetComponent<SpriteRenderer>() : null;
        label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;

        if (label == null)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            label = labelObj.AddComponent<TextMeshPro>();
        }

        ConfigureLabel();
        CacheBackgroundBaseSize();
    }

    private void BuildProceduralVisuals()
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);
        background = bgObj.AddComponent<SpriteRenderer>();
        background.sprite = GetPillSprite();

        float shadowOffset = fontSize * (0.07f / 8f);
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(transform, false);
        shadowObj.transform.localPosition = new Vector3(0f, -shadowOffset, 0.02f);
        shadow = shadowObj.AddComponent<SpriteRenderer>();
        shadow.sprite = GetPillSprite();

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform, false);
        label = labelObj.AddComponent<TextMeshPro>();
        ConfigureLabel();
        CacheBackgroundBaseSize();
    }

    private void ConfigureLabel()
    {
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        label.alignment = TextAlignmentOptions.Center;
        label.autoSizeTextContainer = true;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.fontWeight = FontWeight.Black;
        label.color = Color.white;
        label.outlineWidth = fontSize * (0.12f / 8f);
        label.outlineColor = new Color(0.05f, 0.08f, 0.12f, 1f);
    }

    private void ApplyColorToVisuals()
    {
        if (background != null)
        {
            background.color = badgeColor;
            background.sortingOrder = -1;
        }

        if (tailRenderer != null)
        {
            tailRenderer.color = badgeColor;
            tailRenderer.sortingOrder = -1;
        }

        if (shadow != null)
        {
            shadow.color = new Color(0f, 0f, 0f, shadowAlpha);
            shadow.sortingOrder = -2;
        }
    }

    private void CacheBackgroundBaseSize()
    {
        if (background == null || background.sprite == null) return;

        Vector2 spriteSize = background.sprite.bounds.size;
        Vector3 scale = background.transform.localScale;
        baseCapsuleWidth = Mathf.Max(0.0001f, spriteSize.x * Mathf.Abs(scale.x));
        baseCapsuleHeight = Mathf.Max(0.0001f, spriteSize.y * Mathf.Abs(scale.y));
    }

    private static Sprite GetPillSprite()
    {
        if (pillSprite != null) return pillSprite;

        const int W = 256;
        const int H = 160;
        const float cx = W * 0.5f;
        const float cy = H * 0.67f;
        const float halfW = W * 0.5f;
        const float halfH = H * 0.33f;
        const float radius = halfH;
        const float tailTop = cy - halfH;
        const float tailHalfWidth = 16f;
        const float tailApexY = tailTop - 14f;

        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.name = "CountBadgePill";
        Color[] pixels = new Color[W * H];

        const int AA = 8;
        const float samples = AA * AA;

        for (int py = 0; py < H; py++)
        {
            for (int px = 0; px < W; px++)
            {
                int hits = 0;
                for (int sy = 0; sy < AA; sy++)
                {
                    for (int sx = 0; sx < AA; sx++)
                    {
                        float x = px + (sx + 0.5f) / AA;
                        float y = py + (sy + 0.5f) / AA;
                        if (InsidePillOrTail(x, y, cx, cy, halfW, halfH, radius, tailTop, tailHalfWidth, tailApexY))
                        {
                            hits++;
                        }
                    }
                }

                pixels[py * W + px] = new Color(1f, 1f, 1f, hits / samples);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        pillSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.67f), 100f);
        baseCapsuleWidth = W / 100f;
        baseCapsuleHeight = (2f * halfH) / 100f;
        return pillSprite;
    }

    private static bool InsidePillOrTail(float x, float y, float cx, float cy, float halfW, float halfH, float radius, float tailTop, float tailHalfWidth, float tailApexY)
    {
        float dx = Mathf.Abs(x - cx) - (halfW - radius);
        float dy = Mathf.Abs(y - cy) - (halfH - radius);
        float sdf = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
                    + Mathf.Min(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)) - radius;
        if (sdf <= 0f) return true;

        return PointInTriangle(x, y, cx - tailHalfWidth, tailTop, cx + tailHalfWidth, tailTop, cx, tailApexY);
    }

    private static bool PointInTriangle(float px, float py, float ax, float ay, float bx, float by, float cxx, float cyy)
    {
        float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
        float d2 = (px - cxx) * (by - cyy) - (bx - cxx) * (py - cyy);
        float d3 = (px - ax) * (cyy - ay) - (cxx - ax) * (py - ay);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    private void Update()
    {
        if (host == null)
        {
            Destroy(gameObject);
            return;
        }

        int count = getCount != null ? getCount() : 0;
        if (selfDestructWhenZero && count <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (label != null)
        {
            string newText = count.ToString();
            if (label.text != newText)
            {
                label.text = newText;
                label.ForceMeshUpdate();
                SizeBackgroundToText();
            }
        }

        CrowdMetrics metrics = CalculateCrowdMetrics();
        float adaptiveHeight = Mathf.Min(metrics.HorizontalRadius * heightFactor, maxAdaptiveHeight);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude * (fontSize / 8f);
        Vector3 targetPosition = new Vector3(metrics.Center.x, metrics.TopY + heightAbove + adaptiveHeight + bob, metrics.Center.z);

        if (!hasSmoothedPosition)
        {
            transform.position = targetPosition;
            hasSmoothedPosition = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref smoothVelocity, positionSmoothTime);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
        }
    }

    private CrowdMetrics CalculateCrowdMetrics()
    {
        if (getAnchor != null)
        {
            Transform anchor = getAnchor();
            Vector3 anchorPosition = anchor != null ? anchor.position : host.position;
            return new CrowdMetrics { Center = anchorPosition, TopY = anchorPosition.y, HorizontalRadius = 0f, Count = anchor != null ? 1 : 0 };
        }

        List<Transform> tracked = CollectTrackedTransforms();
        if (tracked.Count == 0)
        {
            return new CrowdMetrics { Center = host.position, TopY = host.position.y, HorizontalRadius = 0f, Count = 0 };
        }

        Vector3 sum = Vector3.zero;
        float topY = float.MinValue;
        for (int i = 0; i < tracked.Count; i++)
        {
            Vector3 p = tracked[i].position;
            sum += p;
            topY = Mathf.Max(topY, GetTopY(tracked[i]));
        }

        Vector3 center = sum / tracked.Count;
        float radius = 0f;
        for (int i = 0; i < tracked.Count; i++)
        {
            Vector3 p = tracked[i].position;
            Vector2 delta = new Vector2(p.x - center.x, p.z - center.z);
            radius = Mathf.Max(radius, delta.magnitude);
        }

        return new CrowdMetrics { Center = center, TopY = topY, HorizontalRadius = radius, Count = tracked.Count };
    }

    private List<Transform> CollectTrackedTransforms()
    {
        List<Transform> tracked = new List<Transform>();

        if (playerCrowd != null)
        {
            List<GameObject> runners = playerCrowd.ActiveRunners;
            for (int i = 0; i < runners.Count; i++)
            {
                if (runners[i] != null && runners[i].activeInHierarchy)
                {
                    tracked.Add(runners[i].transform);
                }
            }
            return tracked;
        }

        if (!trackChildren)
        {
            tracked.Add(host);
            return tracked;
        }

        for (int i = 0; i < host.childCount; i++)
        {
            Transform child = host.GetChild(i);
            if (child == transform) continue;
            if (!child.gameObject.activeInHierarchy) continue;
            if (child.GetComponent<CountBadge>() != null) continue;

            Enemy enemy = child.GetComponent<Enemy>();
            if (enemy != null && enemy.IsDead) continue;

            tracked.Add(child);
        }

        return tracked;
    }

    private static float GetTopY(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasRenderer = false;
        float topY = target.position.y;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (renderer.GetComponentInParent<CountBadge>() != null) continue;
            topY = hasRenderer ? Mathf.Max(topY, renderer.bounds.max.y) : renderer.bounds.max.y;
            hasRenderer = true;
        }

        return topY;
    }

    private void SizeBackgroundToText()
    {
        if (background == null || label == null) return;
        CacheBackgroundBaseSize();
        if (baseCapsuleWidth <= 0.0001f || baseCapsuleHeight <= 0.0001f) return;

        Vector3 textSize = ComputeTextMeshBounds(label);
        if (textSize.x <= 0.001f) textSize.x = 0.5f;
        if (textSize.y <= 0.001f) textSize.y = 0.7f;

        float pad = textSize.y * paddingScale;
        float bubbleHeight = textSize.y + pad * 2f;
        float minWidth = bubbleHeight * minBubbleWidthScale;
        float bubbleWidth = Mathf.Max(minWidth, textSize.x + pad * 2f);

        Vector3 scale = new Vector3(bubbleWidth / Mathf.Max(0.0001f, background.sprite.bounds.size.x), bubbleHeight / Mathf.Max(0.0001f, background.sprite.bounds.size.y), 1f);
        background.transform.localScale = scale;

        if (tailRenderer != null)
        {
            tailRenderer.color = badgeColor;
        }

        if (shadow != null)
        {
            shadow.transform.localScale = scale;
        }
    }

    private static Vector3 ComputeTextMeshBounds(TMP_Text label)
    {
        if (label.textInfo == null || label.textInfo.meshInfo.Length == 0) return Vector3.zero;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        bool hasVertices = false;

        for (int m = 0; m < label.textInfo.meshInfo.Length; m++)
        {
            TMP_MeshInfo mi = label.textInfo.meshInfo[m];
            for (int i = 0; i < mi.vertexCount; i++)
            {
                Vector3 v = mi.vertices[i];
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
                hasVertices = true;
            }
        }

        return hasVertices ? max - min : Vector3.zero;
    }
}
