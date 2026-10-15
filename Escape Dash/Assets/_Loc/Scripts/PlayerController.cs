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
// MỚI: luật chơi hiện tại là CHẠM 1 LẦN LÀ THUA. Obstacle bắn thẳng GameEvents.OnGameOver,
// nên phần vấp lần 1 (giảm tốc, animation Hit, danger timer) đã được bỏ khỏi script này.
// State "Hit" trong Animator không còn được dùng.
// MỚI: phần tốc độ (forwardSpeed, CurrentSpeed) vẫn giữ vì ChaserController còn đọc.
// Tốc độ cuộn map thật sự do ChunkSpawner điều khiển (startSpeed, maxSpeed, acceleration).
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // MỚI: khôi phục nhóm Speed vì ChaserController (và script khác) còn đọc CurrentSpeed.
    // Lưu ý: map thật sự do ChunkSpawner điều khiển, giá trị này chỉ để các script khác tham khảo.
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

    // MỚI: đã bỏ nhóm [Header("Hit Penalty")] (hitSlowFactor, slowDuration, dangerDuration,
    // invulnerableTime, hitAnimDuration) vì không còn luật vấp lần 1.

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float fadeTime = 0.1f;
    [SerializeField] private float jumpLoopDelay = 0.25f;

    // Tốc độ thực tế mà map đang cuộn (= 0 khi đã thua)
    // MỚI: không còn nhân hệ số giảm tốc khi vấp (luật vấp đã bỏ)
    public float CurrentSpeed
    {
        get
        {
            if (isDead) return 0f;
            return forwardSpeed;
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

    // MỚI: đã bỏ slowTimer, dangerTimer, invulnTimer, hitAnimTimer

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
        // MỚI: đã bỏ đăng ký GameEvents.OnPlayerHit (không còn ai bắn sự kiện này)
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= OnGameOver;
        // MỚI: đã bỏ hủy đăng ký GameEvents.OnPlayerHit
    }

    private void OnGameOver()
    {
        isDead = true;
        Play("Death");
    }

    public void Revive()
    {
        isDead = false;
        // MỚI: không còn timer vấp để reset
        Play("Run");
    }

    // MỚI: đã bỏ HandleHit() (vấp lần 1: giảm tốc, animation Hit, RaisePlayerStumbled)

    private void Update()
    {
        if (isDead) return;

        // MỚI: đã bỏ TickTimers() vì các timer vấp không còn tồn tại
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

        // Tăng tốc dần (map cuộn nhanh dần). Player KHÔNG di chuyển theo Z.
        // MỚI: khôi phục dòng này để CurrentSpeed tăng dần như cũ.
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
        // MỚI: đã bỏ hitAnimTimer = 0f
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
        // MỚI: đã bỏ hitAnimTimer = 0f

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