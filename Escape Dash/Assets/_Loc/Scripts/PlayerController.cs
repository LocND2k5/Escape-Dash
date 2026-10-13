using UnityEngine;

// Gắn lên Player (root). Pivot của Player nằm ở CHÂN. Tag = "Player".
// Cần: CapsuleCollider (không trigger) + Rigidbody (Is Kinematic). Model là object con.
// KHÔNG gắn chung với TempPlayerRunner (hai script cùng điều khiển sẽ xung đột).
//
// Animator: tạo Animator Controller có các state tên đúng như dưới đây (không cần transition):
//   Run, JumpStart, JumpLoop, Roll, Death
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Forward")]
    public float forwardSpeed = 8f;
    public float maxSpeed = 20f;
    public float speedIncreasePerSecond = 0.1f;

    [Header("Lane")]
    public float laneDistance = 2f;      // khớp với TempPlayerRunner / vị trí Obstacle, Coin
    public float laneChangeSpeed = 12f;

    [Header("Jump & Slide")]
    public float jumpForce = 8f;
    public float gravity = -22f;
    public float slideDuration = 0.8f;   // chỉnh gần bằng độ dài clip Roll
    public float slideHeightRatio = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;   // Animator của model con
    [SerializeField] private float fadeTime = 0.1f;
    [SerializeField] private float jumpLoopDelay = 0.25f; // sau bấy lâu thì chuyển JumpStart -> JumpLoop

    private int currentLane = 1; // 0 trái, 1 giữa, 2 phải
    private float verticalVelocity;
    private bool isGrounded = true;
    private bool isSliding;
    private bool isDead;
    private float slideTimer;
    private float airTime;
    private bool jumpLoopStarted;
    private float groundY;

    private CapsuleCollider col;
    private float originalHeight;
    private Vector3 originalCenter;

    // Tự đặt collider mặc định khi vừa gắn script (nhân vật cao ~1.8, pivot ở chân)
    private void Reset()
    {
        var c = GetComponent<CapsuleCollider>();
        c.height = 1.8f;
        c.radius = 0.35f;
        c.center = new Vector3(0f, 0.9f, 0f);
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        originalHeight = col.height;
        originalCenter = col.center;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        groundY = transform.position.y;
        Play("Run");
    }

    private void OnEnable() { GameEvents.OnGameOver += OnGameOver; }
    private void OnDisable() { GameEvents.OnGameOver -= OnGameOver; }

    private void OnGameOver()
    {
        isDead = true;
        Play("Death");
    }

    public void Revive()
    {
        isDead = false;
        Play("Run");
    }

    private void Update()
    {
        if (isDead) return;

        HandleInput();
        HandleSlideTimer();
        Move();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            currentLane = Mathf.Max(0, currentLane - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            currentLane = Mathf.Min(2, currentLane + 1);

        if (isGrounded && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
            Jump();

        if (!isSliding && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
            StartSlide();
    }

    private void Move()
    {
        float dt = Time.deltaTime;
        Vector3 pos = transform.position;

        forwardSpeed = Mathf.Min(forwardSpeed + speedIncreasePerSecond * dt, maxSpeed);
        pos.z += forwardSpeed * dt;

        float targetX = (currentLane - 1) * laneDistance;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * dt);

        if (!isGrounded)
        {
            airTime += dt;
            verticalVelocity += gravity * dt;
            pos.y += verticalVelocity * dt;

            // Chuyển sang pose lơ lửng sau một lúc
            if (!jumpLoopStarted && !isSliding && airTime >= jumpLoopDelay)
            {
                jumpLoopStarted = true;
                Play("JumpLoop");
            }

            if (pos.y <= groundY)
            {
                pos.y = groundY;
                verticalVelocity = 0f;
                isGrounded = true;
                airTime = 0f;
                jumpLoopStarted = false;
                if (!isSliding) Play("Run");
            }
        }

        transform.position = pos;
    }

    private void Jump()
    {
        if (isSliding) EndSlide(false);
        verticalVelocity = jumpForce;
        isGrounded = false;
        airTime = 0f;
        jumpLoopStarted = false;
        Play("JumpStart");
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        if (!isGrounded) verticalVelocity = -jumpForce; // đang trên không thì lao xuống

        float newHeight = originalHeight * slideHeightRatio;
        col.height = newHeight;
        col.center = new Vector3(originalCenter.x,
                                 originalCenter.y - (originalHeight - newHeight) / 2f,
                                 originalCenter.z);
        Play("Roll");
    }

    private void HandleSlideTimer()
    {
        if (!isSliding) return;
        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f) EndSlide(true);
    }

    private void EndSlide(bool playRun)
    {
        isSliding = false;
        col.height = originalHeight;
        col.center = originalCenter;
        if (playRun && isGrounded) Play("Run");
    }

    private void Play(string state)
    {
        if (animator != null) animator.CrossFade(state, fadeTime);
    }
}