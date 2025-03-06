using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// Handles the loading of different scenes in the game.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Loads the specified scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    public void SceneSwitch(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
