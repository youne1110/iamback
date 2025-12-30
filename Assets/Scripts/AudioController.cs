using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Background Music")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    public bool playBgmOnStart = true;
    [Range(0f,1f)] public float bgmVolume = 0.6f;

    [Header("Sound Effects")]
    public AudioSource sfxSource;
    [Range(0f,1f)] public float sfxVolume = 1f;

    [Header("Action Clips")]
    public AudioClip stateChangeClip;
    public AudioClip eatClip;
    public AudioClip hitClip;
    public AudioClip petClip;
    public AudioClip chokeClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
    }

    void Start()
    {
        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
        if (playBgmOnStart)
            PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmClip == null) return;
        if (bgmSource.clip != bgmClip)
            bgmSource.clip = bgmClip;
        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource.isPlaying)
            bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
    }

    public void PlayStateChange()
    {
        PlaySFX(stateChangeClip);
    }

    public void PlayEat()
    {
        PlaySFX(eatClip);
    }

    public void PlayHit()
    {
        PlaySFX(hitClip);
    }

    public void PlayPet()
    {
        PlaySFX(petClip);
    }

    public void PlayChoke()
    {
        PlaySFX(chokeClip);
    }
}
