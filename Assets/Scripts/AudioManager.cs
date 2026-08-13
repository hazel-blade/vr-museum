using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The AudioSource used to play Background Music (BGM). If left blank, it will automatically add one.")]
    public AudioSource bgmSource;
    [Tooltip("The AudioSource used to play Sound Effects (SFX). If left blank, it will automatically add one.")]
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip generatorCompleteSound;
    public AudioClip missionCompleteSound;
    public AudioClip museumOpenSound;
    public AudioClip victorySound;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            // Uncomment the line below if you want music to persist across scene loads (e.g. into EndingScene)
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-setup AudioSources if not assigned
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    public void PlayBGM()
    {
        if (backgroundMusic != null && bgmSource != null)
        {
            Debug.Log("[AudioManager] Playing BGM");
            bgmSource.clip = backgroundMusic;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            Debug.Log("[AudioManager] Stopping BGM");
            bgmSource.Stop();
        }
    }

    public void PlayGeneratorComplete()
    {
        PlaySFX(generatorCompleteSound);
    }

    public void PlayMissionComplete()
    {
        PlaySFX(missionCompleteSound);
    }

    public void PlayMuseumOpen()
    {
        PlaySFX(museumOpenSound);
    }

    public void PlayVictory()
    {
        PlaySFX(victorySound);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            Debug.Log($"[AudioManager] Playing SFX: {clip.name}");
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            if (clip == null)
                Debug.LogWarning("[AudioManager] Tried to play a sound, but the AudioClip is missing in the Inspector!");
        }
    }
}
