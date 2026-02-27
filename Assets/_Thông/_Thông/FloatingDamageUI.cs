using UnityEngine;
using UnityEngine.UI;

public class FloatingDamageUI : MonoBehaviour
{
    public Text DamageText; // Hoặc TextMeshProUGUI nếu bạn dùng TextMeshPro
    public float MoveSpeed = 50f;
    public float FadeSpeed = 1.5f;
    
    private Color _textColor;

    public void Setup(float damageAmount)
    {
        if (DamageText == null) DamageText = GetComponent<Text>();
        
        DamageText.text = Mathf.RoundToInt(damageAmount).ToString();
        _textColor = DamageText.color;
        
        Destroy(gameObject, 2f); // Tự hủy sau 2 giây
    }

    void Update()
    {
        // Bay lên trên (trong không gian UI 2D)
        transform.position += Vector3.up * MoveSpeed * Time.deltaTime;

        // Mờ dần Alpha
        _textColor.a -= FadeSpeed * Time.deltaTime;
        DamageText.color = _textColor;
    }
}