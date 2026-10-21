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
//
// MỚI: luật chơi hiện tại giống Subway Surfers:
// - Obstacle loại Light (chạm nhẹ): vấp lần 1, phát animation Hit, bắt đầu dangerDuration giây.
//   Vấp thêm lần nữa trong khoảng này -> Game Over.
// - Obstacle loại Heavy (chạm mạnh): Game Over ngay (Obstacle bắn thẳng GameEvents.OnGameOver).
// MỚI: phần giảm tốc khi vấp không dùng vì map do ChunkSpawner điều khiển.
// MỚI: phần tốc độ (forwardSpeed, CurrentSpeed) vẫn giữ vì ChaserController còn đọc.
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

    // MỚI: khôi phục nhóm Hit Penalty, bỏ hitSlowFactor và slowDuration (không còn giảm tốc)
    [Header("Hit Penalty (vấp Obstacle nhẹ)")]
    public float dangerDuration = 4f;      // vấp lần nữa trong khoảng này -> Game Over
    public float invulnerableTime = 0.8f;  // miễn nhiễm ngắn sau khi vấp (tránh bị tính 2 lần)
    public float hitAnimDuration = 0.6f;   // thời gian phát animation Hit

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float fadeTime = 0.1f;
    [SerializeField] private float jumpLoopDelay = 0.25f;

    // Tốc độ thực tế mà map đang cuộn (= 0 khi đã thua)
    public float CurrentSpeed
    {
        get
        {
            if (isDead) return 0f;
            return forwardSpeed;
        }
    }

    // MỚI: true khi đang trong thời gian nguy hiểm (đã vấp 1 lần). UI/hiệu ứng có thể đọc.
    public bool IsInDanger => dangerTimer > 0f;

    private int currentLane = 1;
    private float verticalVelocity;
    private bool isGrounded = true;
    private bool isSliding;
    private bool isDead;
    private float slideTimer;
    private float airTime;
    private bool jumpLoopStarted;
    private float groundY;

    // MỚI: không còn slowTimer
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
        GameEvents.OnPlayerHit += HandleHit; // MỚI: nhận sự kiện chạm nhẹ từ Obstacle
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= OnGameOver;
        GameEvents.OnPlayerHit -= HandleHit; // MỚI
    }

    private void OnGameOver()
    {
        isDead = true;
        Play("Death");
    }

    public void Revive()
    {
        isDead = false;
        dangerTimer = invulnTimer = hitAnimTimer = 0f; // MỚI: không còn slowTimer
        Play("Run");
    }

    // MỚI: chạm nhẹ. Lần 1 -> vấp; lần 2 trong dangerDuration -> Game Over.
    private void HandleHit()
    {
        if (isDead || invulnTimer > 0f) return;

        // Vấp lần nữa khi còn trong thời gian nguy hiểm -> thua
        if (dangerTimer > 0f)
        {
            GameEvents.RaiseGameOver();
            return;
        }

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

        TickTimers(); // MỚI: khôi phục để đếm thời gian vấp
        HandleInput();
        HandleSlideTimer();
        Move();
    }

    // MỚI: đếm ngược các timer của cơ chế vấp
    private void TickTimers()
    {
        float dt = Time.deltaTime;
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
        hitAnimTimer = 0f; // MỚI: khôi phục
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
        hitAnimTimer = 0f; // MỚI: khôi phục

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