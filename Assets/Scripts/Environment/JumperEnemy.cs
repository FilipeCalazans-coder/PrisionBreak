using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o comportamento de um inimigo que realiza pulos em intervalos de tempo fixos.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JumperEnemy : MonoBehaviour
{
    [Header("Configurações de Pulo")]
    [Tooltip("Força vertical do pulo.")]
    [SerializeField] private float jumpForce = 8f;

    [Tooltip("Tempo de espera em segundos entre cada pulo.")]
    [SerializeField] private float jumpInterval = 2f;

    [Header("Verificação de Chão")]
    [Tooltip("Ponto na base do inimigo para checar colisão com o chão.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Raio da esfera de detecção do chão.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("Camada (Layer) correspondente ao chão.")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // Inicia a rotina de pulo sempre que o objeto for ativado na cena
        StartCoroutine(JumpRoutine());
    }

    private void Update()
    {
        // Verifica se o inimigo está encostado no chão
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    /// <summary>
    /// Rotina que aguarda o intervalo de tempo e aplica a força de pulo se o inimigo estiver no chão.
    /// </summary>
    private IEnumerator JumpRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(jumpInterval);

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}