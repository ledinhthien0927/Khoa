using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public PrinceQuestState princeState = PrinceQuestState.None;
    public VillageQuestState villageState = VillageQuestState.None;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("QuestManager Ready");
    }

    // ============ PRINCE ============

    public void SetPrince(PrinceQuestState s)
    {
        princeState = s;
        Debug.Log("Prince = " + s);
    }

    // ============ VILLAGE ============

    public void SetVillage(VillageQuestState s)
    {
        villageState = s;
        Debug.Log("Village = " + s);
    }
}
