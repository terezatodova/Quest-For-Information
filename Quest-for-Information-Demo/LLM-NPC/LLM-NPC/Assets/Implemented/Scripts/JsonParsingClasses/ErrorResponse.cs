using System;


/// <summary>
/// Represents a error message returned from the server if the chat request failed.
/// </summary>
[Serializable]
public class ErrorResponse
{
    public string Error;
}