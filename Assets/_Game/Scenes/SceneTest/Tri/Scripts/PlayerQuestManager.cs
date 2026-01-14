using UnityEngine;
using System.Collections.Generic;

public class PlayerQuestManager : MonoBehaviour
{
    public static PlayerQuestManager Instance;

    public List<Quest> quests = new List<Quest>();

    private void Awake()
    {
        Instance = this;
    }

    public bool HasQuest(string id)
    {
        return quests.Exists(q => q.questID == id);
    }

    public void AddQuest(Quest quest)
    {
        quests.Add(quest);
    }

    public Quest GetQuest(string id)
    {
        return quests.Find(q => q.questID == id);
    }
}
