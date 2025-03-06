using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Main Class for managing an NPC
/// Handles the communication between STT, LLM client and TTS components
/// </summary>
public class NPCManager : MonoBehaviour
{
    // True if the NPC is hovered over. Indicates that the user is speaking / wants to speak to it.
    public bool InActiveConversation { get; set; } = false;

    // True if the LLM is running out of memory (we are at maximum tokens). Indicates that we need to shorten the memory by creating a summary.
    public bool NeedsMemoryRefresher { get; set; } = false;

    // Only used for testing the LLM -> To not have to use STT for every test interaction
    [SerializeField]
    private bool _testSpeech = false;   // Testing
    private bool _prevTestSpeech = false;  // Testing

    [SerializeField]
    private string _testingSpeechText = "Hello, my name is Tereza. Who are you?";   // Testing

    [SerializeField]
    private bool _viewDebugLogs = false;

    // Note - make sure that the name macthes the folder name as well as the name in the personality file
    [SerializeField]
    private string _characterName = "John";

    [SerializeField]
    private LLMClient _llmClient;

    [SerializeField]
    private SpeechToText _speechToText;

    [SerializeField]
    private TextToSpeech _textToSpeech;

    private bool _npcInProgress = false;

    [SerializeField]
    private GameObject _otherNPCSpeakingText;

    [SerializeField]
    private GameObject _processingText;

    [SerializeField]
    private GameObject _userPromptText;

    [SerializeField]
    private Outline _outline;

    private float _stopSpeakingDelay = 0.3f;

    void Start()
    {
        // IMPORTANT - currently we allow insecure HTTP requests
        //PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;


        if (_llmClient == null)
        {
            Debug.LogError($"LLMClient on character {_characterName} is not assigned! Please assign it in the inspector.");
        }
        if (_speechToText == null)
        {
            Debug.LogError($"Speech to text on character {_characterName} is not assigned! Please assign it in the inspector.");
        }
        if (_textToSpeech == null)
        {
            Debug.LogError($"Text to speech on character {_characterName} is not assigned! Please assign it in the inspector.");
        }

        _speechToText.SetNPCManager(this);
        _textToSpeech.SetNPCManager(this);
        _llmClient.SetNPCManager(this);

        _llmClient.InitializeSystemPrompt();
        _llmClient.InitializeQuestSystemPrompt();

        _otherNPCSpeakingText.SetActive(false);
        _processingText.SetActive(false);
        _userPromptText.SetActive(false);
        _outline.enabled = false;
    }

    /// <summary>
    /// Used to display text on when a chatbot can speak and when it needs to wait
    /// Also used to change outline color between red and green (whether NPC is usable or not)
    /// It will not be used to turn the outline on and off - that is done on hover.
    /// Additionally, enables testing without the need of VR os STT.
    /// </summary>
    void Update()
    {
        // An NPC is speaking and it's not this one
        if (GameManager.Instance.AnyChatbotSpeaking && !_npcInProgress)
        {
            _otherNPCSpeakingText.SetActive(true);
            _processingText.SetActive(false);
            _userPromptText.SetActive(false);
            _outline.OutlineColor = Constants.NegativeOutlineColor;
        }
        else if (_npcInProgress)
        {
            _otherNPCSpeakingText.SetActive(false);
            _processingText.SetActive(true);
            _outline.OutlineColor = Constants.NegativeOutlineColor;
        }
        else
        {
            _otherNPCSpeakingText.SetActive(false);
            _processingText.SetActive(false);
            _userPromptText.SetActive(false);
            _outline.OutlineColor = Constants.PositiveOutlineColor;
        }

        // Testing
        if (_testSpeech != _prevTestSpeech)
        {
            _prevTestSpeech = _testSpeech;

            ReceiveTranslatedSpeech(_testingSpeechText, Constants.TTSResult.Success);
        }
    }

    /// <summary>
    /// Called when the player activates the NPC.
    /// Initiates listening for player's speech input.
    /// </summary>
    public void PlayerStartSpeaking()
    {
        // Another chatbot is speaking, we won't interrupt
        if (GameManager.Instance.AnyChatbotSpeaking)
            return;

        if (_viewDebugLogs) Debug.Log($"Character: {_characterName} listening activated. Player can start speaking.");

        GameManager.Instance.AnyChatbotSpeaking = true;
        _npcInProgress = true;
        _speechToText.StartRecording();
    }

    /// <summary>
    /// Called when the player deactivates the object and when the hover exits (just in case).
    /// Starts the coroutine to stop listening to player input
    /// </summary>
    public void PlayerStopSpeaking()
    {
        if (_viewDebugLogs) Debug.Log($"Character : {_characterName} listening will be deactivated in {_stopSpeakingDelay}.");
        WaitForPlayerStop();
    }

