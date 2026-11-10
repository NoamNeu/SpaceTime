using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectSource;

    [Header("Clips")]
    [SerializeField] private AudioClip coinPickup;
    [SerializeField] private AudioClip ghostHit;
    [SerializeField] private AudioClip spaceshipTakeoff;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float soundEffectVolume = 0.7f;

    private void Awake()
    {
        Instance = this;
        ResolveSources(true);
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        ResolveSources(false);
        ApplyVolumes();
    }

    public void PlayCoinPickup()
    {
        PlayEffect(coinPickup);
    }

    public void PlayGhostHit()
    {
        PlayEffect(ghostHit);
    }

    public void PlaySpaceshipTakeoff()
    {
        PlayEffect(spaceshipTakeoff);
    }

    private void ResolveSources(bool createEffectSource)
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (musicSource == null && sources.Length > 0)
            musicSource = sources[0];

        if (effectSource == null || effectSource == musicSource)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i] != musicSource)
                {
                    effectSource = sources[i];
                    break;
                }
            }
        }

        if (!createEffectSource || (effectSource != null && effectSource != musicSource))
            return;

        effectSource = gameObject.AddComponent<AudioSource>();
        effectSource.playOnAwake = false;
        effectSource.loop = false;
        effectSource.clip = null;
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        if (effectSource != null && effectSource != musicSource)
        {
            effectSource.spatialBlend = 0f;
            effectSource.loop = false;
            effectSource.playOnAwake = false;
            effectSource.volume = soundEffectVolume;
        }
    }

    private void PlayEffect(AudioClip clip)
    {
        if (clip == null || effectSource == null)
            return;

        effectSource.PlayOneShot(clip);
    }
}
