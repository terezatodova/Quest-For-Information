using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A singleton class that manages the switching of quests and the overall management of the game.
/// It stores all NPCs in the scene and manages the reloading of system prompt when the quest is switched. 
/// It also manages the NPC memories. Once an NPC memory is full (tokenizer is to the limit) it needs to ask the LLM for a conversation recap and store that in history.
/// This can only be done if no other NPC is using the server (for server delay purposes).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int QuestNumber { get; private set; } = 1;

    public bool AnyChatbotSpeaking = false;

    [SerializeField]
    private List<NPCManager> _npcList = new List<NPCManager>();

    [SerializeField]
    private UISpawner _finalQuestUI;

    [SerializeField]
    private GameObject _quest1UI;

    [SerializeField]
    private GameObject _quest2UI;

    [SerializeField]
    private GameObject _quest1HandUI;

    [SerializeField]
    private GameObject _quest2HandUI;



    private void Start()
    {
        _quest1UI.SetActive(true);
        _quest1HandUI.SetActive(true);
        _quest2UI.SetActive(false);
        _quest2HandUI.SetActive(false);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Makes sure it persists across scenes if needed
        }
        else
        {
            Destroy(gameObject); // Destroy any duplicate instances
        }
    }

    /// <summary>
    /// Iterates over all NPCs and checks whether there is any NPC that is nearing memory issues (it's tokenizer is about to run out)
    /// If there is, and tno other NPC is using the server, grants access to server to the first NPC that needs a memory refresh.
    /// This ensures that the serve is not overloaded and will not be too slow.
    /// The NPC also asks for a memory refresh with a good reserve, which allows it to wait in line and not be in a rush.
    /// </summary>
    void Update()
    {
        NPCManager refreshNPC = null;
        for (int i = 0; i < _npcList.Count; i++)
        {
            if (_npcList[i].InActiveConversation)
                return;
            if (_npcList[i].NeedsMemoryRefresher && refreshNPC is null)
            {
                refreshNPC = _npcList[i];
            }
        }
        if (refreshNPC is not null)
        {
            refreshNPC.StartMemoryRefresh();
            Debug.Log($"NPC {refreshNPC.GetCharacterName()} is running out of tokenizers. Starting to refresh it's memory.");
        }
    
    }
    public void NextQuest()
    {
        QuestNumber++;
        ChangeSystemPrompts();

        if (QuestNumber == 2)
        {
            _quest1UI.SetActive(false);
            _quest1HandUI.SetActive(false);
            _quest2UI.SetActive(true);
            _quest2HandUI.SetActive(true);
        }

        if (QuestNumber > Constants.TotalQuests)
            FinalQuest();
    }
    
    private void ChangeSystemPrompts()
    {
        for (int i = 0; i < _npcList.Count; i++)
        {
            _npcList[i].UpdateSystemPrompt();
        }
    }

    // Show the final quest UI to the player
    private void FinalQuest()
    {
        _finalQuestUI.SpawnUI();
        _quest1UI.SetActive(false);
        _quest2UI.SetActive(false);
        _quest1HandUI.SetActive(false);
        _quest2HandUI.SetActive(false);
    }
}
