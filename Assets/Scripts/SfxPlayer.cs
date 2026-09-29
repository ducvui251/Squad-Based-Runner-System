using UnityEngine;
using UnityEngine.UI;

public sealed class SfxPlayer : MonoBehaviour
{
    [SerializeField] private SfxLibrary library;
    [SerializeField] private AudioSource[] voicePool = new AudioSource[4];
    [SerializeField] private Button[] uiButtons;
    [SerializeField, Min(0f)] private float repeatedCrowdLossCooldown = 0.15f;

    private readonly Button[] runtimeButtons = new Button[32];
    private int nextVoiceIndex;
    private float lastCrowdLossTime = float.NegativeInfinity;
    private bool hasPlayedGameOver;
    private bool hasPlayedFinish;

    private void Awake()
    {
        if (voicePool == null) return;

        for (int i = 0; i < voicePool.Length; i++)
        {
            if (voicePool[i] != null)
            {
                voicePool[i].ignoreListenerVolume = false;
            }
        }
    }

    private void OnEnable()
    {
        HookUiButtons(true);
        HookRuntimeButtons(true);
    }

    private void OnDisable()
    {
        HookUiButtons(false);
        HookRuntimeButtons(false);
    }

    private void OnValidate()
    {
        repeatedCrowdLossCooldown = Mathf.Max(0f, repeatedCrowdLossCooldown);
    }

    public void PlayUiClick()
    {
        if (library != null) Play(library.UiClick, library.UiClickGain);
    }

    public void PlayCoinPickup()
    {
        if (library != null) Play(library.CoinPickup, library.CoinPickupGain);
    }

    public void PlayCrowdGrowth()
    {
        if (library != null) Play(library.CrowdGrowth, library.CrowdGrowthGain);
    }

    public void PlayCrowdLoss()
    {
        if (Time.unscaledTime - lastCrowdLossTime < repeatedCrowdLossCooldown) return;
        lastCrowdLossTime = Time.unscaledTime;

        if (library != null) Play(library.CrowdLoss, library.CrowdLossGain);
    }

    public void PlayGameOver()
    {
        if (hasPlayedGameOver) return;
        hasPlayedGameOver = true;

        if (library != null) Play(library.GameOver, library.GameOverGain);
    }

    public void PlayFinish()
    {
        if (hasPlayedFinish) return;
        hasPlayedFinish = true;

        if (library != null) Play(library.Finish, library.FinishGain);
    }

    public void PlayTankShot()
    {
        if (library != null) Play(library.TankShot, library.TankShotGain);
    }

    public void RegisterButton(Button button)
    {
        if (button == null || IsButtonAlreadyRegistered(button)) return;

        int freeIndex = -1;
        for (int i = 0; i < runtimeButtons.Length; i++)
        {
            if (runtimeButtons[i] == null)
            {
                freeIndex = i;
                break;
            }
        }

        if (freeIndex < 0) return;
        runtimeButtons[freeIndex] = button;
        if (isActiveAndEnabled)
        {
            button.onClick.AddListener(PlayUiClick);
        }
    }

    private void HookUiButtons(bool subscribe)
    {
        if (uiButtons == null) return;

        for (int i = 0; i < uiButtons.Length; i++)
        {
            Button button = uiButtons[i];
            if (button == null || !IsFirstButtonOccurrence(i, button)) continue;

            if (subscribe)
            {
                button.onClick.AddListener(PlayUiClick);
            }
            else
            {
                button.onClick.RemoveListener(PlayUiClick);
            }
        }
    }

    private bool IsFirstButtonOccurrence(int index, Button button)
    {
        for (int i = 0; i < index; i++)
        {
            if (uiButtons[i] == button) return false;
        }

        return true;
    }

    private bool IsButtonAlreadyRegistered(Button button)
    {
        if (uiButtons != null)
        {
            for (int i = 0; i < uiButtons.Length; i++)
            {
                if (uiButtons[i] == button) return true;
            }
        }

        for (int i = 0; i < runtimeButtons.Length; i++)
        {
            if (runtimeButtons[i] == button) return true;
        }

        return false;
    }

    private void HookRuntimeButtons(bool subscribe)
    {
        for (int i = 0; i < runtimeButtons.Length; i++)
        {
            Button button = runtimeButtons[i];
            if (button == null) continue;

            if (subscribe)
            {
                button.onClick.AddListener(PlayUiClick);
            }
            else
            {
                button.onClick.RemoveListener(PlayUiClick);
            }
        }
    }

    private void Play(AudioClip clip, float gain)
    {
        if (clip == null || gain <= 0f || AudioListener.volume <= 0f ||
            voicePool == null || voicePool.Length == 0)
        {
            return;
        }

        int voiceIndex = FindIdleVoice();
        if (voiceIndex < 0)
        {
            voiceIndex = FindNextAvailableVoice();
            if (voiceIndex < 0) return;
            voicePool[voiceIndex].Stop();
        }

        AudioSource voice = voicePool[voiceIndex];
        voice.ignoreListenerVolume = false;
        voice.PlayOneShot(clip, gain);
        nextVoiceIndex = (voiceIndex + 1) % voicePool.Length;
    }

    private int FindIdleVoice()
    {
        if (voicePool == null || voicePool.Length == 0) return -1;

        for (int offset = 0; offset < voicePool.Length; offset++)
        {
            int index = (nextVoiceIndex + offset) % voicePool.Length;
            AudioSource voice = voicePool[index];
            if (voice != null && !voice.isPlaying) return index;
        }

        return -1;
    }

    private int FindNextAvailableVoice()
    {
        if (voicePool == null || voicePool.Length == 0) return -1;

        for (int offset = 0; offset < voicePool.Length; offset++)
        {
            int index = (nextVoiceIndex + offset) % voicePool.Length;
            if (voicePool[index] != null) return index;
        }

        return -1;
    }
}
