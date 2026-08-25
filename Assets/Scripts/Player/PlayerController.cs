using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla a física, as ações e as animações do jogador no runner 2D,
/// com checagem segura de parâmetros do Animator para evitar erros no Console.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade constante padrão do jogador para a direita.")]
    [SerializeField] private float runSpeed = 4f;

    [Tooltip("Força do pulo normal aplicada ao personagem.")]
    [SerializeField] private float jumpForce = 6f;

    [Header("Configurações de Ataque Básico")]
    [Tooltip("Objeto filho que contém o colisor (Hitbox) do ataque.")]
    [SerializeField] private GameObject attackHitboxObject;

    [Tooltip("Duração em segundos que a Hitbox do ataque permanece ativa.")]
    [SerializeField] private float attackDuration = 0.2f;

    [Header("Configurações de Impacto (Bounce)")]
    [Tooltip("Força do pulo de resposta ao esmagar um inimigo com o Ground Pound ou pulo.")]
    [SerializeField] private float bounceForce = 6f;

    [Header("Configurações de Slide & Dash")]
    [Tooltip("Tempo em segundos que o personagem permanece agachado e acelerado.")]
    [SerializeField] private float slideDuration = 0.8f;

    [Tooltip("Velocidade extra adicionada horizontalmente durante o Slide.")]
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Configurações de Ground Pound")]
    [Tooltip("Força vertical descendente aplicada durante o Ground Pound.")]
    [SerializeField] private float groundPoundForce = 25f;

    [Header("Configurações de Swipe (Sensibilidade)")]
    [Tooltip("Distância mínima em pixels para considerar um gesto de deslize.")]
    [SerializeField] private float minSwipeDistance = 30f;

    [Header("Verificação de Chão")]
    [Tooltip("Ponto de onde será feito o raio de detecção do chão.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Raio da esfera de detecção do chão.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("Camada (Layer) correspondente ao chão.")]
    [SerializeField] private LayerMask groundLayer;

    // Componentes internos
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;

    // Conjunto para armazenar e validar parâmetros existentes no Animator
    private HashSet<int> existingAnimatorParams = new HashSet<int>();

    // Hashes dos parâmetros do Animator
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int isSlidingHash = Animator.StringToHash("isSliding");
    private readonly int isGroundPoundingHash = Animator.StringToHash("isGroundPounding");
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");

    // Estados de movimento
    private bool isGrounded;
    private bool jumpRequested;
    private bool isSliding;
    private bool isGroundPounding;
    private bool isAttacking;

    public bool IsGroundPounding => isGroundPounding;

    // Controle de dimensões do colisor
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Controle de toque e gestos
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
            if (foundHitbox != null)
            {
                attackHitboxObject = foundHitbox.gameObject;
            }
        }

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(false);
        }

        // Mapeia todos os parâmetros criados no Animator Controller para validação segura
        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // 1. Verificação de Chão
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                isGroundPounding = false;
            }
        }

        // 2. Atualização segura do Animator
        UpdateAnimator();

        // 3. Processamento de Toque / Mouse
        HandleInputLifecycle();
    }

    private void FixedUpdate()
    {
        float currentSpeed = runSpeed;
        if (isSliding && !isGroundPounding)
        {
            currentSpeed += dashBonusSpeed;
        }

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            jumpRequested = false;
        }
    }

    /// <summary>
    /// Lê e guarda os parâmetros cadastrados no Animator Controller.
    /// </summary>
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

    /// <summary>
    /// Envia os valores para o Animator apenas se os parâmetros existirem na controladora.
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
    }

    private void HandleInputLifecycle()
    {
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

        // Mouse (Editor / PC)
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
        // 3. Toque Simples (Ataque Instantâneo)
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

        // Dispara o Trigger apenas se ele existir no Animator Controller
        if (animator != null && existingAnimatorParams.Contains(attackTriggerHash))
        {
            animator.SetTrigger(attackTriggerHash);
        }

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(true);
        }

        yield return new WaitForSeconds(attackDuration);

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(false);
        }
        isAttacking = false;
    }

    public void Bounce()
    {
        isGroundPounding = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
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

        rb.linearVelocity = new Vector2(runSpeed, -groundPoundForce);
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