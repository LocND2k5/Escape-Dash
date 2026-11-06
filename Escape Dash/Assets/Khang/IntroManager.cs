using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    [Header("Intro UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Loading")]
    [SerializeField] private string menuSceneName = "MenuScence";
    [SerializeField, Min(0.1f)] private float minimumLoadingTime = 2f;

    private void Start()
    {
        if (loadingBar != null)
            loadingBar.value = 0f;

        StartCoroutine(LoadMenuScene());
    }

    private IEnumerator LoadMenuScene()
    {
        AsyncOperation loadingOperation =
            SceneManager.LoadSceneAsync(menuSceneName);

        if (loadingOperation == null)
        {
            Debug.LogError($"IntroManager: Scene '{menuSceneName}' was not found or cannot be loaded.");
            yield break;
        }

        loadingOperation.allowSceneActivation = false;

        float elapsedTime = 0f;
        while (elapsedTime < minimumLoadingTime)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float timeProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);
            float sceneProgress = Mathf.Clamp01(loadingOperation.progress / 0.9f);
            float displayedProgress = Mathf.Min(timeProgress, sceneProgress);

            SetLoadingProgress(displayedProgress);
            yield return null;
        }

        while (!loadingOperation.isDone &&
               loadingOperation.progress < 0.9f)
        {
            SetLoadingProgress(Mathf.Clamp01(loadingOperation.progress / 0.9f));
            yield return null;
        }

        SetLoadingProgress(1f);
        yield return null;

        loadingOperation.allowSceneActivation = true;
    }

    private void SetLoadingProgress(float progress)
    {
        if (loadingBar != null)
            loadingBar.value = progress;

        if (loadingText != null)
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
    }
}
