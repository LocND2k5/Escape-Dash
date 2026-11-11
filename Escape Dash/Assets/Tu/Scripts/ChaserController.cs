using UnityEngine;

// Con vật luôn chạy theo sau Player một khoảng "gap".
// - Đầu game: Bull ở gần (introGap), giữ introHoldTime giây rồi tụt từ từ ra sau camera (normalGap).
// - Bình thường: giữ khoảng cách normalGap (nằm sau camera nên không thấy).
// - Player vấp: Stumble() -> áp sát, rồi lùi dần lại.
// - Game Over: lao tới Player và chạy animation Attack.
public class ChaserController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [Tooltip("Tùy chọn: model Bull (object CON). Tự ẩn khi gap lớn hơn hideGap. Để trống nếu không dùng.")]
    [SerializeField] private GameObject model;

    [Header("Distance")]
    [SerializeField] private float normalGap = 15f;    // nằm sau camera -> không thấy
    [SerializeField] private float stumbleGap = 2.5f;
    [SerializeField] private float caughtGap = 1.2f;
    [SerializeField] private float stumbleDuration = 4f;
    [SerializeField] private float hideGap = 12f;      // gap lớn hơn mức này thì ẩn model (nếu có gán model)

    [Header("Speed")]
    [SerializeField] private float closeSpeed = 10f;   // áp sát khi vấp / bị bắt
    [SerializeField] private float recoverSpeed = 3f;  // lùi lại sau khi vấp
    [SerializeField] private float xFollowSpeed = 5f;

    [Header("Intro (lúc mới vào game)")]
    [SerializeField] private float introGap = 5f;          // khoảng cách ban đầu, phải nhìn thấy trong camera
    [SerializeField] private float introHoldTime = 1.2f;   // giữ Bull ở gần bao lâu rồi mới tụt
    [SerializeField] private float introStartSpeed = 0.8f; // tốc độ tụt lúc bắt đầu (rất chậm)
    [SerializeField] private float introDropSpeed = 3.5f;  // tốc độ tụt tối đa ở cuối (tăng dần từ Start -> Drop)

    private float currentGap;
    private float targetGap;
    private float stumbleTimer;
    private float introTimer;
    private bool introDropping;
    private float groundY;
    private bool caught;

    private void Start()
    {
        groundY = transform.position.y;
        currentGap = targetGap = introGap;
        introTimer = introHoldTime;
        introDropping = false;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        Play("Gallop");
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += OnCaught;
        GameEvents.OnPlayerRevived += ResetChaser;
        GameEvents.OnPlayerStumbled += OnPlayerStumbled;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= OnCaught;
        GameEvents.OnPlayerRevived -= ResetChaser;
        GameEvents.OnPlayerStumbled -= OnPlayerStumbled;
    }

    private void OnPlayerStumbled(float duration)
    {
        stumbleDuration = duration;
        Stumble();
    }

    public void Stumble()
    {
        if (caught) return;
        introTimer = 0f;          // huỷ intro nếu đang chạy
        introDropping = false;
        targetGap = stumbleGap;
        stumbleTimer = stumbleDuration;
    }

    private void OnCaught()
    {
        caught = true;
        introTimer = 0f;
        introDropping = false;
        targetGap = caughtGap;
        Play("Attack");
    }

    public void ResetChaser()
    {
        caught = false;
        introTimer = 0f;
        introDropping = false;
        currentGap = targetGap = normalGap;
        stumbleTimer = 0f;
        Play("Gallop");
    }

    private void Update()
    {
        if (player == null) return;
        float dt = Time.deltaTime;

        // Intro: giữ Bull ở gần một lúc rồi cho tụt từ từ
        if (!caught && introTimer > 0f)
        {
            introTimer -= dt;
            if (introTimer <= 0f)
            {
                targetGap = normalGap;
                introDropping = true;
            }
        }

        // Hết thời gian áp sát sau khi vấp -> lùi về normalGap
        if (!caught && stumbleTimer > 0f)
        {
            stumbleTimer -= dt;
            if (stumbleTimer <= 0f) targetGap = normalGap;
        }

        // Chọn tốc độ
        float speed;
        if (currentGap > targetGap)
        {
            speed = closeSpeed;                                    // áp sát (nhanh)
        }
        else if (introDropping)
        {
            float t = Mathf.InverseLerp(introGap, normalGap, currentGap); // 0 -> 1
            speed = Mathf.Lerp(introStartSpeed, introDropSpeed, t);       // đầu chậm, sau nhanh dần
        }
        else
        {
            speed = recoverSpeed;                                  // lùi sau khi vấp
        }

        currentGap = Mathf.MoveTowards(currentGap, targetGap, speed * dt);

        if (introDropping && Mathf.Approximately(currentGap, normalGap))
            introDropping = false;

        // Đặt vị trí
        Vector3 pos = transform.position;
        pos.z = player.position.z - currentGap;
        pos.x = Mathf.Lerp(pos.x, player.position.x, xFollowSpeed * dt);
        pos.y = groundY;
        transform.position = pos;

        // Ẩn model khi ở quá xa (chỉ khi có gán model)
        if (model != null)
        {
            bool visible = currentGap < hideGap;
            if (model.activeSelf != visible) model.SetActive(visible);
        }
    }

    private void Play(string state)
    {
        if (animator != null) animator.CrossFade(state, 0.1f);
    }
}