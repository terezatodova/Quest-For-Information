using System;


/// <summary>
/// Represents a response of the chatbot received from the LLM server.
/// </summary>
[Serializable]
public class NPCResponse
{
    public string Response;

    public bool QuestCompletion;

    public bool NeedsRefresh;
}
