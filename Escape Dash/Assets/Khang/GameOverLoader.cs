using System.Collections; // MỚI: để dùng coroutine (IEnumerator)
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any GameObject in the Linh (Gameplay) scene.
/// When the player hits an obstacle → GameEvents.OnGameOver fires
/// → saves a flag → loads back to Khang (Hub) scene
/// → Khang's GameOverUI reads the flag and shows the Game Over panel.
/// </summary>
public class GameOverLoader : MonoBehaviour
{
    // Key used to signal Khang scene that Game Over should be shown
    public const string GameOverKey = "ShowGameOver";

    // MỚI: chờ animation Death chạy xong rồi mới chuyển scene
    [Header("Death Animation")]
    [SerializeField] private Animator playerAnimator;      // Kéo Animator của model Player vào (tùy chọn)
    [SerializeField] private string deathStateName = "Death"; // Tên state Death trong Animator
    [SerializeField] private float maxWait = 3f;           // Chờ tối đa bao nhiêu giây (phòng bị kẹt)
    [SerializeField] private float extraDelay = 0.3f;      // Nghỉ thêm một chút sau khi animation xong
    [SerializeField] private float fallbackDelay = 1.5f;   // Dùng khi KHÔNG gán Animator: chờ cố định

    private bool isLoading; // MỚI: tránh chạy 2 lần nếu OnGameOver bắn lại

    private void OnEnable() => GameEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver()
    {
        // MỚI: không load ngay nữa, chuyển sang coroutine để chờ animation
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(LoadAfterDeath());
    }

    // MỚI: chờ animation Death xong → lưu cờ → load scene Menu
    private IEnumerator LoadAfterDeath()
    {
        if (playerAnimator != null)
        {
            // Chờ 1 frame để PlayerController kịp gọi CrossFade("Death")
            yield return null;

            float waited = 0f;
            while (waited < maxWait)
            {
                AnimatorStateInfo info = playerAnimator.GetCurrentAnimatorStateInfo(0);
                // normalizedTime >= 1 nghĩa là clip Death đã chạy hết 1 lần
                if (info.IsName(deathStateName) && info.normalizedTime >= 1f) break;

                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (extraDelay > 0f) yield return new WaitForSecondsRealtime(extraDelay);
        }
        else
        {
            // Không gán Animator thì chờ một khoảng cố định
            yield return new WaitForSecondsRealtime(fallbackDelay);
        }

        // Save flag so Khang scene knows to show Game Over panel
        PlayerPrefs.SetInt(GameOverKey, 1);
        PlayerPrefs.Save();

        // Reset time before switching scenes
        Time.timeScale = 1f;

        // Load Khang hub scene (index 0 in Build Settings)
        SceneManager.LoadScene(0);
    }
}