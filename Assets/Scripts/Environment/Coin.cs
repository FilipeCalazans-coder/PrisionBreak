using UnityEngine;

/// <summary>
/// Controla o comportamento individual de cada moeda no jogo.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class Coin : MonoBehaviour
{
    [Header("Configurações da Moeda")]
    [Tooltip("Quantidade de moedas que este item concede ao ser coletado.")]
    [SerializeField] private int coinValue = 1;

    [Header("Tag do Jogador")]
    [Tooltip("Tag atribuída ao GameObject do jogador para confirmar a colisão.")]
    [SerializeField] private string playerTag = "Player";

    private void OnEnable()
    {
        // Garante que a moeda volte a ficar visível e colidível sempre que o Chunk for reativado pelo Object Pooling
        GetComponent<Collider2D>().enabled = true;
        
        // Ativa os componentes visuais caso tenham sido ocultados
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se quem entrou em contato com a moeda foi o jogador
        if (collision.CompareTag(playerTag))
        {
            CollectCoin();
        }
    }

    /// <summary>
    /// Envia o valor da moeda para o ScoreManager e desativa o objeto para reaproveitamento.
    /// </summary>
    private void CollectCoin()
    {
        // Notifica o gerenciador de pontos
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddCoins(coinValue);
        }

        // Desativa a moeda em vez de usar Destroy, preservando a memória para o Object Pooling
        gameObject.SetActive(false);
    }
}