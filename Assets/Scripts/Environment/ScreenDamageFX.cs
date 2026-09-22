using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine; // Namespace do novo Cinemachine (Unity 6 / Cinemachine 3.x)

/// <summary>
/// Controla o Camera Shake nativo via Cinemachine Impulse
/// e o flash vermelho no ecrã ao sofrer ou causar dano.
/// </summary>
public class ScreenDamageFX : MonoBehaviour
{
    public static ScreenDamageFX Instance;

    [Header("Efeito de Flash Vermelho")]
    [Tooltip("Imagem da UI que cobre o ecrã com a cor vermelha.")]
    [SerializeField] private Image damageFlashImage;
    [Tooltip("Transparência máxima do vermelho ao tomar dano (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxFlashAlpha = 0.5f;
    [Tooltip("Velocidade com que o flash vermelho desaparece.")]
    [SerializeField] private float flashFadeSpeed = 3f;

    [Header("Cinemachine Impulse")]
    [Tooltip("Componente de impulso nativo do Cinemachine para gerar o tremor.")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private Coroutine currentFlashCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tenta pegar o componente no próprio objeto se não foi arrastado no Inspector
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        // Garante que o flash vermelho inicia invisível
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
            damageFlashImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Dispara o tremor da câmara via impulso nativo do Cinemachine.
    /// </summary>
    /// <param name="duration">Mantido para compatibilidade de chamada.</param>
    /// <param name="magnitude">Força do impacto.</param>
    public void TriggerShake(float duration, float magnitude)
    {
        if (impulseSource != null)
        {
            // Dispara o impulso multiplicando pela força desejada
            impulseSource.GenerateImpulse(magnitude);
        }
    }

    /// <summary>
    /// Faz o ecrã piscar em vermelho e desaparecer suavemente.
    /// </summary>
    public void TriggerRedFlash()
    {
        if (damageFlashImage == null) return;

        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }

        currentFlashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color c = damageFlashImage.color;
        c.a = maxFlashAlpha;
        damageFlashImage.color = c;

        while (damageFlashImage.color.a > 0.01f)
        {
            c.a = Mathf.MoveTowards(c.a, 0f, flashFadeSpeed * Time.unscaledDeltaTime);
            damageFlashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        damageFlashImage.color = c;
        currentFlashCoroutine = null;
    }
}