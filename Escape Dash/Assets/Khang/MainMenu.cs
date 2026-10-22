using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Called by the Start/Play button
    public void PlayGame()
    {
        // Loads the next scene in Build Settings (index 0 = Menu, index 1 = Gameplay)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Called by the Exit button
    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit(); // Works in a built/standalone player

#if UNITY_EDITOR
        // Also stops Play mode when testing inside the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
