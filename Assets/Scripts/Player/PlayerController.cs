using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla movimentacao, fisica cinematica, combate e sons de acao do jogador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuracoes de Velocidade Progressiva")]
    [SerializeField] private float initialRunSpeed = 4f;
    [SerializeField] private float maxRunSpeed = 12f;
    [SerializeField] private float speedIncreaseRate = 0.05f;

    [Header("Configuracoes de Salto Independente")]
    [SerializeField] private float jumpHeight = 3.5f;
    [SerializeField] private float timeToJumpApex = 0.3f;
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Configuracoes de Quique Cinematico (Bounce)")]
    [SerializeField] private float bounceHeight = 3.5f;

    [Header("Buffer de Entrada")]
    [SerializeField] private float jumpBufferDuration = 0.15f;

    [Header("Configuracoes de Ground Pound")]
    [SerializeField] private float groundPoundForce = 25f;

    [Header("Configuracoes de Wall Jump")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckRadius = 0.2f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(-4f, 11f);
    [SerializeField] private float wallJumpDuration = 0.25f;

    [Header("Configuracoes de Ataque e Combo")]
    [SerializeField] private GameObject attackHitboxObject;
    [SerializeField] private float attackStepDuration = 0.25f;
    [SerializeField] private float comboWindowGraceTime = 0.15f;
    [SerializeField] private bool canAttackInAir = true;

    [Header("Configuracoes de Slide & Dash")]
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Efeitos Sonoros do Jogador (SFX)")]
    [Tooltip("Som executado ao desferir um soco (toca na acao do golpe).")]
    [SerializeField] private AudioClip punchSFX;
    [Tooltip("Som executado durante o slide (termina junto com a duracao do slide).")]
    [SerializeField] private AudioClip slideSFX;

    [Header("Configuracoes de Invulnerabilidade ao Tomar Dano")]
    [SerializeField] private float invulnerabilityDuration = 1.2f;

    [Header("Sensibilidade de Toque")]
    [SerializeField] private float minSwipeDistance = 15f;

    [Header("Verificacao de Chao")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Componentes internos
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource slideAudioSource; // AudioSource dedicado para permitir corte exato no fim do slide

    // Vida e Invulnerabilidade
    private int currentHealth;
    private int maxHealth;
    private bool isInvulnerable = false;

    // Variaveis de fisica
    private float calculatedGravity;
    private float calculatedInitialJumpVelocity;
    private float calculatedBounceVelocity;
    private float jumpBufferTimer = 0f;

    // Cache do Animator
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

    // Estados
    private float currentRunSpeed;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isSliding;
    private bool isGroundPounding;

    // Combate
    private int comboStep = 0;
    private bool canCombo = false;
    private bool bufferNextAttack = false;
    private Coroutine currentComboCoroutine;

    public bool IsGroundPounding => isGroundPounding;
    public float CurrentRunSpeed => currentRunSpeed;
    public int CurrentHealth => currentHealth;

    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector2 startTouchPos;
    private bool isTouching;
    private bool gestureConsumed;
    private bool touchBeganOverUI;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Configura o AudioSource dedicado para o Slide
        slideAudioSource = gameObject.AddComponent<AudioSource>();
        slideAudioSource.playOnAwake = false;
        slideAudioSource.loop = false;

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

    private void Start()
    {
        // Aplica os atributos atualizados do UpgradeManager no início
        ApplyUpgrades();
    }

    /// <summary>
    /// Recarrega a vida máxima e restaura os pontos de vida com base nos upgrades comprados.
    /// </summary>
    public void ApplyUpgrades()
    {
        maxHealth = (UpgradeManager.Instance != null) ? UpgradeManager.Instance.CurrentHealth : 1;
        currentHealth = maxHealth;

        if (HealthBarUI.Instance != null)
        {
            HealthBarUI.Instance.UpdateHealth(currentHealth, maxHealth);
        }
    }

    private void OnValidate()
    {
        RecalculateJumpPhysics();
    }

    private void RecalculateJumpPhysics()
    {
        if (timeToJumpApex <= 0.01f) timeToJumpApex = 0.01f;
        calculatedGravity = (2f * jumpHeight) / Mathf.Pow(timeToJumpApex, 2f);
        calculatedInitialJumpVelocity = calculatedGravity * timeToJumpApex;
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

    public void TakeDamage(int damageAmount)
    {
        if (isInvulnerable) return;

        currentHealth -= damageAmount;
        Debug.Log($"Jogador tomou dano! Vida restante: {currentHealth}");

        if (HealthBarUI.Instance != null)
        {
            HealthBarUI.Instance.UpdateHealth(currentHealth, maxHealth);
        }

        if (ScreenDamageFX.Instance != null)
        {
            ScreenDamageFX.Instance.TriggerShake(0.3f, 0.35f);
            ScreenDamageFX.Instance.TriggerRedFlash();
        }

        if (currentHealth <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        isInvulnerable = false;
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

    public void Bounce()
    {
        isGroundPounding = false;
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

    private bool IsPointerOverUI(int touchFingerId = -1)
    {
        if (EventSystem.current == null) return false;

        if (touchFingerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(touchFingerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void HandleInputLifecycle()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver))
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RequestJump();
        }

        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.touchId))
                {
                    touchBeganOverUI = true;
                    return;
                }

                touchBeganOverUI = false;
                startTouchPos = touch.screenPosition;
                isTouching = true;
                gestureConsumed = false;
            }
            else if (!touchBeganOverUI)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    CheckInstantGesture(touch.screenPosition);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    FinalizeTouch(touch.screenPosition);
                }
            }

            return;
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUI())
                {
                    touchBeganOverUI = true;
                    return;
                }

                touchBeganOverUI = false;
                startTouchPos = Mouse.current.position.ReadValue();
                isTouching = true;
                gestureConsumed = false;
            }
            else if (!touchBeganOverUI)
            {
                if (Mouse.current.leftButton.isPressed && isTouching)
                {
                    CheckInstantGesture(Mouse.current.position.ReadValue());
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame && isTouching)
                {
                    FinalizeTouch(Mouse.current.position.ReadValue());
                }
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

    private void PlayPunchSound()
    {
        if (AudioManager.Instance != null && punchSFX != null)
        {
            AudioManager.Instance.PlaySFX(punchSFX);
        }
    }

    private IEnumerator ComboSequenceRoutine()
    {
        comboStep = 1;
        bufferNextAttack = false;
        canCombo = true;

        // Dispara o som no momento em que o 1º golpe é iniciado
        PlayPunchSound();

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

            // Dispara o som no momento em que o 2º golpe é iniciado
            PlayPunchSound();

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
            StopSlideImmediate();
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

        // Inicia o áudio do slide usando o volume do SFX configurado no AudioManager
        if (slideAudioSource != null && slideSFX != null)
        {
            float sfxVol = (AudioManager.Instance != null) ? AudioManager.Instance.SFXVolume : 1f;
            slideAudioSource.clip = slideSFX;
            slideAudioSource.volume = sfxVol;
            slideAudioSource.Play();
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
            capsuleCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y * 0.25f));
        }

        // Aguarda exatamente o tempo configurado para o slide
        yield return new WaitForSeconds(slideDuration);

        // Interrompe o som do slide sincronizado com o fim do slide
        StopSlideImmediate();
    }

    private void StopSlideImmediate()
    {
        if (slideAudioSource != null && slideAudioSource.isPlaying)
        {
            slideAudioSource.Stop();
        }

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