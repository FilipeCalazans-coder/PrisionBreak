using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla movimentação, velocidade progressiva sincronizada com o Animator,
/// física de pulo com peso, gestos de toque na tela e combate.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Velocidade Progressiva")]
    [Tooltip("Velocidade inicial de corrida do jogador.")]
    [SerializeField] private float initialRunSpeed = 4f;
    [Tooltip("Velocidade máxima que o jogador pode atingir ao longo do tempo.")]
    [SerializeField] private float maxRunSpeed = 12f;
    [Tooltip("Taxa de aumento de velocidade por segundo.")]
    [SerializeField] private float speedIncreaseRate = 0.05f;

    [Header("Configurações de Pulo & Peso")]
    [Tooltip("Força fixa do impulso de pulo.")]
    [SerializeField] private float jumpForce = 9f;
    [Tooltip("Multiplicador de gravidade durante a descida (adiciona peso ao personagem).")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Configurações de Ataque")]
    [Tooltip("Hitbox filha para colisão do golpe.")]
    [SerializeField] private GameObject attackHitboxObject;
    [Tooltip("Tempo em segundos que a hitbox fica ativa.")]
    [SerializeField] private float attackDuration = 0.2f;

    [Header("Configurações de Impacto (Bounce)")]
    [Tooltip("Força vertical ao quicar em um inimigo.")]
    [SerializeField] private float bounceForce = 7f;

    [Header("Configurações de Slide & Dash")]
    [Tooltip("Duração do slide em segundos.")]
    [SerializeField] private float slideDuration = 0.8f;
    [Tooltip("Velocidade adicional horizontal durante o slide.")]
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Configurações de Ground Pound")]
    [Tooltip("Força descendente vertical do ataque aéreo.")]
    [SerializeField] private float groundPoundForce = 25f;

    [Header("Configurações de Gesto (Swipe)")]
    [Tooltip("Distância mínima em pixels para validar um swipe.")]
    [SerializeField] private float minSwipeDistance = 30f;

    [Header("Verificação de Chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Componentes internos
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;

    // Cache de parâmetros do Animator
    private HashSet<int> existingAnimatorParams = new HashSet<int>();
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int isSlidingHash = Animator.StringToHash("isSliding");
    private readonly int isGroundPoundingHash = Animator.StringToHash("isGroundPounding");
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int animSpeedHash = Animator.StringToHash("animSpeed");

    // Estados de movimento e velocidade
    private float currentRunSpeed;
    private bool isGrounded;
    private bool jumpRequested;
    private bool isSliding;
    private bool isGroundPounding;
    private bool isAttacking;

    public bool IsGroundPounding => isGroundPounding;
    public float CurrentRunSpeed => currentRunSpeed;

    // Dimensões originais do colisor
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Controle de gestos de toque
    private Vector2 startTouchPos;
    private Vector2 currentTouchPos;
    private bool isTouching;

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

        CacheAnimatorParameters();
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
        // 1. Aceleração progressiva ao longo da corrida
        UpdateProgressiveSpeed();

        // 2. Detecção de Chão
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                isGroundPounding = false;
            }
        }

        // 3. Atualização segura dos parâmetros no Animator
        UpdateAnimator();

        // 4. Processamento de Entradas (Swipe / Teclado / Toque)
        HandleInputLifecycle();
    }

    private void FixedUpdate()
    {
        // Calcula a velocidade horizontal somando o bônus de slide se ativo
        float activeSpeed = currentRunSpeed;
        if (isSliding && !isGroundPounding)
        {
            activeSpeed += dashBonusSpeed;
        }

        rb.linearVelocity = new Vector2(activeSpeed, rb.linearVelocity.y);

        // Disparo do pulo
        if (jumpRequested)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            jumpRequested = false;
        }

        // Aplica peso extra na descida
        ApplyFallGravity();
    }

    /// <summary>
    /// Aumenta a velocidade do jogador gradualmente a cada frame até atingir o teto máximo.
    /// </summary>
    private void UpdateProgressiveSpeed()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver)
            {
                return;
            }
        }

        if (currentRunSpeed < maxRunSpeed)
        {
            currentRunSpeed = Mathf.MoveTowards(currentRunSpeed, maxRunSpeed, speedIncreaseRate * Time.deltaTime);
        }
    }

    /// <summary>
    /// Aumenta a gravidade na descida para dar sensação de peso ao pulo.
    /// </summary>
    private void ApplyFallGravity()
    {
        if (isGroundPounding || isGrounded) return;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Envia os estados e o multiplicador de velocidade de corrida para o Animator.
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) return;

        if (existingAnimatorParams.Contains(isGroundedHash))
            animator.SetBool(isGroundedHash, isGrounded);

        if (existingAnimatorParams.Contains(isSlidingHash))
            animator.SetBool(isSlidingHash, isSliding);

        if (existingAnimatorParams.Contains(isGroundPoundingHash))
            animator.SetBool(isGroundPoundingHash, isGroundPounding);

        // Atualiza a velocidade relativa da animação de corrida (Ex: 1x na largada, aumentando proporcionalmente)
        if (existingAnimatorParams.Contains(animSpeedHash) && initialRunSpeed > 0f)
        {
            float normalizedSpeedRatio = currentRunSpeed / initialRunSpeed;
            animator.SetFloat(animSpeedHash, normalizedSpeedRatio);
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

    private void HandleInputLifecycle()
    {
        // Teclado (Editor / PC)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded && !isGroundPounding)
            {
                jumpRequested = true;
            }
        }

        // Touchscreen (Mobile)
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                ProcessTouchStart(touch.screenPosition);
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                currentTouchPos = touch.screenPosition;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                currentTouchPos = touch.screenPosition;
                ProcessTouchEnd();
            }
            return;
        }

        // Mouse (Editor)
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessTouchStart(Mouse.current.position.ReadValue());
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                currentTouchPos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && isTouching)
            {
                currentTouchPos = Mouse.current.position.ReadValue();
                ProcessTouchEnd();
            }
        }
    }

    private void ProcessTouchStart(Vector2 position)
    {
        startTouchPos = position;
        currentTouchPos = position;
        isTouching = true;
    }

    private void ProcessTouchEnd()
    {
        isTouching = false;
        EvaluateGesture();
    }

    private void EvaluateGesture()
    {
        if (isGroundPounding) return;

        float deltaY = currentTouchPos.y - startTouchPos.y;

        // 1. Swipe Up (Pulo)
        if (deltaY >= minSwipeDistance)
        {
            if (isGrounded)
            {
                jumpRequested = true;
            }
        }
        // 2. Swipe Down (Slide no chão OU Ground Pound no ar)
        else if (deltaY <= -minSwipeDistance)
        {
            if (isGrounded)
            {
                StartSlide();
            }
            else
            {
                ExecuteGroundPound();
            }
        }
        // 3. Toque Simples (Ataque)
        else if (Mathf.Abs(deltaY) < minSwipeDistance)
        {
            TriggerAttack();
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}