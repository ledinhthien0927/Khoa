using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quest States")]
    public PrinceQuestState princeState = PrinceQuestState.None; // Trạng thái ban đầu
    public VillageQuestState villageState = VillageQuestState.None;

    void Awake()
    {
        // Khởi tạo Singleton để truy cập từ mọi nơi
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("QuestManager đã sẵn sàng.");
    }

    // Cập nhật trạng thái nhiệm vụ Hoàng tử
    public void SetPrince(PrinceQuestState s)
{
    princeState = s;
    Debug.Log("Prince = " + s);
    
    // CẬP NHẬT UI TỰ ĐỘNG
    if (QuestUIManager.Instance != null)
        QuestUIManager.Instance.UpdateQuestUI();
}

    // Cập nhật trạng thái nhiệm vụ Làng
    public void SetVillage(VillageQuestState s)
    {
        villageState = s;
        Debug.Log("Nhiệm vụ Làng chuyển sang: " + s);
    }
}