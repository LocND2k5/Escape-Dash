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
    public float slideHeightRatio = 0.4f;

    [Header("Power-up")]
    public float superJumpMultiplier = 1.8f;   // nhảy cao gấp bao nhiêu lần khi có Super Jump
    private float JumpMultiplier => PowerUpManager.IsActive(PowerUpType.SuperJump) ? superJumpMultiplier : 1f;

    [Header("Mặt đỡ (nóc tàu, dốc)")]
    [SerializeField] private LayerMask walkableMask;   // chọn layer "Walkable"
    [SerializeField] private float stepUp = 0.5f;      // MỚI: mặt đỡ cao hơn chân tối đa bao nhiêu thì tự bước lên
    [SerializeField] private float stepDown = 0.3f;    // MỚI: mặt đỡ thấp hơn chân tối đa bao nhiêu thì vẫn bám, hơn thì rơi

    [Header("Hit Penalty (vấp Obstacle)")]
    public float hitSlowFactor = 0.4f;     // tốc độ còn bao nhiêu % ngay khi vấp (0.4 = 40%)
    public float slowDuration = 2f;        // thời gian tốc độ hồi phục về bình thường
    public float dangerDuration = 4f;      // vấp lần nữa trong khoảng này -> bị Bull bắt
    public float invulnerableTime = 0.8f;  // miễn nhiễm ngắn sau khi vấp (tránh bị tính 2 lần)
    public float hitAnimDuration = 0.6f;   // thời gian phát animation Hit

    [Header("Revive Glow (vòng sáng sau khi hồi sinh)")]
    public float reviveGlowDuration = 3f;                          // 0 = tắt vòng sáng
    public float reviveGlowDiameter = 2.6f;
    public Color reviveGlowColor = new Color(1f, 0.78f, 0.15f, 1f); // vàng cam sáng (kiểu Temple Run)

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
            return forwardSpeed * SlowMultiplier;
        }
    }

    // Hệ số giảm tốc khi vấp: 1 = bình thường; vừa vấp = hitSlowFactor (0.4), hồi dần về 1 trong slowDuration giây
    public float SlowMultiplier
    {
        get
        {
            if (slowTimer <= 0f) return 1f;
            return Mathf.Lerp(1f, hitSlowFactor, slowTimer / slowDuration);
        }
    }

    private int currentLane = 1;
    private int previousLane = 1;   // MỚI: làn trước khi đổi, để bị đẩy về khi va vào hông vật cản
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
        slowTimer = slowDuration; // Hồi sinh xong cũng bị giảm tốc như khi vấp (còn 40%, hồi dần)
        ReviveGlow.Show(transform,
                        col != null ? col.center : new Vector3(0f, 0.9f, 0f),
                        reviveGlowDiameter, reviveGlowDuration, reviveGlowColor);
        verticalVelocity = 0f;
        currentLane = 1; // Về lại làn giữa an toàn
        previousLane = 1;
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

    // MỚI: va vào hông vật cản khi đang đổi làn -> bị đẩy về làn cũ (không xuyên qua). Gọi bởi SideBump.
    public void BumpBack()
    {
        if (isDead) return;
        currentLane = previousLane;
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
        int laneBefore = currentLane;   // MỚI
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            currentLane = Mathf.Max(0, currentLane - 1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            currentLane = Mathf.Min(2, currentLane + 1);
        if (currentLane != laneBefore) previousLane = laneBefore;   // MỚI

        if (isGrounded && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
            Jump();

        if (!isSliding && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
            StartSlide();
    }


    // MỚI: độ cao mặt đỡ ngay dưới chân (nóc container...), không có thì là mặt đường gốc.
    private float GetGroundHeight(Vector3 pos)
    {
        if (walkableMask.value == 0) return groundY;

        // Map chạy bằng cách dời transform mỗi frame, nên đồng bộ vật lý trước khi bắn tia
        Physics.SyncTransforms();

        float above = stepUp + 0.1f;   // tia bắt đầu cao hơn chân một chút
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

        // Tăng tốc dần (map cuộn nhanh dần). Player KHÔNG di chuyển theo Z.
        forwardSpeed = Mathf.Min(forwardSpeed + speedIncreasePerSecond * dt, maxSpeed);

        float targetX = (currentLane - 1) * laneDistance;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * dt);

        // MỚI: độ cao mặt đỡ dưới chân
        float ground = GetGroundHeight(pos);

        if (isGrounded)
        {
            float diff = ground - pos.y;
            if (diff >= -stepDown && diff <= stepUp)
                pos.y = ground;              // bám theo mặt đỡ (đứng trên nóc container)
            else if (diff < -stepDown)
                StartFalling();              // hết nóc / sang làn khác -> rơi xuống
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

            // MỚI: đáp xuống mặt đỡ (chỉ khi đang đi xuống và trước đó không thấp hơn mặt đỡ quá stepUp)
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

    // MỚI: bắt đầu rơi (hết mặt đỡ)
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

        if (!isGrounded) verticalVelocity = -jumpForce * JumpMultiplier;   // lướt xuống nhanh tương ứng khi đang ở trên cao

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

    // Chờ animation "Death" chạy xong (hoặc quá maxWait giây thì thôi, phòng khi thiếu state).
    public System.Collections.IEnumerator WaitForDeathAnimation(float maxWait)
    {
        if (animator == null) yield break;

        float t = 0f;
        while (t < maxWait)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Death") && !animator.IsInTransition(0) && info.normalizedTime >= 1f)
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void Play(string state)
    {
        if (animator != null) animator.CrossFade(state, fadeTime);
    }
}