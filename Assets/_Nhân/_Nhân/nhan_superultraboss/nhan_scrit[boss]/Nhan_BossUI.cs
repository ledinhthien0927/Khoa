using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Nhan_BossUI : MonoBehaviour
{
    public static Nhan_BossUI Instance; // Singleton để BossStats dễ tìm

    [Header("UI Components")]
    public GameObject healthPanel;   // Cái khung chứa thanh máu
    public Slider hpSlider;          // Thanh trượt
    public TextMeshProUGUI nameText; // Tên Boss

    private float _maxHp;

    void Awake()
    {
        // Tạo Singleton để gọi từ bất cứ đâu: Nhan_BossUI.Instance...
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ẩn lúc đầu
        if (healthPanel) healthPanel.SetActive(false);
    }

    // Gọi hàm này khi bắt đầu đánh Boss
    public void ShowBoss(string name, float maxHp)
    {
        if (healthPanel) healthPanel.SetActive(true);
        if (nameText) nameText.text = name;
        
        _maxHp = maxHp;
        if (hpSlider) hpSlider.value = 1f;
    }

    // Gọi hàm này khi Boss mất máu
    public void UpdateHP(float currentHp)
    {
        if (hpSlider) hpSlider.value = currentHp / _maxHp;
    }

    // Gọi hàm này khi Boss chết
    public void HideBoss()
    {
        if (healthPanel) healthPanel.SetActive(false);
    }
}