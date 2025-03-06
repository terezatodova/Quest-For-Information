using System.Linq;
using UnityEngine;

/// <summary>
/// Contains constant values used throughout the application.
/// Modify the serverUrl to set yor personal URL.
/// </summary>
public class Constants : MonoBehaviour
{
    /// <summary>
    /// Key for server IP
    /// </summary>
    public const string ServerIPKey = "ServerIP";

    /// <summary>
    /// Server port
    /// </summary>
    public const string Port = "5000";

    /// <summary>
    /// The endpoint for chat functionality.
    /// </summary>
    public const string ChatEndpoint = "/chat";

    /// <summary>
    /// The endpoint for testing server status.
    /// </summary>
    public const string TestEndpoint = "/status";

    /// <summary>
    /// The path to the filder that stores tge NPC Data.
    /// Important - If you want to change the file structure modify the GetFileLocation functions.
    /// </summary>
    public const string CharacterDocsLocation = "NPC-Documents";

    /// <summary>
    /// The number of total quests in the game.
    /// </summary>
    public const int TotalQuests = 2;

    /// <summary>
    /// Positive outline color - when the chatbot can be interacted with.
    /// </summary>
    public static readonly Color PositiveOutlineColor = new Color(0, 0.5f, 0, 0.5f);

    /// <summary>
    /// NEgative outline color - when the chatbot cannot be interacted with.
    /// </summary>
    public static readonly Color NegativeOutlineColor = new Color(0.5f, 0, 0, 0.5f);

    /// <summary>
    /// Enum capturing the possible Speech To Text results
    /// </summary>
    public enum TTSResult
    {
        Success,
        NoData,
        Timeout
    }


    public static string GetChatServerUrl()
    {
        var serverUrl = ServerIPString.Instance.ServerIP;
        return $"http://{serverUrl}:{Port}{ChatEndpoint}";
    }

    public static string GetTestServerUrl()
    {
        var serverUrl = ServerIPString.Instance.ServerIP;
        return $"http://{serverUrl}:{Port}{TestEndpoint}";
    }

    public static string GetWorldFileContent()
    {
        var filename = CharacterDocsLocation + "/World";
        TextAsset textAsset = Resources.Load<TextAsset>(filename);
        return textAsset.text.Replace("\r", string.Empty);
    }

    public static string GetChatbotPersonalFileContent(string chatbotName)
    {
        var filename = CharacterDocsLocation + $"/{chatbotName}/Personal";
        TextAsset textAsset = Resources.Load<TextAsset>(filename);
        return textAsset.text.Replace("\r", string.Empty);
    }

    public static string GetChatbotStateFileContent(string chatbotName)
    {
        var filename = CharacterDocsLocation + $"/{chatbotName}/State";
        TextAsset textAsset = Resources.Load<TextAsset>(filename);
        return textAsset.text.Replace("\r", string.Empty);
    }

    /// <summary>
    /// Important - all quest files must follow the proper format - first line is *NPC* or *Player*, based on who do we want to finish the quest
    /// From the second line furthewr the system prompt starts.
    /// This function returns the quest system prompt.
    /// </summary>
    public static string GetChatbotQuestFileContent(string chatbotName, int quest)
    {
        var filename = CharacterDocsLocation + $"/{chatbotName}/Quests/Quest{quest}";
        TextAsset textAsset = Resources.Load<TextAsset>(filename);

        if (textAsset == null)
            return string.Empty;

        string[] lines = textAsset.text.Replace("\r", string.Empty).Split('\n');
        if (lines.Length < 2)
            return string.Empty;

        return string.Join("\n", lines.Skip(1));
    }

    /// <summary>
    /// Important - all quest files must follow the proper format - first line is *NPC* or *Player*, based on who do we want to finish the quest
    /// Returns true if the player should finish the quest. False otherwise ( NPC should finish the quest or nothin is specified).
    /// </summary>
    public static bool GetPlayerQuestFinisher(string chatbotName, int quest)
    {
        var filename = CharacterDocsLocation + $"/{chatbotName}/Quests/Quest{quest}";
        TextAsset textAsset = Resources.Load<TextAsset>(filename);

        if (textAsset == null)
            return false;

        string firstLine = textAsset.text.Replace("\r", string.Empty).Split('\n')[0];

        int startIndex = firstLine.IndexOf('*') + 1;
        int endIndex = firstLine.LastIndexOf('*');

        if (startIndex > 0 && endIndex > startIndex)
        {
            var questFinisher = firstLine.Substring(startIndex, endIndex - startIndex);
            return (questFinisher.ToLower() == "player");
        }

        return false;
    }
}
