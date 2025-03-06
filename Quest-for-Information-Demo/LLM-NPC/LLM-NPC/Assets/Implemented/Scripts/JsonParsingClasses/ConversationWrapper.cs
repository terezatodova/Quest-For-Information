using System;
using System.Collections.Generic;


/// <summary>
/// A wrapper class for holding a list of messages in a conversation.
/// This class is used to send the chatbot history to the LLM server.
/// </summary>
[Serializable]
public class ConversationWrapper
{
    // History without the latest prompt from the user
    public List<Message> History;

    public string ChatbotName;

    public int Quest;

    // True if we are looking for quest completion from player
    public bool PlayerQuestCompletion;
}