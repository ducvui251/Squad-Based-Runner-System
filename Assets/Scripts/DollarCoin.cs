using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DollarCoin : MonoBehaviour
{
    public enum CoinSize
    {
        Small,
        Medium,
        Large
    }

    [SerializeField] private CoinSize coinSize = CoinSize.Small;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private int smallCoinValue = 5;
    [SerializeField] private int mediumCoinValue = 20;
    [SerializeField] private int largeCoinValue = 50;
    [SerializeField] private float smallCoinScale = 1f;
    [SerializeField] private float chimeFrequency = 1320f;
    [SerializeField] private float chimeDuration = 0.08f;

    private Collider coinCollider;
    private bool collected;

    public int CurrencyValue
    {
        get
        {
            switch (coinSize)
            {
                case CoinSize.Medium:
                    return mediumCoinValue;
                case CoinSize.Large:
                    return largeCoinValue;
                default:
                    return smallCoinValue;
            }
        }
    }

    private void Awake()
    {
        coinCollider = GetComponent<Collider>();
        coinCollider.isTrigger = true;
        ApplySizeScale();
    }

    private void OnValidate()
    {
        ApplySizeScale();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (!IsPlayerCrowdCollider(other))
        {
            return;
        }

        collected = true;

        CurrencyWallet wallet = CurrencyWallet.Instance;
        if (wallet != null)
        {
            wallet.AddRunCoins(CurrencyValue);
        }

        PlayChime();
        Destroy(gameObject);
    }

    private bool IsPlayerCrowdCollider(Collider other)
    {
        if (other.GetComponentInParent<PlayerCrowdManager>() != null) return true;
        if (other.GetComponentInParent<PlayerController>() != null) return true;
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void ApplySizeScale()
    {
        float xyScale = smallCoinScale;
        if (coinSize == CoinSize.Medium)
        {
            xyScale *= 1.5f;
        }
        else if (coinSize == CoinSize.Large)
        {
            xyScale *= 2.25f;
        }

        Vector3 scale = transform.localScale;
        scale.x = xyScale;
        scale.z = xyScale;
        transform.localScale = scale;
    }

    private void PlayChime()
    {
        AudioClip clip = AudioClip.Create("DollarCoinChime", Mathf.CeilToInt(44100 * chimeDuration), 1, 44100, false);
        float[] samples = new float[clip.samples];

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / 44100f;
            float fade = 1f - (i / (float)samples.Length);
            samples[i] = Mathf.Sin(2f * Mathf.PI * chimeFrequency * t) * fade * 0.35f;
        }

        clip.SetData(samples, 0);
        AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
    }
}
