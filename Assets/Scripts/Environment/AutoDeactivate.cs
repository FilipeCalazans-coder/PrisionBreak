using UnityEngine;

/// <summary>
/// Desativa automaticamente o GameObject quando ele fica para trás do jogador,
/// permitindo que o ObjectPooler o reaproveite no futuro.
/// </summary>
public class AutoDeactivate : MonoBehaviour
{
    [Header("Configurações de Distância")]
    [Tooltip("Distância atrás do jogador em que o objeto será recolhido/desativado.")]
    [SerializeField] private float distanceBehindPlayer = 15f;

    // Referência ao Transform do jogador
    private Transform playerTransform;

    private void OnEnable()
    {
        // Localiza o jogador na cena quando o objeto é ativado pelo ObjectPooler
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Se o objeto ficou para trás da posição X do jogador além do limite, desativa
        if (transform.position.x < playerTransform.position.x - distanceBehindPlayer)
        {
            gameObject.SetActive(false);
        }
    }
}