using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla movimentação, velocidade progressiva e física de pulo cinemática,
/// permitindo configurar a altura máxima e a velocidade de subida de forma independente.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Velocidade Progressiva")]
    [Tooltip("Velocidade inicial de corrida do jogador.")]
    [SerializeField] private float initialRunSpeed = 4f;
    [Tooltip("Velocidade máxima que o jogador pode atingir.")]
    [SerializeField] private float maxRunSpeed = 12f;
    [Tooltip("Taxa de aumento de velocidade por segundo.")]
    [SerializeField] private float speedIncreaseRate = 0.05f;

    [Header("Configurações de Pulo Independente (Altura vs Tempo)")]
    [Tooltip("Altura exata máxima (em unidades/metros da Unity) que o pulo alcança.")]
    [SerializeField] private float jumpHeight = 3.5f;
    [Tooltip("Tempo em segundos que o personagem leva para sair do chão e atingir o ápice do pulo (menor = mais rápido).")]
    [SerializeField] private float timeToJumpApex = 0.3f;
    [Tooltip("Multiplicador de gravidade durante a queda (adiciona peso na descida).")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Configurações de Ground Pound")]
    [Tooltip("Força descendente vertical aplicada durante a queda do Ground Pound.")]
    [SerializeField] private float groundPoundForce = 25f;

    [Header("Configurações de Wall Jump")]
    [Tooltip("Ponto frontal para detectar contato com a parede.")]
    [SerializeField] private Transform wallCheck;
    [Tooltip("Raio de detecção da parede.")]
    [SerializeField] private float wallCheckRadius = 0.2f;
    [Tooltip("Camada (Layer) correspondente às paredes escaláveis.")]
    [SerializeField] private LayerMask wallLayer;
    [Tooltip("Velocidade máxima de descida enquanto escorrega na parede.")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [Tooltip("Força horizontal (X) e vertical (Y) aplicadas ao realizar o Wall Jump.")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(-4f, 11f);
    [Tooltip("Tempo em que o controle horizontal fica bloqueado para dar espaço ao pulo.")]
    [SerializeField] private float wallJumpDuration = 0.25f;

    [Header("Configurações de Ataque")]
    [Tooltip("Hitbox filha para colisão do golpe.")]
    [SerializeField] private GameObject attackHitboxObject;
    [Tooltip("Tempo em segundos que a hitbox fica ativa.")]
    [SerializeField] private float attackDuration = 0.2f;

    [Header("Configurações de Impacto (Bounce)")]
    [Tooltip("Força vertical ao quicar em um inimigo.")]
    [SerializeField] private float bounceForce = 8f;

    [Header("Configurações de Slide & Dash")]
    [Tooltip("Duração do slide em segundos.")]
    [SerializeField] private float slideDuration = 0.8f;
    [Tooltip("Velocidade adicional horizontal durante o slide.")]
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Sensibilidade de Toque (Mobile)")]
    [Tooltip("Distância mínima em pixels para validar o gesto imediatamente.")]
    [SerializeField] private float minSwipeDistance = 15f;

    [Header("Verificação de Chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Componentes internos
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;

    // Variáveis cinemáticas calculadas
    private float calculatedGravity;
    private float calculatedInitialJumpVelocity;

    // Cache de parâmetros do Animator
    private HashSet<int> existingAnimatorParams = new HashSet<int>();
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int isSlidingHash = Animator.StringToHash("isSliding");
    private readonly int isGroundPoundingHash = Animator.StringToHash("isGroundPounding");
    private readonly int groundPoundImpactHash = Animator.StringToHash("GroundPoundImpact");
    private readonly int isWallSlidingHash = Animator.StringToHash("isWallSliding");
    private readonly int wallJumpTriggerHash = Animator.StringToHash("WallJump");
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int animSpeedHash = Animator.StringToHash("animSpeed");

    // Estados de movimento e física
    private float currentRunSpeed;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isSliding;
    private bool isGroundPounding;
    private bool isAttacking;

    public bool IsGroundPounding => isGroundPounding;
    public float CurrentRunSpeed => currentRunSpeed;

    // Dimensões do colisor
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Controle de gestos táteis em tempo real
    private Vector2 startTouchPos;
    private bool isTouching;
    private bool gestureConsumed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();

        if (capsuleCollider != null)
        {
            originalColliderSize = capsuleCollider.size;
            originalColliderOffset = capsuleCollider.offset;
        }

        if (attackHitboxObject == null)
        {
            Transform foundHitbox = transform.Find("AttackHitbox");
            if (foundHitbox != null) attackHitboxObject = foundHitbox.gameObject;
        }

        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);

        // Calcula a física exata baseada na altura e tempo desejados
        RecalculateJumpPhysics();

        CacheAnimatorParameters();
    }

    private void OnValidate()
    {
        // Permite recalcular as fórmulas mesmo alterando os valores no Inspector durante o teste
        RecalculateJumpPhysics();
    }

    /// <summary>
    /// Calcula a gravidade e o impulso inicial com base na cinemática clássica (Torricelli).
    /// </summary>
    private void RecalculateJumpPhysics()
    {
        if (timeToJumpApex <= 0.01f) timeToJumpApex = 0.01f;

        // g = (2 * altura) / (tempo^2)
        calculatedGravity = (2f * jumpHeight) / Mathf.Pow(timeToJumpApex, 2f);

        // v0 = g * tempo
        calculatedInitialJumpVelocity = calculatedGravity * timeToJumpApex;

        if (rb != null)
        {
            // Ajusta o GravityScale relativo à gravidade padrão da Unity (-9.81)
            float standardGravityMagnitude = Mathf.Abs(Physics2D.gravity.y);
            if (standardGravityMagnitude > 0.01f)
            {
                rb.gravityScale = calculatedGravity / standardGravityMagnitude;
            }
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        currentRunSpeed = initialRunSpeed;
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        UpdateProgressiveSpeed();
        HandleInputLifecycle();
        CheckSurroundings();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Movimentação horizontal contínua
        if (!isWallJumping)
        {
            float activeSpeed = currentRunSpeed;
            if (isSliding && !isGroundPounding)
            {
                activeSpeed += dashBonusSpeed;
            }

            rb.linearVelocity = new Vector2(activeSpeed, rb.linearVelocity.y);
        }

        // Física na parede e na queda
        if (isWallSliding)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            ApplyFallGravity();
        }
    }

    private void CheckSurroundings()
    {
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                isWallJumping = false;

                if (isGroundPounding)
                {
                    TriggerGroundPoundImpact();
                }
            }
        }

        if (wallCheck != null)
        {
            isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
        }

        isWallSliding = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0.1f && !isGroundPounding;
    }

    /// <summary>
    /// Aplica o pulo no exato frame do comando.
    /// </summary>
    private void ExecuteImmediateJump()
    {
        if (isGroundPounding) return;

        // 1. Pulo na parede (Wall Jump)
        if (isWallSliding || (isTouchingWall && !isGrounded))
        {
            StartCoroutine(WallJumpRoutine());
            return;
        }

        // 2. Pulo no chão: aplica a velocidade calculada para atingir o jumpHeight
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, calculatedInitialJumpVelocity);
        }
    }

    private IEnumerator WallJumpRoutine()
    {
        isWallJumping = true;
        isWallSliding = false;

        if (animator != null && existingAnimatorParams.Contains(wallJumpTriggerHash))
        {
            animator.SetTrigger(wallJumpTriggerHash);
        }

        rb.linearVelocity = wallJumpForce;

        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;
    }

    public void TriggerGroundPoundImpact()
    {
        isGroundPounding = false;

        if (animator != null && existingAnimatorParams.Contains(groundPoundImpactHash))
        {
            animator.SetTrigger(groundPoundImpactHash);
        }
    }

    private void UpdateProgressiveSpeed()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver) return;
        }

        if (currentRunSpeed < maxRunSpeed)
        {
            currentRunSpeed = Mathf.MoveTowards(currentRunSpeed, maxRunSpeed, speedIncreaseRate * Time.deltaTime);
        }
    }

    private void ApplyFallGravity()
    {
        if (isGroundPounding || isGrounded || isWallSliding) return;

        // Se o personagem estiver na trajetória de descida, adiciona o peso extra
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void HandleInputLifecycle()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExecuteImmediateJump();
        }

        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                startTouchPos = touch.screenPosition;
                isTouching = true;
                gestureConsumed = false;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                CheckInstantGesture(touch.screenPosition);
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                FinalizeTouch(touch.screenPosition);
            }
            return;
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                startTouchPos = Mouse.current.position.ReadValue();
                isTouching = true;
                gestureConsumed = false;
            }
            else if (Mouse.current.leftButton.isPressed && isTouching)
            {
                CheckInstantGesture(Mouse.current.position.ReadValue());
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && isTouching)
            {
                FinalizeTouch(Mouse.current.position.ReadValue());
            }
        }
    }

    private void CheckInstantGesture(Vector2 currentPos)
    {
        if (gestureConsumed || isGroundPounding) return;

        float deltaY = currentPos.y - startTouchPos.y;

        if (deltaY >= minSwipeDistance)
        {
            gestureConsumed = true;
            ExecuteImmediateJump();
        }
        else if (deltaY <= -minSwipeDistance)
        {
            gestureConsumed = true;
            if (isGrounded)
            {
                StartSlide();
            }
            else
            {
                ExecuteGroundPound();
            }
        }
    }

    private void FinalizeTouch(Vector2 endPos)
    {
        isTouching = false;

        if (!gestureConsumed)
        {
            float deltaY = endPos.y - startTouchPos.y;
            if (Mathf.Abs(deltaY) < minSwipeDistance)
            {
                TriggerAttack();
            }
        }
    }

    public void TriggerAttack()
    {
        if (isAttacking || isGroundPounding) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (animator != null && existingAnimatorParams.Contains(attackTriggerHash))
        {
            animator.SetTrigger(attackTriggerHash);
        }

        if (attackHitboxObject != null) attackHitboxObject.SetActive(true);

        yield return new WaitForSeconds(attackDuration);

        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);

        isAttacking = false;
    }

    public void Bounce()
    {
        isGroundPounding = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
    }

    private void ExecuteGroundPound()
    {
        if (isGroundPounding) return;
        isGroundPounding = true;

        if (isAttacking)
        {
            StopAllCoroutines();
            if (attackHitboxObject != null) attackHitboxObject.SetActive(false);
            isAttacking = false;
        }

        if (isSliding)
        {
            StopAllCoroutines();
            ResetCollider();
            isSliding = false;
        }

        rb.linearVelocity = new Vector2(currentRunSpeed, -groundPoundForce);
    }

    private void StartSlide()
    {
        if (isSliding || isGroundPounding) return;
        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        if (capsuleCollider != null)
        {
            capsuleCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
            capsuleCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y * 0.25f));
        }

        yield return new WaitForSeconds(slideDuration);

        ResetCollider();
        isSliding = false;
    }

    private void ResetCollider()
    {
        if (capsuleCollider != null)
        {
            capsuleCollider.size = originalColliderSize;
            capsuleCollider.offset = originalColliderOffset;
        }
    }

    private void CacheAnimatorParameters()
    {
        existingAnimatorParams.Clear();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                existingAnimatorParams.Add(param.nameHash);
            }
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        if (existingAnimatorParams.Contains(isGroundedHash))
            animator.SetBool(isGroundedHash, isGrounded);

        if (existingAnimatorParams.Contains(isSlidingHash))
            animator.SetBool(isSlidingHash, isSliding);

        if (existingAnimatorParams.Contains(isGroundPoundingHash))
            animator.SetBool(isGroundPoundingHash, isGroundPounding);

        if (existingAnimatorParams.Contains(isWallSlidingHash))
            animator.SetBool(isWallSlidingHash, isWallSliding);

        if (existingAnimatorParams.Contains(animSpeedHash) && initialRunSpeed > 0f)
        {
            float normalizedSpeedRatio = currentRunSpeed / initialRunSpeed;
            animator.SetFloat(animSpeedHash, normalizedSpeedRatio);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }
}