using UnityEngine;


/// <summary>
/// Loads the IP of the server from the lobby and stores it in PlayerPrefs throughout the game
/// </summary>
public class ServerIPString : MonoBehaviour
{
    public static ServerIPString Instance { get; private set; }

    public string ServerIP
    {
        get => PlayerPrefs.GetString(Constants.ServerIPKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(Constants.ServerIPKey, value); 
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // When the player exits or at the end of the session, reset PlayerPrefs
    void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey(Constants.ServerIPKey);
        PlayerPrefs.Save();
    }
}
