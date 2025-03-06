using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using Oculus.Voice;
using UnityEngine;


/// <summary>
/// Handles speech-to-text functionality for Standalone VR setup.
/// Uses the VoiceSDK from Meta.ai (specifically the voice experience) to translate the speech.
/// Note - this can also be used for the PCVR approach.
/// </summary>
public class SpeechToText : MonoBehaviour
{
    [SerializeField]
    private bool _viewDebugLogs = false;

    [SerializeField]
    private AppVoiceExperience _voiceExperience;

    private NPCManager _npcManager;

    private bool _speechTranslated = false;

    void OnEnable()
    {
        if (_voiceExperience != null)
        {
            _voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscriptionListener);
            _voiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnWitResponseListener);
            _voiceExperience.VoiceEvents.OnError.AddListener(OnError);
            _voiceExperience.VoiceEvents.OnStoppedListeningDueToTimeout.AddListener(OnTimeout);
            _voiceExperience.VoiceEvents.OnComplete.AddListener(OnComplete);
        }
    }

    void OnDisable()
    {
        if (_voiceExperience != null)
        {
            _voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscriptionListener);
            _voiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnWitResponseListener);
            _voiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
            _voiceExperience.VoiceEvents.OnStoppedListeningDueToTimeout.RemoveListener(OnTimeout);
            _voiceExperience.VoiceEvents.OnComplete.RemoveListener(OnComplete);
        }
    }

    /// <summary>
    /// Sets the ChatbotManager instance, in order to send back the STT response.
    /// </summary>
    /// <param name="chatbotManager">The ChatbotManager instance.</param>
    public void SetNPCManager(NPCManager nNPCMananger)
    {
        _npcManager = nNPCMananger;
    }


    /// <summary>
    /// Begins recording audio input for speech recognition.
    /// </summary>
    public void StartRecording()
    {
        if (_voiceExperience != null && _voiceExperience.Active == false)
        {
            if (_viewDebugLogs) Debug.Log($"Starting voice recording for character {_npcManager.GetCharacterName()}");
            _speechTranslated = false;
            _voiceExperience.Activate();
        }
        else
        {
            Debug.LogError($"Starting voice recording failed for {_npcManager.GetCharacterName()}");
        }
    }

    /// <summary>
    /// Stops recording audio input and processes the recorded audio for speech recognition.
    /// </summary>
    public void StopRecording()
    {
        if (_voiceExperience != null && _voiceExperience.Active)
        {
            if (_viewDebugLogs) Debug.Log($"Stopping voice recording for character {_npcManager.GetCharacterName()}...");
            _voiceExperience.Deactivate();
        }
        else
        {
            Debug.LogError($"Stopping voice recording failed for {_npcManager.GetCharacterName()}");
        }
    }


    /// <summary>
    /// <returns>True if the recording is currently running. False otherwise</returns>
    /// </summary>
    public bool IsRecording()
    {
        return (_voiceExperience != null && _voiceExperience.Active);
    }

    /// <summary>
    /// A listener that handles partial transcription - to update the user prompt text on NPC manager.
    /// </summary>
    /// <param name="partialResponse">The partially transcribed text.</param>
    private void OnPartialTranscriptionListener(string partialResponse)
    {
        _npcManager.UpdatePlayerSpeechText(partialResponse);
    }

    /// <summary>
    /// A listener.Handles Wit.ai response (i.e., when transcription is successfully returned)
    /// </summary>
    /// <param name="text">The transcribed text.</param>
    private void OnWitResponseListener(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            OnTranslatedSpeech(text);
        }
        else
        {
            OnError("empty", "Received empty response from Wit.ai.");
        }
    }

    /// <summary>
    /// Calls chatbot manager with translated speech.
    /// </summary>
    /// <param name="text">The transcribed text.</param>
    private void OnTranslatedSpeech(string text)
    {
        _speechTranslated = true;
        if (_viewDebugLogs) Debug.Log($"Character {_npcManager.GetCharacterName()} Dictation result: " + text);
        _npcManager.ReceiveTranslatedSpeech(text, Constants.TTSResult.Success);
    }

    /// <summary>
    /// A listener. Called when an error occurs during speech recognition
    /// </summary>
    /// <param name="error">The error code.</param>
    /// <param name="message">The error message.</param>
    private void OnError(string error, string message)
    {
        _speechTranslated = true;
        Debug.LogWarning($"Character {_npcManager.GetCharacterName()} Speech error: " + error + " and message " + message);
        _npcManager.ReceiveTranslatedSpeech("", Constants.TTSResult.NoData);
    }

    /// <summary>
    /// A listener. Called when there is a timeout (no speech detected or cut-off)
    /// </summary>
    private void OnTimeout()
    {
        _speechTranslated = true;
        Debug.LogWarning($"Character {_npcManager.GetCharacterName()} Response - timeout");
        _npcManager.ReceiveTranslatedSpeech("", Constants.TTSResult.Timeout);
    }

    /// <summary>
    /// A listener. Called when there is a timeout (no speech detected or cut-off)
    /// </summary>
    private void OnComplete(VoiceServiceRequest req)
    {
        if (!_speechTranslated)
        {
            Debug.LogWarning($"Character {_npcManager.GetCharacterName()} Response not created");
            _npcManager.ReceiveTranslatedSpeech("", Constants.TTSResult.NoData);
        }
    }
}
