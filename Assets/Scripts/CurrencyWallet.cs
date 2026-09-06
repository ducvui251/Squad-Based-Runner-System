using UnityEngine;

public class CurrencyWallet : MonoBehaviour
{
    public static CurrencyWallet Instance { get; private set; }

    [Header("Persistent Wallet Balance")]
    [SerializeField] private int walletCoins;

    [Header("Current Playthrough Coins")]
    [SerializeField] private int runCoins;

    public int WalletCoins => walletCoins;
    public int RunCoins => runCoins;

    // Compatibility alias for existing HUD/gameplay code. This is the current run counter.
    public int Currency => runCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetRunCoins();
    }

    public void Add(int amount)
    {
        AddRunCoins(amount);
    }

    public void AddRunCoins(int amount)
    {
        if (amount <= 0) return;

        runCoins += amount;
    }

    public void ResetRunCoins()
    {
        runCoins = 0;
    }

    public void BankRunCoins()
    {
        if (runCoins <= 0) return;

        walletCoins += runCoins;
        runCoins = 0;
    }

    public void AddWalletCoins(int amount)
    {
        if (amount <= 0) return;

        walletCoins += amount;
    }

    public bool SpendWalletCoins(int amount)
    {
        if (amount <= 0) return true;
        if (walletCoins < amount) return false;

        walletCoins -= amount;
        return true;
    }
}
