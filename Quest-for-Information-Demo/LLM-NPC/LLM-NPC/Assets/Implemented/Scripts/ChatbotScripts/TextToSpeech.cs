using Meta.WitAi.TTS.Utilities;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Handles text-to-speecj functionality.
/// Uses the VoiceSDK from Meta.ai (specifically the TTSSpeaker) to generate the speech.
/// </summary>
public class TextToSpeech : MonoBehaviour
{
    [SerializeField]
    private bool _viewDebugLogs = false;

    [SerializeField]
    private TTSSpeaker _speaker;

    private NPCManager _npcManager;

    private bool _isSpeaking = false;


    /// <summary>
    /// Bumps up the speed of the speaker to 150% for a more natural feel.
    /// </summary>
    void Start()
    {
        _speaker.customWitVoiceSettings.speed = 200;
    }

    void Update()
    {
        if (_isSpeaking && !_speaker.IsActive)
        {
            _isSpeaking = false;
            _npcManager.EndNPCProcessing();
        }
    }

    public void SetNPCManager(NPCManager nNPCMananger)
    {
        _npcManager = nNPCMananger;
    }


    /// <summary>
    /// Called from the chatbot manager.
    /// Plays a spoken response based on the provided text.
    /// If the response exceeds 280 characters, it is split into smaller chunks.
    /// This is due to the fact that Wit.Ai can only handle up to 280 characters at once
    /// </summary>
    /// <param name="response">LLM rsponse to be converted to speech.</param>

    public void PlayResponse(string response)
    {
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
                Debug.LogError($"Chatbot {_npcManager.GetCharacterName()} included a sentence with more than 280 characters!. Splitting. Might cause wrong behavior. Sentence: {sentence}");

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
                if (_viewDebugLogs) Debug.Log($"Chatbot {_npcManager.GetCharacterName()} responded with a longer message. Splitting into chunks.");
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
