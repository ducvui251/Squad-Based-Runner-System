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

        PlayerCrowdManager playerCrowdManager = other.GetComponentInParent<PlayerCrowdManager>();
        if (!IsPlayerCrowdCollider(other, playerCrowdManager))
        {
            return;
        }

        collected = true;

        CurrencyWallet wallet = CurrencyWallet.Instance;
        if (wallet != null)
        {
            wallet.AddRunCoins(CurrencyValue);
        }

        if (playerCrowdManager != null) playerCrowdManager.PlayCoinPickupSfx();
        Destroy(gameObject);
    }

    private bool IsPlayerCrowdCollider(Collider other, PlayerCrowdManager playerCrowdManager)
    {
        if (playerCrowdManager != null) return true;
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

}
