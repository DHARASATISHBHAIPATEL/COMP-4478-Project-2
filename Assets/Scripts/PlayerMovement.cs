using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    public bool canMove = true;

    private Rigidbody2D rigidbodyComponent;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float inputX;
    private bool isRunning;
    private bool jumpPressed;
    private bool isGrounded;

    void Start()
    {
        rigidbodyComponent = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!canMove)
        {
            inputX = 0;
            isRunning = false;
            UpdateAnimations();
            return;
        }

        inputX = Input.GetAxisRaw("Horizontal");
        isRunning = Input.GetButton("Fire3");

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
        }

        if (inputX > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (inputX < 0)
        {
            spriteRenderer.flipX = true;
        }

        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(rigidbodyComponent.linearVelocity.x));
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("yVelocity", rigidbodyComponent.linearVelocity.y);
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        float speed = isRunning ? runSpeed : walkSpeed;
        rigidbodyComponent.linearVelocity = new Vector2(inputX * speed, rigidbodyComponent.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rigidbodyComponent.linearVelocity = new Vector2(rigidbodyComponent.linearVelocity.x, jumpForce);
        }
        jumpPressed = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}