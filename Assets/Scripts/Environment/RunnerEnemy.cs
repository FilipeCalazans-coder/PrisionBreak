using UnityEngine;

/// <summary>
/// Controla o comportamento de um inimigo que corre na direção oposta ao jogador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RunnerEnemy : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade com que o inimigo corre para a esquerda.")]
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Move o inimigo continuamente para a esquerda (eixo X negativo)
        rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
    }
}