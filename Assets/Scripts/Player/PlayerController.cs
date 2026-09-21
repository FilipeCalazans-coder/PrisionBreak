using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla movimentação, física cinemática unificada de salto e quique (bounce),
/// mecânica de corrida contínua e sistema de combate com combos.
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

    [Header("Configurações de Salto Independente (Altura vs Tempo)")]
    [Tooltip("Altura exata máxima (em metros/unidades da Unity) que o salto normal alcança.")]
    [SerializeField] private float jumpHeight = 3.5f;
    [Tooltip("Tempo em segundos que o personagem leva para atingir o ápice do salto.")]
    [SerializeField] private float timeToJumpApex = 0.3f;
    [Tooltip("Multiplicador de gravidade durante a descida (adiciona peso na queda).")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Configurações de Quique Cinemático (Bounce / Stomp)")]
    [Tooltip("Altura exata (em metros) que o jogador atinge ao quicar em um inimigo ou após o Ground Pound.")]
    [SerializeField] private float bounceHeight = 3.5f;

    [Header("Buffer de Entrada (Responsividade do Salto)")]
    [Tooltip("Tempo em segundos que o jogo lembra que você apertou para pular antes de tocar no chão.")]
    [SerializeField] private float jumpBufferDuration = 0.15f;

    [Header("Configurações de Ground Pound")]
    [Tooltip("Força vertical aplicada durante a queda rápida do Ground Pound.")]
    [SerializeField] private float groundPoundForce = 25f;

    [Header("Configurações de Wall Jump")]
    [Tooltip("Ponto frontal para detetar a parede escalável.")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckRadius = 0.2f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(-4f, 11f);
    [SerializeField] private float wallJumpDuration = 0.25f;

    [Header("Configurações de Ataque e Combo")]
    [Tooltip("Hitbox filha para colisão do golpe.")]
    [SerializeField] private GameObject attackHitboxObject;
    [Tooltip("Duração de cada golpe em segundos.")]
    [SerializeField] private float attackStepDuration = 0.25f;
    [Tooltip("Tempo extra de tolerância após o primeiro soco para aceitar o segundo soco.")]
    [SerializeField] private float comboWindowGraceTime = 0.15f;
    [Tooltip("Permite desferir socos enquanto estiver no ar.")]
    [SerializeField] private bool canAttackInAir = true;

    [Header("Configurações de Slide & Dash")]
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Sensibilidade de Toque (Mobile)")]
    [SerializeField] private float minSwipeDistance = 15f;

    [Header("Verificação de Chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Componentes internos
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;

    // Variáveis físicas calculadas
    private float calculatedGravity;
    private float calculatedInitialJumpVelocity;
    private float calculatedBounceVelocity;

    // Temporizador do Jump Buffer
    private float jumpBufferTimer = 0f;

    // Cache de hashes do Animator
    private HashSet<int> existingAnimatorParams = new HashSet<int>();
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int isSlidingHash = Animator.StringToHash("isSliding");
    private readonly int isGroundPoundingHash = Animator.StringToHash("isGroundPounding");
    private readonly int groundPoundImpactHash = Animator.StringToHash("GroundPoundImpact");
    private readonly int isWallSlidingHash = Animator.StringToHash("isWallSliding");
    private readonly int wallJumpTriggerHash = Animator.StringToHash("WallJump");
    private readonly int attack1TriggerHash = Animator.StringToHash("Attack1");
    private readonly int attack2TriggerHash = Animator.StringToHash("Attack2");
    private readonly int animSpeedHash = Animator.StringToHash("animSpeed");

    // Estados de movimento
    private float currentRunSpeed;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isSliding;
    private bool isGroundPounding;

    // Estados de combate
    private int comboStep = 0;
    private bool canCombo = false;
    private bool bufferNextAttack = false;
    private Coroutine currentComboCoroutine;

    public bool IsGroundPounding => isGroundPounding;
    public float CurrentRunSpeed => currentRunSpeed;

    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

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

        RecalculateJumpPhysics();
        CacheAnimatorParameters();
    }

    private void OnValidate()
    {
        RecalculateJumpPhysics();
    }

    /// <summary>
    /// Calcula a gravidade, a velocidade do pulo padrão e a velocidade de quique (bounce)
    /// com base nas equações cinemáticas clássicas.
    /// </summary>
    private void RecalculateJumpPhysics()
    {
        if (timeToJumpApex <= 0.01f) timeToJumpApex = 0.01f;

        // g = (2 * altura) / (tempo^2)
        calculatedGravity = (2f * jumpHeight) / Mathf.Pow(timeToJumpApex, 2f);

        // v0_pulo = g * tempo
        calculatedInitialJumpVelocity = calculatedGravity * timeToJumpApex;

        // v0_bounce = raiz(2 * g * altura_bounce)
        calculatedBounceVelocity = Mathf.Sqrt(2f * calculatedGravity * bounceHeight);

        if (rb != null)
        {
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
        ProcessBufferedJump();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!isWallJumping)
        {
            float activeSpeed = currentRunSpeed;
            if (isSliding && !isGroundPounding)
            {
                activeSpeed += dashBonusSpeed;
            }
            rb.linearVelocity = new Vector2(activeSpeed, rb.linearVelocity.y);
        }

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

    private void RequestJump()
    {
        if (isGroundPounding) return;
        jumpBufferTimer = jumpBufferDuration;
    }

    private void ProcessBufferedJump()
    {
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;

            if (isWallSliding || (isTouchingWall && !isGrounded))
            {
                jumpBufferTimer = 0f;
                StartCoroutine(WallJumpRoutine());
                return;
            }

            if (isGrounded)
            {
                jumpBufferTimer = 0f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, calculatedInitialJumpVelocity);
            }
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

    /// <summary>
    /// Aplica o quique cinemático no jogador (ao pisar em inimigo ou impacto de Ground Pound).
    /// Utiliza a mesma gravidade e fórmula física do pulo padrão.
    /// </summary>
    public void Bounce()
    {
        isGroundPounding = false;

        // Zera a velocidade vertical residual antes de aplicar o impulso cinemático
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, calculatedBounceVelocity);
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

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void HandleInputLifecycle()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RequestJump();
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
            RequestJump();
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
                RegisterAttackInput();
            }
        }
    }

    public void RegisterAttackInput()
    {
        if (isGroundPounding) return;
        if (!isGrounded && !canAttackInAir) return;

        if (comboStep == 0)
        {
            currentComboCoroutine = StartCoroutine(ComboSequenceRoutine());
        }
        else if (comboStep == 1 && canCombo)
        {
            bufferNextAttack = true;
        }
    }

    private IEnumerator ComboSequenceRoutine()
    {
        comboStep = 1;
        bufferNextAttack = false;
        canCombo = true;

        if (animator != null && existingAnimatorParams.Contains(attack1TriggerHash))
        {
            animator.ResetTrigger(attack2TriggerHash);
            animator.SetTrigger(attack1TriggerHash);
        }

        if (attackHitboxObject != null) attackHitboxObject.SetActive(true);

        float timer = 0f;
        while (timer < attackStepDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!bufferNextAttack && comboWindowGraceTime > 0f)
        {
            if (attackHitboxObject != null) attackHitboxObject.SetActive(false);

            float graceTimer = 0f;
            while (graceTimer < comboWindowGraceTime && !bufferNextAttack)
            {
                graceTimer += Time.deltaTime;
                yield return null;
            }
        }

        if (bufferNextAttack)
        {
            comboStep = 2;
            canCombo = false;
            bufferNextAttack = false;

            if (animator != null && existingAnimatorParams.Contains(attack2TriggerHash))
            {
                animator.ResetTrigger(attack1TriggerHash);
                animator.SetTrigger(attack2TriggerHash);
            }

            if (attackHitboxObject != null) attackHitboxObject.SetActive(true);

            timer = 0f;
            while (timer < attackStepDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        ResetComboState();
    }

    private void ResetComboState()
    {
        if (attackHitboxObject != null) attackHitboxObject.SetActive(false);
        comboStep = 0;
        canCombo = false;
        bufferNextAttack = false;
        currentComboCoroutine = null;
    }

    private void ExecuteGroundPound()
    {
        if (isGroundPounding) return;
        isGroundPounding = true;

        if (currentComboCoroutine != null)
        {
            StopCoroutine(currentComboCoroutine);
        }
        ResetComboState();

        if (isSliding)
        {
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