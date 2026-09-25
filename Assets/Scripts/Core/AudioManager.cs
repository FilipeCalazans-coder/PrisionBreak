using UnityEngine;

/// <summary>
/// Gerencia a reproducao de musicas e efeitos sonoros (SFX),
/// alem de salvar, carregar e calibrar os limites de volume.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // Chaves de gravacao no PlayerPrefs
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    [Header("Calibracao de Limite de Volume")]
    [Tooltip("Define qual sera o volume real quando o Slider de Musica estiver em 100% (ex: 0.5 = 50% do volume original).")]
    [Range(0.05f, 1f)]
    [SerializeField] private float maxMusicVolumeCap = 0.5f;

    [Tooltip("Define qual sera o volume real quando o Slider de SFX estiver em 100%.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float maxSFXVolumeCap = 1f;

    [Header("Componentes de Audio")]
    [Tooltip("AudioSource dedicado a tocar a musica de fundo em loop.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("AudioSource dedicado a disparar efeitos sonoros.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips de Musica (Opcional)")]
    [Tooltip("Musica tema principal tocada no inicio.")]
    [SerializeField] private AudioClip mainThemeMusic;

    // Variaveis de controle que guardam a porcentagem escolhida pelo jogador (0f a 1f)
    private float currentMusicVolume = 0.75f;
    private float currentSFXVolume = 0.75f;

    public float MusicVolume => currentMusicVolume;
    public float SFXVolume => currentSFXVolume;

    private void Awake()
    {
        // Garante que apenas uma instancia do AudioManager exista na cena
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Se os AudioSources nao forem arrastados no Inspector, cria-os automaticamente
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        CarregarVolumes();
    }

    private void Start()
    {
        // Inicia a musica tema caso esteja associada
        if (mainThemeMusic != null)
        {
            PlayMusic(mainThemeMusic);
        }
    }

    /// <summary>
    /// Carrega os volumes gravados previamente ou usa 0.75 (75%) como padrao.
    /// </summary>
    private void CarregarVolumes()
    {
        currentMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.75f);
        currentSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.75f);

        AplicarVolumes();
    }

    /// <summary>
    /// Aplica os volumes nos AudioSources multiplicando pela calibracao de teto maximo.
    /// </summary>
    private void AplicarVolumes()
    {
        if (musicSource != null)
        {
            // Aplica a porcentagem do jogador sobre o limite configurado
            musicSource.volume = currentMusicVolume * maxMusicVolumeCap;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = currentSFXVolume * maxSFXVolumeCap;
        }
    }

    /// <summary>
    /// Ajusta a porcentagem de volume da musica e grava a alteracao.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = Mathf.Clamp01(volume);
        
        if (musicSource != null)
        {
            musicSource.volume = currentMusicVolume * maxMusicVolumeCap;
        }

        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, currentMusicVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Ajusta a porcentagem de volume dos efeitos sonoros (SFX) e grava a alteracao.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = Mathf.Clamp01(volume);
        
        if (sfxSource != null)
        {
            sfxSource.volume = currentSFXVolume * maxSFXVolumeCap;
        }

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, currentSFXVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Toca uma faixa musical continua.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        musicSource.clip = clip;
        musicSource.volume = currentMusicVolume * maxMusicVolumeCap;
        musicSource.Play();
    }

    /// <summary>
    /// Dispara um efeito sonoro instantaneo com o volume calibrado.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        // Multiplica o volume escolhido pelo limite maximo de SFX
        float finalSFXVolume = currentSFXVolume * maxSFXVolumeCap;
        sfxSource.PlayOneShot(clip, finalSFXVolume);
    }
}