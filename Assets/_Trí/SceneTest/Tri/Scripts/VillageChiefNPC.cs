using UnityEngine;
using System.Collections.Generic; // Bổ sung thư viện này để dùng List

public class VillageChiefNPC : NPCController
{
    [Header("Dialogues")]
    public DialogueLine[] intro;
    public DialogueLine[] quest;
    public DialogueLine[] warning;
    public DialogueLine[] cleared;
    public DialogueLine[] reward;
    public DialogueLine[] done;

    [Header("Quest Requirements")]
    [Tooltip("Kéo thả các GameObject enemy vào danh sách này.")]
    public List<GameObject> enemiesToDefeat; 

    [Header("Quest Rewards")]
    public GameObject rewardChestPrefab; 
    [Tooltip("Vị trí rương sẽ xuất hiện. Nếu bỏ trống sẽ spawn cạnh NPC.")]
    public Transform chestSpawnPoint; 
    private bool hasSpawnedChest = false;

    public override void Interact()
    {
        if (DialogueUI.Instance == null) return;
        if (QuestManager.Instance == null) return;

        var qm = QuestManager.Instance;

        // Tự động kiểm tra nếu người chơi đang làm nhiệm vụ và đã giết hết quái
        if (qm.villageState == VillageQuestState.Working)
        {
            if (AreAllEnemiesDefeated())
            {
                qm.SetVillage(VillageQuestState.Cleared);
            }
        }

        switch (qm.villageState)
        {
            // ============ FIRST ============
            case VillageQuestState.None:
                DialogueUI.Instance.Show(
                    intro,
                    this,
                    () =>
                    {
                        qm.SetVillage(VillageQuestState.Accepted);
                    });
                break;

            // ============ ACCEPT ============
            case VillageQuestState.Accepted:
                DialogueUI.Instance.Show(
                    quest,
                    this,
                    null,
                    AcceptQuest);
                break;

            // ============ WORKING ============
            case VillageQuestState.Working:
                DialogueUI.Instance.Show(warning, this);
                break;

            // ============ CLEARED ============
            case VillageQuestState.Cleared:
                DialogueUI.Instance.Show(
                    cleared,
                    this,
                    () =>
                    {
                        qm.SetVillage(VillageQuestState.Rewarded);
                    });
                break;

            // ============ REWARD ============
            case VillageQuestState.Rewarded:
                DialogueUI.Instance.Show(
                    reward,
                    this,
                    () =>
                    {
                        SpawnRewardChest(); // Gọi hàm spawn rương khi thoại xong
                        qm.SetVillage(VillageQuestState.Done);
                    });
                break;

            // ============ DONE ============
            case VillageQuestState.Done:
                DialogueUI.Instance.Show(done, this);
                break;

            default:
                Debug.LogWarning("VillageChief: Unknown state " + qm.villageState);
                break;
        }
    }

    void AcceptQuest()
    {
        QuestManager.Instance.SetVillage(VillageQuestState.Working);
    }

    // Hàm kiểm tra xem toàn bộ quái trong danh sách đã bị tiêu diệt chưa
    bool AreAllEnemiesDefeated()
    {
        if (enemiesToDefeat == null || enemiesToDefeat.Count == 0) return true;

        foreach (GameObject enemy in enemiesToDefeat)
        {
            // Unity tự động đánh giá enemy == null nếu GameObject đó đã bị Destroy()
            if (enemy != null) 
            {
                return false; // Vẫn còn quái vật sống
            }
        }
        return true; // Tất cả đã bị tiêu diệt
    }

    // Hàm gọi sinh ra rương phần thưởng
    void SpawnRewardChest()
    {
        if (!hasSpawnedChest && rewardChestPrefab != null)
        {
            // Nếu có thiết lập chestSpawnPoint thì xuất hiện ở đó, nếu không thì xuất hiện cách NPC 2 đơn vị
            Vector3 spawnPos = chestSpawnPoint != null ? chestSpawnPoint.position : transform.position + transform.forward * 2f;
            Instantiate(rewardChestPrefab, spawnPos, Quaternion.identity);
            hasSpawnedChest = true;
        }
        else if (rewardChestPrefab == null)
        {
            Debug.LogWarning("Chưa gắn Prefab rương phần thưởng cho trưởng làng!");
        }
    }
}