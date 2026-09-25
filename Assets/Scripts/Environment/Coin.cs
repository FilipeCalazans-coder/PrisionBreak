using UnityEngine;

/// <summary>
/// Controla a coleta de moedas e dispara o efeito sonoro no AudioManager.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class Coin : MonoBehaviour
{
    [Header("Configuracoes da Moeda")]
    [Tooltip("Quantidade de moedas concedida ao coletar.")]
    [SerializeField] private int coinValue = 1;

    [Tooltip("Efeito sonoro tocado ao coletar a moeda.")]
    [SerializeField] private AudioClip coinCollectSFX;

    [Header("Tag do Jogador")]
    [Tooltip("Tag atribuida ao jogador.")]
    [SerializeField] private string playerTag = "Player";

    private void OnEnable()
    {
        // Reativa a colisao e o visual ao sair do pool
        GetComponent<Collider2D>().enabled = true;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            CollectCoin();
        }
    }

    private void CollectCoin()
    {
        // 1. Notifica o gerenciador de pontuacao
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddCoins(coinValue);
        }

        // 2. Dispara o efeito sonoro via AudioManager
        if (AudioManager.Instance != null && coinCollectSFX != null)
        {
            AudioManager.Instance.PlaySFX(coinCollectSFX);
        }

        // 3. Desativa o objeto para reaproveitamento no pool
        gameObject.SetActive(false);
    }
}