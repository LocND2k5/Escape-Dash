using UnityEngine;

// Gắn lên object gốc của con Bull (hoặc Dog, Wolf...). KHÔNG cần collider.
// Con vật luôn chạy theo sau Player một khoảng "gap".
// - Bình thường: giữ khoảng cách normalGap.
// - Player vấp: gọi Stumble() -> áp sát, rồi lùi dần lại.
// - Game Over: lao tới Player và chạy animation Attack.
//
// Animator: tạo Animator Controller có state tên: Gallop, Attack (không cần transition).
public class ChaserController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Distance")]
    [SerializeField] private float normalGap = 7f;
    [SerializeField] private float stumbleGap = 2.5f;
    [SerializeField] private float caughtGap = 1.2f;
    [SerializeField] private float stumbleDuration = 4f;

    [Header("Speed")]
    [SerializeField] private float closeSpeed = 10f;
    [SerializeField] private float recoverSpeed = 1.5f;
    [SerializeField] private float xFollowSpeed = 5f;

    private float currentGap;
    private float targetGap;
    private float stumbleTimer;
    private float groundY;
    private bool caught;

    private void Start()
    {
        groundY = transform.position.y;
        currentGap = targetGap = normalGap;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        Play("Gallop");
    }

    private void OnEnable() { GameEvents.OnGameOver += OnCaught; }
    private void OnDisable() { GameEvents.OnGameOver -= OnCaught; }

    public void Stumble()
    {
        if (caught) return;
        targetGap = stumbleGap;
        stumbleTimer = stumbleDuration;
    }

    private void OnCaught()
    {
        caught = true;
        targetGap = caughtGap;
        Play("Attack");
    }

    private void Update()
    {
        if (player == null) return;
        float dt = Time.deltaTime;

        if (!caught && stumbleTimer > 0f)
        {
            stumbleTimer -= dt;
            if (stumbleTimer <= 0f) targetGap = normalGap;
        }

        float speed = currentGap > targetGap ? closeSpeed : recoverSpeed;
        currentGap = Mathf.MoveTowards(currentGap, targetGap, speed * dt);

        Vector3 pos = transform.position;
        pos.z = player.position.z - currentGap;
        pos.x = Mathf.Lerp(pos.x, player.position.x, xFollowSpeed * dt);
        pos.y = groundY;
        transform.position = pos;
    }

    private void Play(string state)
    {
        if (animator != null) animator.CrossFade(state, 0.1f);
    }
}
