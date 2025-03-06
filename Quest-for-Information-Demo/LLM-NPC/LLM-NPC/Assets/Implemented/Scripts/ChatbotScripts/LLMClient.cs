using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


/// <summary>
/// Acts as a client to the LLM Server.
/// Manages communication with the server by sending prompts and receiving responses.
/// Stores the memory for the current LLM.
/// </summary>
public class LLMClient : MonoBehaviour
{
    [SerializeField]
    private bool _viewDebugLogs = false;

    private NPCManager _npcManager;

    private List<Message> _conversationHistory = new List<Message>();


    public void SetNPCManager (NPCManager nNPCMananger)
    {
        _npcManager = nNPCMananger;
    }

    /// <summary>
    /// Adds the system prompt to the conversation history.
    /// </summary>
    public void InitializeSystemPrompt()
    {
        var sysPrompt = CreateSystemPrompt();
        AddMessageToHistory("system", sysPrompt, _conversationHistory);
    }

    /// <summary>
    /// Adds the current quest system prompt to the conversation history.
    /// </summary>
    public void InitializeQuestSystemPrompt()
    {
        var sysPrompt = CreateQuestSystemPrompt();
        AddMessageToHistory("system", sysPrompt, _conversationHistory);
    }

    /// <summary>
    /// Adds the user prompt to the conversation history, packages the history in a json and sends it to the server.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    public void SendLLMPromptToServer(string prompt)
    {
        AddMessageToHistory("user", prompt, _conversationHistory);

        var llmName = _npcManager.GetCharacterName();
        var quest = GameManager.Instance.QuestNumber;
        var questCompletion = Constants.GetPlayerQuestFinisher(llmName, quest);
        ConversationWrapper wrapper = new ConversationWrapper { History = _conversationHistory, ChatbotName = llmName, Quest = quest, PlayerQuestCompletion = questCompletion};
        string jsonData = JsonUtility.ToJson(wrapper);

        StartCoroutine(SendChatPostRequest(jsonData, HandleResponseWithMemory));
    }

    /// <summary>
    /// This function is called when something failed while executing speech to text - nicely handles generation of error messages.
    /// Typically the player was silent, or there was a timeout - the player spoke for too long.
    /// Sends the created prompt to chatbot with NO prior conversation history. Doesn't expect the result to be remembered in conversation either.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    public void SendSTTFailurePromptToServer(string prompt)
    {
        var failureConversationHistory = new List<Message>();
        var sysPrompt = CreateSystemPrompt();

        AddMessageToHistory("system", sysPrompt, failureConversationHistory);
        AddMessageToHistory("user", prompt, failureConversationHistory);

        ConversationWrapper wrapper = new ConversationWrapper { History = failureConversationHistory};
        string jsonData = JsonUtility.ToJson(wrapper);

        StartCoroutine(SendChatPostRequest(jsonData, HandleResponseWithoutMemory));
    }

    /// <summary>
    /// Recreates the entire history of the NPC.
    /// Used when the chatbot ran out of tokens and needs to crop the history. 
    /// Also used preventively between quests.
    /// This function cleans the history, regenrates the system prompt and asks the LLM to summarize the conversation till now.
    /// Afterwards it adds this conversation summary as a regular prompt while ignoring the response.
    /// </summary>
    public void StartMemoryRefresh()
    {
        var prompt = "Please summarize the entire conversation in the following message. You can break character and you don't have to make the message short.";
        AddMessageToHistory("user", prompt, _conversationHistory);

        ConversationWrapper wrapper = new ConversationWrapper { History = _conversationHistory };
        string jsonData = JsonUtility.ToJson(wrapper);

        StartCoroutine(SendChatPostRequest(jsonData, HandleSummarizationResponse   ));
    }

    private void AddMessageToHistory(string role, string content, List<Message> convHistory)
    {
        convHistory.Add(new Message { role = role, content = content });
    }

