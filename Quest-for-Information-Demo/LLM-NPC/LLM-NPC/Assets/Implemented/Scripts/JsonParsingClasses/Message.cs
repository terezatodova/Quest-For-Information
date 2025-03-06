using System;


/// <summary>
/// Represents a single message exchanged in a conversation.
/// This class is used to send a single message to the LLM server.
/// </summary>
[Serializable]
public class Message
{
    public string role;

    public string content;
}