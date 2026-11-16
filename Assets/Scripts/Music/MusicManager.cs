using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
 
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
 
    private const string MusicVolumeKey = "MusicVolume";
    private const string SoundVolumeKey = "SoundVolume";

    [SerializeField]
    private MusicLibrary musicLibrary;
    [SerializeField]
    private AudioSource musicSource;
    [SerializeField]
    private AudioSource soundSource;

    [Header("UI Slidery pre hlasitosť (voliteľné - nájdu sa aj automaticky)")]
    [SerializeField]
    private Slider musicSlider;
    [SerializeField]
    private Slider soundSlider;

    private float musicVolume = 1f;
    private float soundVolume = 1f;
    private Coroutine crossfadeCoroutine;
    private AudioClip currentTrack; // Track, ktorý hrá (alebo na ktorý sa práve prelína)
 
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        soundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        // Zdroj pre zvukové efekty - ak nie je priradený v Inspectore, vytvorí sa automaticky
        if (soundSource == null)
        {
            soundSource = gameObject.AddComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.loop = false;
        }

        ApplySoundVolume();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        BindVolumeSliders();
        ApplySoundVolume();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindVolumeSliders();
        ApplySoundVolume();
    }

    private void BindVolumeSliders()
    {
        Slider[] allSliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Slider slider in allSliders)
        {
            if (slider == null) continue;

            if (slider.gameObject.name == "MusicSlider")
            {
                musicSlider = slider;
            }
            else if (slider.gameObject.name == "SoundSlider")
            {
                soundSlider = slider;
            }
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (soundSlider != null)
        {
            soundSlider.SetValueWithoutNotify(soundVolume);
            soundSlider.onValueChanged.RemoveListener(SetSoundVolume);
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        ApplySoundVolume();
        PlayerPrefs.SetFloat(SoundVolumeKey, soundVolume);
        PlayerPrefs.Save();
    }

    private void ApplySoundVolume()
    {
        if (soundSource != null)
        {
            soundSource.volume = soundVolume;
        }

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource source in allSources)
        {
            if (source != null && source != musicSource)
            {
                source.volume = soundVolume;
            }
        }
    }
 
    // Zahrá krátky zvukový efekt z library (neprerušuje hudbu ani iné efekty)
    public void PlaySound(string soundName)
    {
        if (musicLibrary == null || soundSource == null) return;

        AudioClip clip = musicLibrary.GetClipFromName(soundName);
        if (clip == null) return;

        soundSource.PlayOneShot(clip);
    }

    // Náhodný z troch zvukov úderu (Hit1, Hit2, Hit3)
    public void PlayHitSound()
    {
        PlaySound("Hit" + Random.Range(1, 4));
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        if (musicLibrary == null || musicSource == null) return;

        AudioClip nextTrack = musicLibrary.GetClipFromName(trackName);
        if (nextTrack == null) return;

        // Rovnaký track už hrá - nezačíname ho odznova (plynulé pokračovanie medzi scénami)
        if (nextTrack == currentTrack && musicSource.isPlaying) return;
        currentTrack = nextTrack;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }
        crossfadeCoroutine = StartCoroutine(AnimateMusicCrossfade(nextTrack, fadeDuration));
    }

    // Stlmí a zastaví hudbu (napr. pri prehre)
    public void StopMusic(float fadeDuration = 0.5f)
    {
        if (musicSource == null) return;

        currentTrack = null;
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }
        crossfadeCoroutine = StartCoroutine(AnimateMusicFadeOut(fadeDuration));
    }

    IEnumerator AnimateMusicFadeOut(float fadeDuration)
    {
        float startVolume = musicSource.volume;
        float percent = 0;

        while (percent < 1 && fadeDuration > 0f)
        {
            percent += Time.deltaTime * 1f / fadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, percent);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = musicVolume;
        crossfadeCoroutine = null;
    }
 
    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f)
    {
        float startVolume = musicSource.volume;
        float percent = 0;

        if (musicSource.isPlaying && musicSource.clip != null && fadeDuration > 0f)
        {
            while (percent < 1)
            {
                percent += Time.deltaTime * 1f / fadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, percent);
                yield return null;
            }
        }
 
        musicSource.clip = nextTrack;
        musicSource.Play();
 
        percent = 0;
        if (fadeDuration > 0f)
        {
            while (percent < 1)
            {
                percent += Time.deltaTime * 1f / fadeDuration;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, percent);
                yield return null;
            }
        }

        musicSource.volume = musicVolume;
        crossfadeCoroutine = null;
    }
}
