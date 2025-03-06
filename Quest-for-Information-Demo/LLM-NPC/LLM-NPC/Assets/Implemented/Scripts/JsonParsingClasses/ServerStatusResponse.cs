using System;


/// <summary>
/// Represents the response received from the server regarding it's status.
/// Is used in the lobby to check whether the server is up and running.
/// </summary>
[Serializable]
public class ServerStatusResponse
{
    public bool Status;
}