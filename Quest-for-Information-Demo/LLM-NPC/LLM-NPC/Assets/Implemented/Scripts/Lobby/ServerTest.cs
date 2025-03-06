using System.Collections;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;


/// <summary>
/// Periodically pings the server to check whether the chatbot is operational.
/// Manages the visibility of UI buttons based on the server's status.
/// </summary>
public class ServerTest : MonoBehaviour
{
    [SerializeField]
    private GameObject _startGameButton;

    [SerializeField]
    private GameObject _serverUnavailableButton;

    [SerializeField]
    private bool _viewDebugLogs = false;


    void Start()
    {
        // IMPORTANT - currently we allow insecure HTTP requests
        // PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        _startGameButton.SetActive(false);
        _serverUnavailableButton.SetActive(true);
    }

    void Update()
    {
       StartCoroutine(CheckStatus());
    }

    /// <summary>
    /// Checks the server status by sending a GET request to the server.
    /// Changes the UI buttons based on the response.
    /// </summary>
    /// <returns>Coroutine enumerator. Unused.</returns>
    private IEnumerator CheckStatus()
    {
        var targetUrl = Constants.GetTestServerUrl();

        if (string.IsNullOrEmpty(targetUrl))
        {
            if (_viewDebugLogs) Debug.Log($"Request to: {targetUrl}. Url is null or empty.");
            yield break;
        }

        if (_viewDebugLogs) Debug.Log($"Pinging: {targetUrl}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(targetUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                ServerStatusResponse statusResponse = JsonUtility.FromJson<ServerStatusResponse>(jsonResponse);

                if (statusResponse.Status)
                {
                    if (_viewDebugLogs) Debug.Log("Server is fully initialized and ready to handle requests.");
                    EnableJoinRoomButton();
                }
                else
                {
                    if (_viewDebugLogs) Debug.Log("Server is not yet fully initialized.");
                    DisableJoinRoomButton();
                }
            }
            else
            {
                if (_viewDebugLogs) Debug.Log($"URL: {targetUrl} Failed to check server status: " + webRequest.error);
                DisableJoinRoomButton();
            }
        }
    }

    private void EnableJoinRoomButton()
    {
        if (!_startGameButton.activeSelf)
        {
            _startGameButton.SetActive(true);
            _serverUnavailableButton.SetActive(false);
        }
    }

    private void DisableJoinRoomButton()
    {
        if (!_serverUnavailableButton.activeSelf)
        {
            _serverUnavailableButton.SetActive(true);
            _startGameButton.SetActive(false);
        }
    }
}
