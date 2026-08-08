using UnityEngine;
using UnityEngine.InputSystem; // Namespace necessário para o novo Input System

/// <summary>
/// Controla a movimentação automática e os pulos/impulsos do jogador via touch ou mouse.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade constante do jogador para a direita.")]
    [SerializeField] private float runSpeed = 8f;

    [Tooltip("Força do pulo ou impulso vertical.")]
    [SerializeField] private float jumpForce = 12f;

    [Header("Verificação de Chão")]
    [Tooltip("Ponto de onde será feito o raio de detecção do chão.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Raio da esfera de detecção do chão.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("Camada (Layer) correspondente ao chão.")]
    [SerializeField] private LayerMask groundLayer;

    // Referências e variáveis internas de estado
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isTouchingScreen;

    private void Awake()
    {
        // Obtém a referência do componente de física Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Verifica continuamente se o pé do personagem está tocando a camada de chão
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    private void FixedUpdate()
    {
        // 1. Mantém a velocidade contínua para a direita no eixo X
        rb.linearVelocity = new Vector2(runSpeed, rb.linearVelocity.y);

        // 2. Se a tela estiver sendo tocada e o personagem estiver no chão, aplica a força de pulo
        if (isTouchingScreen && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Reseta a variável para exigir um novo toque caso seja pulo único
            isTouchingScreen = false;
        }
    }

    /// <summary>
    /// Método disparado pelo evento do novo Input System quando o jogador toca a tela ou clica com o mouse.
    /// Nome baseado na ação 'Jump' configurada no MobileInputActions.
    /// </summary>
    /// <param name="value">Objeto que contém as informações do toque/clique.</param>
    public void OnJump(InputValue value)
    {
        // Captura se a tela está sendo tocada no momento (true/false)
        isTouchingScreen = value.isPressed;
    }

    // Desenha o círculo do Ground Check na aba Scene para depuração e ajustes no Inspector
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}