    /// <summary>
    /// Receives the partially translated speech text and updates the viewable strign with it
    /// </summary>
    /// <param name="partialResponse">The partially translated text received from speech-to-text.</param>
    public void UpdatePlayerSpeechText(string partialResponse)
    {
        // Show translated speech
        if (partialResponse == "" || partialResponse == _userPromptText.GetNamedChild("Canvas").GetComponentInChildren<TextMeshProUGUI>().text)
            return;

        if (!_userPromptText.activeSelf)
            _userPromptText.SetActive(true);

        _userPromptText.GetNamedChild("Canvas").GetComponentInChildren<TextMeshProUGUI>().SetText("User prompt: " + partialResponse);
    }

    /// <summary>
    /// Receives the translated speech text and sends it to the LLM client
    /// </summary>
    /// <param name="text">The translated text received from speech-to-text.</param>
    public void ReceiveTranslatedSpeech(string text, Constants.TTSResult ttsResult)
    {
        // Show translated speech
        _userPromptText.SetActive(true);
        _userPromptText.GetNamedChild("Canvas").GetComponentInChildren<TextMeshProUGUI>().SetText("User prompt: " + text);

        switch (ttsResult)
        {
            case Constants.TTSResult.Success:
                SendPromptToLLM(text);
                break;
            case Constants.TTSResult.Timeout:
                SendTimeoutPromptToLLM();
                break;
            case Constants.TTSResult.NoData:
                SendNoDataPromptToLLM();
                break;
        }
    }

    /// <summary>
    /// Receives the response from the LLM
    /// </summary>
    /// <param name="response">Response received from the LLM.</param>
    public void ReceiveLLMResponse(string response)
    {
        if (_viewDebugLogs) Debug.Log($"Character : {_characterName} received response from LLM.");
        SendCharacterResponseToTTS(response);
    }

    /// <summary>
    /// The NPC has finished speaking.
    /// End the turn and inform other NPCs that they can speak.
    /// </summary>
    public void EndNPCProcessing()
    {
        _npcInProgress = false;
        GameManager.Instance.AnyChatbotSpeaking = false;
    }

    /// <summary>
    /// Gets the character's name.
    /// </summary>
    /// <returns>The name of the character.</returns>
    public string GetCharacterName()
    {
        return _characterName;
    }

    /// <summary>
    /// Creates a new system prompt for the character (typically when changing quests) and adds it to history
    /// </summary>
    public void UpdateSystemPrompt()
    {
        _llmClient.InitializeQuestSystemPrompt();
    }


    /// <summary>
    /// Called then the LLM is running out of tokens.
    /// Asks the LLMClient to refresh it's memory - Summarize the previous conversation and create a new history.
    /// </summary>
    public void StartMemoryRefresh()
    {
        NeedsMemoryRefresher = false;
        _llmClient.StartMemoryRefresh();
    }

    /// <summary>
    /// Stops listening for player's speech input (after _stopSpeakingDelay) and starts processing it.
    /// </summary>
    private IEnumerator WaitForPlayerStop()
    {
        yield return new WaitForSeconds(_stopSpeakingDelay);

        if (_speechToText.IsRecording())
            _speechToText.StopRecording();
    }

    /// <summary>
    /// Sends the prompt to the LLM client.
    /// </summary>
    /// <param name="prompt">The user input prompt.</param>
    private void SendPromptToLLM(string prompt)
    {
        _llmClient.SendLLMPromptToServer(prompt);
    }

    /// <summary>
    /// A timeout happened (player was speaking for too long). 
    /// This function generates a prompt for timeout and sends it to LLM.
    /// </summary>
    private void SendTimeoutPromptToLLM()
    {
        var prompt = "The player was speaking for too long and you didn't manage to catch the message. Generate a response that asks the player to speak in shorter chunks so you can answer them. Stick to character";
        _llmClient.SendSTTFailurePromptToServer(prompt);
    }

    /// <summary>
    /// A no data error happened (player was not speaking, or the microphone had not caught the speach). 
    /// This function generates a prompt for no data error and sends it to LLM.
    /// </summary>
    private void SendNoDataPromptToLLM()
    {
        var prompt = "The player was trying to speak to you but you didn't hear anything. Generate a response that asks the player to try speaking a bit louder, beacuse you didn't hear them. Stick to character.";
        _llmClient.SendSTTFailurePromptToServer(prompt);
    }

    /// <summary>
    /// Sends the character's response to text-to-speech.
    /// </summary>
    /// <param name="response">The response text.</param>
    private void SendCharacterResponseToTTS(string response)
    {
        if (_viewDebugLogs) Debug.Log($"Character : {_characterName} Response: " + response);
        _textToSpeech.PlayResponse(response);
    }
}
