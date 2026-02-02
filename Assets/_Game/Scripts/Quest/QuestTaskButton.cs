using UnityEngine;

public class QuestTaskButton : MonoBehaviour
{
    private Transform targetTransform;
    private string questName;

    // Hàm này giúp QuestUIManager truyền dữ liệu mục tiêu vào nút
    public void Setup(Transform target, string name)
    {
        targetTransform = target;
        questName = name;
        // Debug để kiểm tra xem nút đã nhận được mục tiêu chưa
        if(target != null) Debug.Log($"Nút {name} đã nhận mục tiêu: {target.name}");
    }

    public void OnClickTask()
    {
        if (QuestNavigation.Instance == null)
        {
            Debug.LogError("Thiếu QuestNavigation trong Scene!");
            return;
        }

        if (targetTransform == null)
        {
            Debug.LogWarning("Nhiệm vụ này không có mục tiêu để dẫn đường!");
            return;
        }

        // Gọi lệnh dẫn đường
        QuestNavigation.Instance.ToggleNavigation(targetTransform, questName);
    }
}