    /// <summary>
    /// Coroutine to send a POST request with the conversation history to the server.
    /// The response from the server will be handled in the HandleJsonResponse function
    /// </summary>
    /// <param name="jsonData">JSON data containing the conversation history.</param>
    /// /// <param name="responseHandler">Delegate to specify which response handler to call once we get the response from the server.</param>
    /// <returns>Enumerator for coroutine handling. Ignore for now</returns>
    private IEnumerator SendChatPostRequest(string jsonData, Action<string> responseHandler)
    {
        if (_viewDebugLogs) Debug.Log($"NPC {_npcManager.GetCharacterName()} sending prompt {jsonData}.");
        // Create a UnityWebRequest to send a POST request
        using (UnityWebRequest webRequest = new UnityWebRequest(Constants.GetChatServerUrl(), "POST"))
        {
            // Set the request body (JSON data) and headers
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                responseHandler.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                var jsonResponse = JsonUtility.FromJson<ErrorResponse>(webRequest.downloadHandler.text);
                if (jsonResponse != null)
                    Debug.LogError($"Error when pinging LLM server for character {_npcManager.GetCharacterName()}. Error: {webRequest.error}. Result: {webRequest.result}. Message: {jsonResponse.Error}");
                else
                    Debug.LogError($"Error when pinging LLM server for character {_npcManager.GetCharacterName()}. Error: {webRequest.error}. Result: {webRequest.result}");

                _npcManager.ReceiveLLMResponse("An error occured when generating NPC response. Please check, whether the server is operational.");
            }
        }
    }


    private string DecodeUnicodeCharacters(string input)
    {
        return System.Text.RegularExpressions.Regex.Unescape(input);
    }

    /// <summary>
    /// Handles the JSON response received from the server, adds it into the conversation history and sends it to the manager.
    /// Handles possible quest completion and tokenizer issues.
    /// </summary>
    /// <param name="jsonResponse">JSON response string from the server.</param>
    private void HandleResponseWithMemory(string jsonResponse)
    {
        NPCResponse responseObj = JsonUtility.FromJson<NPCResponse>(jsonResponse);

        var chatbotResponse = responseObj.Response;
        chatbotResponse = DecodeUnicodeCharacters(chatbotResponse);
        if (_viewDebugLogs) Debug.Log($"Raw JSON Response for character {_npcManager.GetCharacterName()}: " + jsonResponse);

        AddMessageToHistory("assistant", chatbotResponse, _conversationHistory);

        var questCompletion = responseObj.QuestCompletion;
        if (questCompletion)
        {
            GameManager.Instance.NextQuest();
        }

        var needsMemoryRefresh = responseObj.NeedsRefresh;
        if (needsMemoryRefresh)
        {
            Debug.Log($"NPC {_npcManager.GetCharacterName()} needs a memory refresh soon.");
            _npcManager.NeedsMemoryRefresher = true;
        }

        _npcManager.ReceiveLLMResponse(chatbotResponse);
    }

    /// <summary>
    /// Handles the JSON response received from the server, and sends it to the manager without adding it to memory.
    /// Called when generating an error message. There should be no tokenizer issue here, or quest completion.
    /// </summary>
    /// <param name="jsonResponse">JSON response string from the server.</param>
    private void HandleResponseWithoutMemory(string jsonResponse)
    {
        NPCResponse responseObj = JsonUtility.FromJson<NPCResponse>(jsonResponse);
        var chatbotResponse = responseObj.Response;
        chatbotResponse = DecodeUnicodeCharacters(chatbotResponse);

        if (_viewDebugLogs) Debug.Log($"Raw JSON Response for character {_npcManager.GetCharacterName()}: " + jsonResponse);

        _npcManager.ReceiveLLMResponse(chatbotResponse);
    }

    /// <summary>
    /// Handles the JSON response received from the server when asking for a conversation summary.
    /// In this case we are rebuilding the conversation history and trying mto make it shorter. 
    /// We are not handling quests or tokenization issues
    /// </summary>
    /// <param name="jsonResponse">JSON response string from the server.</param>
    private void HandleSummarizationResponse(string jsonResponse)
    {
        NPCResponse responseObj = JsonUtility.FromJson<NPCResponse>(jsonResponse);
        var conversationSummary = responseObj.Response;
        conversationSummary = DecodeUnicodeCharacters(conversationSummary);

        // Clean up conv history
        _conversationHistory = new List<Message>();

        Debug.Log($"The LLM for NPC {_npcManager.GetCharacterName()} has successfully summarized the previous conversation: {conversationSummary}.");

        // Recreate system prompts
        InitializeSystemPrompt();
        InitializeQuestSystemPrompt();

        // Add the history as a user prompt and a default NPC answer
        AddMessageToHistory("user", "Can you summarize our previous conversation?", _conversationHistory);
        AddMessageToHistory("assistant", conversationSummary, _conversationHistory);
    }

    /// <summary>
    /// Builds the system prompt from the provided .txt files. 
    /// IMPORTANT - the structure of the files needs to remain the same. Any change needs to be changed in this function.
    /// </summary>
    /// <returns>Generated system prompt</returns>
    private string CreateSystemPrompt()
    {
        var chatbotName = _npcManager.GetCharacterName();
        var world = Constants.GetWorldFileContent();
        var personalInfo = Constants.GetChatbotPersonalFileContent(chatbotName);
        var state = Constants.GetChatbotPersonalFileContent(chatbotName);

        var systemPrompt = $@"
        Respond as if you are the following character:
        {personalInfo}

        The world:
        {world}
        
        Your state:
        {state}

        IMPORTANT: Do not mention you are an AI machine learning model or OpenAI. Give only dialogue from the first-person perspective. Do not narrate the scene or actions. Limit responses to 3 sentences.
        Do not invent any new facts, people, or names beyond what you've been given by the user. 
        You cannot move from your place, you cannot go talk to any other people, you cannot walk anywhere.
        Keep each sentence to a maximum length of 280 characters. 
        ";

        return systemPrompt.Replace("\r", string.Empty);
    }

    /// <summary>
    /// Builds the current quest system prompt from the provided .txt files.
    /// </summary>
    /// <returns>Generated system prompt for the current quest</returns>
    private string CreateQuestSystemPrompt()
    {
        int currQuest = GameManager.Instance.QuestNumber;
        var questContent = Constants.GetChatbotQuestFileContent(_npcManager.GetCharacterName(), currQuest);

        var systemPrompt = $@"
        Continue acting as the character, and follow these new important guidelines:
        {questContent}
        ";

        return systemPrompt.Replace("\r", string.Empty);
    }

    private void OnDestroy()
    {
        _conversationHistory.Clear();
        if (_viewDebugLogs) Debug.Log("Conversation history cleared on scene unload.");
    }
}