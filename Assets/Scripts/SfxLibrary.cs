using UnityEngine;

[CreateAssetMenu(fileName = "SfxLibrary", menuName = "SpiralSquad/Audio/SFX Library")]
public sealed class SfxLibrary : ScriptableObject
{
    [Header("Clips")]
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip coinPickup;
    [SerializeField] private AudioClip crowdGrowth;
    [SerializeField] private AudioClip crowdLoss;
    [SerializeField] private AudioClip gameOver;
    [SerializeField] private AudioClip finish;
    [SerializeField] private AudioClip tankShot;

    [Header("Gains")]
    [SerializeField, Range(0f, 1f)] private float uiClickGain = 1f;
    [SerializeField, Range(0f, 1f)] private float coinPickupGain = 1f;
    [SerializeField, Range(0f, 1f)] private float crowdGrowthGain = 1f;
    [SerializeField, Range(0f, 1f)] private float crowdLossGain = 1f;
    [SerializeField, Range(0f, 1f)] private float gameOverGain = 1f;
    [SerializeField, Range(0f, 1f)] private float finishGain = 1f;
    [SerializeField, Range(0f, 1f)] private float tankShotGain = 0.55f;

    public AudioClip UiClick => uiClick;
    public AudioClip CoinPickup => coinPickup;
    public AudioClip CrowdGrowth => crowdGrowth;
    public AudioClip CrowdLoss => crowdLoss;
    public AudioClip GameOver => gameOver;
    public AudioClip Finish => finish;
    public AudioClip TankShot => tankShot;

    public float UiClickGain => uiClickGain;
    public float CoinPickupGain => coinPickupGain;
    public float CrowdGrowthGain => crowdGrowthGain;
    public float CrowdLossGain => crowdLossGain;
    public float GameOverGain => gameOverGain;
    public float FinishGain => finishGain;
    public float TankShotGain => tankShotGain;

    private void OnValidate()
    {
        uiClickGain = Mathf.Clamp01(uiClickGain);
        coinPickupGain = Mathf.Clamp01(coinPickupGain);
        crowdGrowthGain = Mathf.Clamp01(crowdGrowthGain);
        crowdLossGain = Mathf.Clamp01(crowdLossGain);
        gameOverGain = Mathf.Clamp01(gameOverGain);
        finishGain = Mathf.Clamp01(finishGain);
        tankShotGain = Mathf.Clamp01(tankShotGain);
    }
}
