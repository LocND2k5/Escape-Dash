using UnityEngine;

// Gắn lên Player (root). Pivot của Player nằm ở CHÂN. Tag = "Player".
// Cần: CapsuleCollider (không trigger) + Rigidbody (Is Kinematic). Model là object con.
//
// MAP CHẠY, PLAYER ĐỨNG YÊN TRÊN TRỤC Z: Player chỉ đổi làn (X) và nhảy (Y).
// Animator: state tên đúng như sau (không cần transition): Run, JumpStart, JumpLoop, Roll, Hit, Death
//
// Luật chơi (giống Subway Surfers):
// - Obstacle Light: vấp lần 1, vấp lần nữa trong dangerDuration giây -> Game Over.
// - Obstacle Heavy: Game Over ngay.
//
// MỚI: MẶT ĐỠ (nóc tàu, dốc). Mỗi frame bắn 1 tia xuống dưới chân, chỉ trúng collider thuộc
// layer "walkableMask". Độ cao trúng = mặt đất mới. Có dốc thì tự chạy lên, hết nóc thì rơi xuống.
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

    [Header("Power-up")]
    public float superJumpMultiplier = 1.8f;   // nhảy cao gấp bao nhiêu lần khi có Super Jump

    [Header("Mặt đỡ (nóc tàu, dốc)")]
    [SerializeField] private LayerMask walkableMask;   // chọn layer "Walkable"
    [SerializeField] private float stepUp = 0.5f;      // cao hơn chân tối đa bao nhiêu thì tự bước lên (dốc)
    [SerializeField] private float stepDown = 0.3f;    // thấp hơn chân tối đa bao nhiêu thì vẫn bám (xuống dốc), hơn thì rơi

    [Header("Hit Penalty (vấp Obstacle nhẹ)")]
    public float dangerDuration = 4f;
    public float invulnerableTime = 0.8f;
    public float hitAnimDuration = 0.6f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float fadeTime = 0.1f;
    [SerializeField] private float jumpLoopDelay = 0.25f;

    public float CurrentSpeed
    {
        get
        {
            if (isDead) return 0f;
            return forwardSpeed;
        }
    }

    public bool IsInDanger => dangerTimer > 0f;

    private float JumpMultiplier => PowerUpManager.IsActive(PowerUpType.SuperJump) ? superJumpMultiplier : 1f;

    private int currentLane = 1;
    private float verticalVelocity;
    private bool isGrounded = true;
    private bool isSliding;
    private bool isDead;
    private float slideTimer;
    private float airTime;
    private bool jumpLoopStarted;
    private float groundY;          // độ cao mặt đường gốc

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

        TickTimers();
        HandleInput();
        HandleSlideTimer();
        Move();
    }

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

    // Độ cao mặt đỡ ngay dưới chân (dốc / nóc tàu), không có thì là mặt đường gốc.
    private float GetGroundHeight(Vector3 pos)
    {
        if (walkableMask.value == 0) return groundY;

        // Map chạy bằng cách dời transform mỗi frame, nên đồng bộ vật lý trước khi bắn tia
        Physics.SyncTransforms();

        float above = stepUp + 0.1f;   // tia bắt đầu cao hơn chân một chút để thấy được dốc đang dâng lên
        Vector3 origin = pos + Vector3.up * above;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, above + 60f,
                            walkableMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(groundY, hit.point.y);

        return groundY;
    }

    private void Move()
    {
        float dt = Time.deltaTime;
        Vector3 pos = transform.position;

        forwardSpeed = Mathf.Min(forwardSpeed + speedIncreasePerSecond * dt, maxSpeed);

        float targetX = (currentLane - 1) * laneDistance;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * dt);

        float ground = GetGroundHeight(pos);

        if (isGrounded)
        {
            float diff = ground - pos.y;
            if (diff >= -stepDown && diff <= stepUp)
                pos.y = ground;              // bám theo dốc / mặt nóc
            else if (diff < -stepDown)
                StartFalling();              // hết mặt đỡ (cuối nóc, ra khỏi làn) -> rơi
            // diff > stepUp: bức tường quá cao, bỏ qua (trigger Obstacle sẽ xử lý)
        }

        if (!isGrounded)
        {
            float oldY = pos.y;
            airTime += dt;
            verticalVelocity += gravity * dt;
            pos.y += verticalVelocity * dt;

            if (!jumpLoopStarted && !isSliding && airTime >= jumpLoopDelay)
            {
                jumpLoopStarted = true;
                Play("JumpLoop");
            }

            // Chỉ đáp xuống mặt đỡ nếu trước đó không thấp hơn nó quá stepUp (tránh bị "hút" lên nóc từ bên dưới)
            if (verticalVelocity <= 0f && pos.y <= ground && ground - oldY <= stepUp)
            {
                pos.y = ground;
                verticalVelocity = 0f;
                isGrounded = true;
                airTime = 0f;
                jumpLoopStarted = false;
                if (!isSliding) Play("Run");
            }
        }

        transform.position = pos;
    }

    private void StartFalling()
    {
        isGrounded = false;
        verticalVelocity = 0f;
        airTime = 0f;
        jumpLoopStarted = false;
        hitAnimTimer = 0f;
    }

    private void Jump()
    {
        if (isSliding) EndSlide(false);
        hitAnimTimer = 0f;
        verticalVelocity = jumpForce * JumpMultiplier;
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

        if (!isGrounded) verticalVelocity = -jumpForce * JumpMultiplier;

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
