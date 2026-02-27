using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class BossHealthZone : MonoBehaviour
{
    [Header("Settings")]
    public GameObject BossObject;       
    public GameObject BossHealthBarUI;  
    
    [Header("Boss Info (Tên Boss)")]
    [Tooltip("Gõ tên Boss vào đây (VD: Kẻ Vô Danh)")]
    public string BossName = "Tên Boss";
    [Tooltip("Kéo chữ TextMeshPro hiển thị tên Boss vào đây")]
    public TextMeshProUGUI BossNameText;

    [Header("Health Sliders")]
    public Slider FrontHealthSlider;    
    public Slider BackHealthSlider;     
    public float EaseSpeed = 3f; 

    [Header("Accumulated Damage UI")]
    public TextMeshProUGUI AccumulatedDamageText; 
    public float DamageDisplayDuration = 1.0f; 

    private float _accumulatedDamage = 0f;
    private float _damageTimer = 0f;

    private Boss1 _boss1;
    private Boss2 _boss2;
    private bool _isActive = false;
    private float _previousHealth;

    void Start()
    {
        if (BossHealthBarUI != null) BossHealthBarUI.SetActive(false);
        if (AccumulatedDamageText != null) AccumulatedDamageText.gameObject.SetActive(false); 

        // Gán tên Boss ngay từ đầu
        if (BossNameText != null) 
        {
            BossNameText.text = BossName;
        }

        if (BossObject != null)
        {
            _boss1 = BossObject.GetComponent<Boss1>();
            _boss2 = BossObject.GetComponent<Boss2>();
            
            if (_boss1 != null) _previousHealth = _boss1.MaxHealth;
            if (_boss2 != null) _previousHealth = _boss2.MaxHealth;
        }
    }

    void Update()
    {
        if (BossObject == null)
        {
            if (BossHealthBarUI.activeSelf) BossHealthBarUI.SetActive(false);
            return;
        }

        bool isDead = false;
        float currentHp = 0;
        float maxHp = 1;

        if (_boss1 != null) { currentHp = _boss1.CurrentHealth; maxHp = _boss1.MaxHealth; if (_boss1.CurrentState == Boss1.State.Dead) isDead = true; }
        else if (_boss2 != null) { currentHp = _boss2.CurrentHealth; maxHp = _boss2.MaxHealth; if (_boss2.CurrentState == Boss2.State.Dead) isDead = true; }

        if (isDead || currentHp <= 0)
        {
            if (BossHealthBarUI.activeSelf) BossHealthBarUI.SetActive(false);
            return;
        }

        if (_isActive && BossHealthBarUI != null)
        {
            if (!BossHealthBarUI.activeSelf) BossHealthBarUI.SetActive(true);
            
            if (FrontHealthSlider != null) FrontHealthSlider.maxValue = maxHp;
            if (BackHealthSlider != null) BackHealthSlider.maxValue = maxHp;

            if (FrontHealthSlider != null) FrontHealthSlider.value = currentHp;

            if (BackHealthSlider != null)
            {
                if (BackHealthSlider.value < currentHp) BackHealthSlider.value = currentHp;
                BackHealthSlider.value = Mathf.Lerp(BackHealthSlider.value, currentHp, Time.deltaTime * EaseSpeed);
            }

            if (_previousHealth > currentHp)
            {
                float damageTaken = _previousHealth - currentHp;
                
                _accumulatedDamage += damageTaken;
                _damageTimer = DamageDisplayDuration; 
                
                if (AccumulatedDamageText != null)
                {
                    AccumulatedDamageText.gameObject.SetActive(true); 
                    AccumulatedDamageText.text = Mathf.RoundToInt(_accumulatedDamage).ToString();
                }
            }
            
            if (_damageTimer > 0)
            {
                _damageTimer -= Time.deltaTime;
                if (_damageTimer <= 0)
                {
                    _accumulatedDamage = 0f; 
                    if (AccumulatedDamageText != null)
                    {
                        AccumulatedDamageText.gameObject.SetActive(false); 
                    }
                }
            }

            _previousHealth = currentHp;
        }
    }

    private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) _isActive = true; }
    
    private void OnTriggerExit(Collider other) 
    { 
        if (other.CompareTag("Player")) 
        { 
            _isActive = false; 
            if (BossHealthBarUI != null) BossHealthBarUI.SetActive(false); 
            
            _accumulatedDamage = 0f;
            _damageTimer = 0f;
            if (AccumulatedDamageText != null) AccumulatedDamageText.gameObject.SetActive(false);
        } 
    }
}