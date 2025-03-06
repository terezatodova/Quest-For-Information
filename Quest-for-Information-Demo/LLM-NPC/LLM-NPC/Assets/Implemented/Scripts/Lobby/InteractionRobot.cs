using Meta.WitAi;
using Meta.WitAi.Configuration;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using Meta.WitAi.TTS.Utilities;
using Oculus.Voice;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;


/// <summary>
/// Handles speech-to-text and text-to-speech functionality for the robot in the lobby
/// </summary>
public class InteractionRobot : MonoBehaviour
{
    [SerializeField]
    private AppVoiceExperience _voiceExperience;

    [SerializeField]
    private TTSSpeaker _speaker;

    [SerializeField]
    private GameObject _processingText;

    [SerializeField]
    private GameObject _userPromptText;

    [SerializeField]
    private Outline _outline;

    private bool _npcInProgress = false;

    private bool _speechTranslated = false;

    private bool _isSpeaking = false;

    private bool _isHovered = false;

    /// <summary>
    /// Bumps up the speed of the speaker to 150% for a more natural feel.
    /// </summary>
    void Start()
    {
        _speaker.customWitVoiceSettings.speed = 200;
        _processingText.SetActive(false);
        _userPromptText.SetActive(false);
    }

    public void GreyOutline()
    {
        _isHovered = false;
        StopRecording();
        _outline.OutlineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }

    public void GreenOutline()
    {
        _isHovered = true;
        _outline.OutlineColor = Constants.PositiveOutlineColor;
    }


    void Update()
    {   
        if (_npcInProgress)
        {
            _processingText.SetActive(true);
            _outline.OutlineColor = Constants.NegativeOutlineColor;
        }
        else
        {
            _processingText.SetActive(false);
            _userPromptText.SetActive(false);

            if (_isHovered)
                _outline.OutlineColor = Constants.PositiveOutlineColor;
            else
                _outline.OutlineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        if (_isSpeaking && !_speaker.IsActive)
        {
            _isSpeaking = false;
            _npcInProgress = false;
        }
    }

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
    /// Begins recording audio input for speech recognition.
    /// </summary>
    public void StartRecording()
    {
        if (_voiceExperience != null && !_voiceExperience.Active)
        {
            _npcInProgress = true;
            _speechTranslated = false;
            _voiceExperience.Activate();
        }
    }

    /// <summary>
    /// Stops recording audio input and processes the recorded audio for speech recognition.
    /// </summary>
    public void StopRecording()
    {
        if (_voiceExperience != null && _voiceExperience.Active)
        {
            _voiceExperience.Deactivate();
        }
    }

    /// <summary>
    /// A listener that handles partial transcription - to update the user prompt text on NPC manager.
    /// </summary>
    /// <param name="partialResponse">The transcribed text.</param>
    private void OnPartialTranscriptionListener(string partialResponse)
    {
        if (partialResponse == "" || partialResponse == _userPromptText.GetNamedChild("Canvas").GetComponentInChildren<TextMeshProUGUI>().text)
            return;

        if (!_userPromptText.activeSelf)
            _userPromptText.SetActive(true);

        _userPromptText.GetNamedChild("Canvas").GetComponentInChildren<TextMeshProUGUI>().SetText("User prompt: " + partialResponse);
    }

    /// <summary>
    /// A listener.Handles Wit.ai response (i.e., when transcription is successfully returned)
    /// </summary>
    /// <param name="response">The transcribed response.</param>
    private void OnWitResponseListener(string response)
    {
        if (!string.IsNullOrEmpty(response))
        {
            OnTranslatedSpeech(response);
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
        PlayResponse(text);
    }

    /// <summary>
    /// A listener. Called when an error occurs during speech recognition
    /// </summary>
    /// <param name="error">The error code.</param>
    /// <param name="message">The error message.</param>
    private void OnError(string error, string message)
    {
        _speechTranslated = true;

        var response = "A speech translation error occured " + error;
        PlayResponse(response);
    }

    /// <summary>
    /// A listener. Called when there is a timeout (no speech detected or cut-off)
    /// </summary>
    private void OnTimeout()
    {
        _speechTranslated = true;

        var response = "You have hit a speech timeout.";
        PlayResponse(response);
    }

    /// <summary>
    /// A listener. Called when there is a timeout (no speech detected or cut-off)
    /// </summary>
    private void OnComplete(VoiceServiceRequest req)
    {
        if (!_speechTranslated)
        {
            PlayResponse("No speech was detected");
        }
    }

    /// <summary>
    /// Plays a spoken response based on the provided text.
    /// If the response exceeds 280 characters, it is split into smaller chunks.
    /// This is due to the fact that Wit.Ai can only handle up to 280 characters at once
    /// </summary>
    /// <param name="response">LLM rsponse to be converted to speech.</param>

    public void PlayResponse(string response)
    {
        if (!_userPromptText.activeSelf)
            _userPromptText.SetActive(true);
        _userPromptText.GetNamedChild("Canvas").GetComponentInChildren<TextMeshProUGUI>().SetText("User prompt: " + response);

        _isSpeaking = true;
        const int maxChunkSize = 280;

        List<string> chunks = SplitResponse(response, maxChunkSize);

        foreach (string chunk in chunks)
        {
            _speaker.SpeakQueued(chunk);
        }
    }

    /// <summary>
    /// Splits a long response into smaller chunks that fit within the maximum character limit.
    /// Note - the responses are ALWAYS split by a dot symbol. If there is a sentence longer that 280 characters, it will be
    /// split in the middle which might cause problematic behavior.
    /// </summary>
    /// <param name="response">The full response textt.</param>
    /// <param name="maxChunkSize">Max size for each chunk of text.</param>
    /// <returns>A list of text chunks that are within the specified maximum size.</returns>

    private List<string> SplitResponse(string response, int maxChunkSize)
    {
        List<string> chunks = new List<string>();
        string[] sentences = response.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);
        string currentChunk = "";

        foreach (string sentence in sentences)
        {
            string trimmedSentence = sentence.Trim();
            if (trimmedSentence.Length > maxChunkSize)
            {
                int startIndex = 0;

                while (startIndex < trimmedSentence.Length)
                {
                    int chunkSize = Mathf.Min(maxChunkSize, trimmedSentence.Length - startIndex);
                    string chunk = trimmedSentence.Substring(startIndex, chunkSize);
                    chunks.Add(chunk);
                    startIndex += chunkSize;
                }
            }
            else
            {
                if (currentChunk.Length + trimmedSentence.Length + 1 > maxChunkSize)
                {
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.TrimEnd() + ".");
                        currentChunk = "";
                    }
                }
                currentChunk += trimmedSentence + ". ";
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.TrimEnd() + ".");
        }

        return chunks;
    }
}