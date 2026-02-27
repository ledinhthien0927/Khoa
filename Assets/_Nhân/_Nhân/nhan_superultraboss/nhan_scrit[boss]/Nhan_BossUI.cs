using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Nhan_BossUI : MonoBehaviour
{
    public static Nhan_BossUI Instance; // Singleton

    [Header("UI Components")]
    public GameObject healthPanel;   // Panel chứa thanh máu
    public Slider hpSlider;          // Thanh Slider
    public TextMeshProUGUI nameText; // Tên Boss

    private float _maxHp;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ẩn lúc đầu
        if (healthPanel) healthPanel.SetActive(false);
    }

    public void ShowBoss(string name, float maxHp)
    {
        if (healthPanel) healthPanel.SetActive(true);
        if (nameText) nameText.text = name;
        _maxHp = maxHp;
        if (hpSlider) hpSlider.value = 1f;
    }

    public void UpdateHP(float currentHp)
    {
        if (hpSlider && _maxHp > 0) hpSlider.value = currentHp / _maxHp;
    }

    public void HideBoss()
    {
        if (healthPanel) healthPanel.SetActive(false);
    }
}