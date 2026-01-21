using UnityEngine;
using System.Collections.Generic;

public class PlayerQuestManager : MonoBehaviour
{
    public static PlayerQuestManager Instance;

    Dictionary<QuestID, QuestState> quests =
        new Dictionary<QuestID, QuestState>();

    void Awake()
    {
        Instance = this;
    }

    public void AcceptQuest(QuestID id)
    {
        quests[id] = QuestState.InProgress;
    }

    public void CompleteQuest(QuestID id)
    {
        quests[id] = QuestState.Completed;
    }

    public QuestState GetState(QuestID id)
    {
        if (!quests.ContainsKey(id))
            return QuestState.NotStarted;

        return quests[id];
    }
}
