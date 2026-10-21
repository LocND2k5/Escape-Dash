using UnityEngine;

// Gắn lên Player (root). Pivot của Player nằm ở CHÂN. Tag = "Player".
// Cần: CapsuleCollider (không trigger) + Rigidbody (Is Kinematic). Model là object con.
// KHÔNG gắn chung với TempPlayerRunner.
//
// MAP CHẠY, PLAYER ĐỨNG YÊN TRÊN TRỤC Z:
// Player chỉ đổi làn (X) và nhảy (Y). TrackManager đọc CurrentSpeed để kéo các khúc đường
// về phía Player, nên mọi tốc độ (tăng tốc dần, giảm tốc khi vấp) đều nằm ở script này.
//
// Animator: state tên đúng như sau (không cần transition): Run, JumpStart, JumpLoop, Roll, Hit, Death
//
// Luật chơi:
// - Chạm Obstacle lần 1: giảm tốc, phát animation Hit, Bull áp sát.
// - Chạm Obstacle lần nữa trong dangerDuration giây: Bull bắt được -> Game Over.
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Speed (tốc độ cuộn của map)")]
    public float forwardSpeed = 8f;
    public float maxSpeed = 20f;
    public float speedIncreasePerSecond = 0.1f;

    [Header("Lane")]
    public float laneDistance = 2f;
    public float laneChangeSpeed = 12f;

    [Header("Jump & Slide")]
    public float jumpForce = 8f;
    public float gravity = -22f;
    public float slideDuration = 0.8f;
    public float slideHeightRatio = 0.5f;

    [Header("Hit Penalty (vấp Obstacle)")]
    public float hitSlowFactor = 0.4f;     // tốc độ còn bao nhiêu % ngay khi vấp (0.4 = 40%)
    public float slowDuration = 2f;        // thời gian tốc độ hồi phục về bình thường
    public float dangerDuration = 4f;      // vấp lần nữa trong khoảng này -> bị Bull bắt
    public float invulnerableTime = 0.8f;  // miễn nhiễm ngắn sau khi vấp (tránh bị tính 2 lần)
    public float hitAnimDuration = 0.6f;   // thời gian phát animation Hit

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float fadeTime = 0.1f;
    [SerializeField] private float jumpLoopDelay = 0.25f;

    // Tốc độ thực tế mà map đang cuộn (đã tính giảm tốc khi vấp; = 0 khi đã thua)
    public float CurrentSpeed
    {
        get
        {
            if (isDead) return 0f;
            float multiplier = 1f;
            if (slowTimer > 0f)
                multiplier = Mathf.Lerp(1f, hitSlowFactor, slowTimer / slowDuration);
            return forwardSpeed * multiplier;
        }
    }

    private int currentLane = 1;
    private float verticalVelocity;
    private bool isGrounded = true;
    private bool isSliding;
    private bool isDead;
    private float slideTimer;
    private float airTime;
    private bool jumpLoopStarted;
    private float groundY;

    private float slowTimer;
    private float dangerTimer;
    private float invulnTimer;
    private float hitAnimTimer;

    private CapsuleCollider col;
    private float originalHeight;
    private Vector3 originalCenter;

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

    private void OnEnable()
    {
        GameEvents.OnGameOver += OnGameOver;
        GameEvents.OnPlayerHit += HandleHit;
        GameEvents.OnPlayerRevived += Revive;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= OnGameOver;
        GameEvents.OnPlayerHit -= HandleHit;
        GameEvents.OnPlayerRevived -= Revive;
    }

    private void OnGameOver()
    {
        isDead = true;
        Play("Death");
    }

    public void Revive()
    {
        isDead = false;
        slowTimer = dangerTimer = invulnTimer = hitAnimTimer = 0f;
        verticalVelocity = 0f;
        currentLane = 1; // Về lại làn giữa an toàn
        var pos = transform.position;
        pos.x = 0f;
        pos.y = groundY;
        transform.position = pos;

        if (col != null)
        {
            col.height = originalHeight;
            col.center = originalCenter;
        }
        isSliding = false;
        Play("Run");
    }

    private void HandleHit()
    {
        if (isDead || invulnTimer > 0f) return;

        // Vấp lần nữa khi Bull còn đang áp sát -> bị bắt
        if (dangerTimer > 0f)
        {
            GameEvents.RaiseGameOver();
            return;
        }

        slowTimer = slowDuration;
        dangerTimer = dangerDuration;
        invulnTimer = invulnerableTime;

        if (isGrounded && !isSliding)
        {
            hitAnimTimer = hitAnimDuration;
            Play("Hit");
        }

        GameEvents.RaisePlayerStumbled(dangerDuration);
    }

    private void Update()
    {
        if (isDead) return;

        TickTimers();
        HandleInput();
        HandleSlideTimer();
        Move();
    }

    private void TickTimers()
    {
        float dt = Time.deltaTime;
        if (slowTimer > 0f) slowTimer -= dt;
        if (dangerTimer > 0f) dangerTimer -= dt;
        if (invulnTimer > 0f) invulnTimer -= dt;

        if (hitAnimTimer > 0f)
        {
            hitAnimTimer -= dt;
            if (hitAnimTimer <= 0f && isGrounded && !isSliding) Play("Run");
        }
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

        // Tăng tốc dần (map cuộn nhanh dần). Player KHÔNG di chuyển theo Z.
        forwardSpeed = Mathf.Min(forwardSpeed + speedIncreasePerSecond * dt, maxSpeed);

        float targetX = (currentLane - 1) * laneDistance;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * dt);

        if (!isGrounded)
        {
            airTime += dt;
            verticalVelocity += gravity * dt;
            pos.y += verticalVelocity * dt;

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
        hitAnimTimer = 0f;
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
        hitAnimTimer = 0f;

        if (!isGrounded) verticalVelocity = -jumpForce;

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