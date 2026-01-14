using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    private Vector2 moveInput;
    public CharacterController controller;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    private Vector3 velocity;
    public int maxJumps = 2;
    private int jumpCount = 0;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    // ---------------- ATTACK ----------------
    [Header("Attack")]
    public float attackCooldown = 0.4f;
    public float attackRange = 2f;
    public LayerMask enemyLayer;

    private float attackTimer = 0f;
    private bool isAttacking = false;

    // ---------------- ANIMATION ----------------
    [Header("Animation")]
    public Animator animator;

    // ---------------- SOUND ----------------
    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip attackSound;

    void Update()
    {
        HandleDashTimers();
        HandleAttackTimer();

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        Vector3 move = Vector3.zero;
        if (!isDashing && !isAttacking)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight   = Camera.main.transform.right;

            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();

            move = camForward * moveInput.y + camRight * moveInput.x;
            controller.Move(move * speed * Time.deltaTime);

            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(move);
        }

        if (!isDashing)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(move);
    }

    // ---------------- INPUT SYSTEM ----------------

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (jumpCount < maxJumps)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpCount++;

                if (animator != null)
                    animator.SetTrigger("Jump");
            }
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && !isDashing && dashCooldownTimer <= 0f)
        {
            StartDash();

            if (animator != null)
                animator.SetTrigger("Dash");
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && attackTimer <= 0f && !isDashing)
        {
            DoAttack();
        }
    }

    // ---------------- DASH ----------------

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight   = Camera.main.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;

        dashDirection = inputDir.sqrMagnitude > 0.1f ? 
                        inputDir.normalized : 
                        transform.forward;

        velocity.y = 0f;
    }

    private void HandleDashTimers()
    {
        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);

            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashCooldownTimer = dashCooldown;
            }
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    // ---------------- ATTACK ----------------

    void HandleAttackTimer()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0)
                isAttacking = false;
        }
    }

    void DoAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        if (animator != null)
            animator.SetTrigger("Attack");

        // 🔊 PLAY ATTACK SOUND
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Raycast tấn công enemy
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, attackRange, enemyLayer))
        {
            Debug.Log("You hit: " + hit.collider.name);
            Destroy(hit.collider.gameObject);
        }
    }

    // ---------------- ANIMATION ----------------

    void UpdateAnimator(Vector3 move)
    {
        if (animator == null) return;

        animator.SetBool("isGrounded", controller.isGrounded);

        float speedPercent = move.magnitude;
        animator.SetFloat("Speed", speedPercent);

        animator.SetBool("isDashing", isDashing);
        animator.SetBool("isAttacking", isAttacking);

        animator.SetFloat("VerticalVelocity", velocity.y);
    }
}
