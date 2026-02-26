using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Camera mainCamera;

    [Header("Weapon Visuals")]
    [SerializeField] private GameObject swordObject;       
    [SerializeField] private GameObject bowObject;         
    [SerializeField] private GameObject arrowVisualObject; 
    [SerializeField] private GameObject hammerObject; // Thêm cái búa

    [Header("UI Components")]
    [SerializeField] private GameObject mainHUDCanvas; 
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private GameObject crosshairUI; 
    [SerializeField] private TextMeshProUGUI arrowCountText; 
    [SerializeField] private TextMeshProUGUI hpText;      
    [SerializeField] private TextMeshProUGUI staminaText; 

    private GameObject _currentMinigameInstance; 

    void Start()
    {
        // Tự động quét và đồng bộ vũ khí ngay khi vừa bật game hoặc vừa Load lại Scene
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            // Bật/tắt thanh kiếm theo đúng dữ liệu trong PlayerModel
            UpdateWeaponVisuals(pc.model.hasSword, pc.model.hasBow);
            
            // Nếu chưa có vũ khí nào, ép Animator về dáng đứng tay không
            if (!pc.model.hasSword && !pc.model.hasBow)
            {
                RestoreVisualState(0);
            }
        }
    }
    public void EquipHammer(bool isEquipped)
    {
        if (hammerObject) hammerObject.SetActive(isEquipped);
        
        // Nếu cầm búa, tạm thời cất vũ khí chính đi
        if (isEquipped)
        {
            if (swordObject) swordObject.SetActive(false);
            if (bowObject) bowObject.SetActive(false);
            if (arrowVisualObject) arrowVisualObject.SetActive(false);
        }
        else
        {
            // Trả lại vũ khí cũ dựa trên logic hiện tại của View
            RestoreVisualState(GetCurrentVisualState());
        }
    }

    // --- 1. VISUALIZATION ---
    public void UpdateWeaponVisuals(bool hasSword, bool hasBow)
    {
        if (swordObject) swordObject.SetActive(hasSword);
        if (bowObject) bowObject.SetActive(hasBow);
        if (arrowVisualObject) arrowVisualObject.SetActive(false);

        int type = hasSword ? 1 : 0;
        if (animator) animator.SetInteger("WeaponType", type);
    }

    public void SwitchWeaponVisuals(WeaponType type)
    {
        if (swordObject) swordObject.SetActive(type == WeaponType.Sword);
        if (bowObject) bowObject.SetActive(type == WeaponType.Bow);
        if (arrowVisualObject) arrowVisualObject.SetActive(false);
        
        if (animator) 
        {
            animator.SetBool("IsBowMode", type == WeaponType.Bow);
            int typeInt = (type == WeaponType.Sword) ? 1 : 2;
            if (type == WeaponType.Sword && !swordObject.activeSelf) typeInt = 0; 
            animator.SetInteger("WeaponType", typeInt);
        }
        ToggleCrosshair(false);
    }

    // --- 2. UI & CAMERA ---
    public void ToggleCombatUI(bool isVisible)
    {
        if (mainHUDCanvas != null) mainHUDCanvas.SetActive(isVisible);
        if (!isVisible && crosshairUI) crosshairUI.SetActive(false);

        if (!isVisible) 
        {
            // Ẩn mô hình vũ khí
            if (swordObject) swordObject.SetActive(false);
            if (bowObject) bowObject.SetActive(false);
            if (arrowVisualObject) arrowVisualObject.SetActive(false);
            
            // --- [THÊM MỚI] ÉP ANIMATOR VỀ DÁNG TAY KHÔNG (UNARMED) ---
            if (animator) 
            {
                animator.SetInteger("WeaponType", 0);
                animator.SetBool("IsBowMode", false);
            }
            // ----------------------------------------------------------
        }
        else
        {
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                if (!pc.model.hasSword && !pc.model.hasBow)
                {
                    if (swordObject) swordObject.SetActive(false);
                    if (bowObject) bowObject.SetActive(false);
                    
                    // Nếu không có vũ khí nào, giữ dáng tay không
                    if (animator) animator.SetInteger("WeaponType", 0);
                }
                else
                {
                    // Hàm này đã tự động cập nhật lại Animator sang 1 (Kiếm) hoặc 2 (Cung)
                    SwitchWeaponVisuals(pc.model.currentWeapon);
                }
            }
        }
    }

    public void UpdateStatsUI(float hp, float maxHp, float stamina, float maxStamina, int arrows)
    {
        if (hpSlider) hpSlider.value = hp / maxHp;
        if (staminaSlider) staminaSlider.value = stamina / maxStamina;
        if (hpText) hpText.text = $"{Mathf.CeilToInt(hp)} / {maxHp}";
        if (staminaText) staminaText.text = $"{Mathf.CeilToInt(stamina)} / {maxStamina}";
        if (arrowCountText) arrowCountText.text = arrows.ToString();
    }

    public void ToggleSmithingUI(bool isOpen, GameObject prefab)
    {
        if (isOpen) {
            if (_currentMinigameInstance == null && prefab != null) 
                _currentMinigameInstance = Instantiate(prefab);
            else if (_currentMinigameInstance != null) 
                _currentMinigameInstance.SetActive(true);
            
            ToggleCombatUI(false); 
            Cursor.visible = true; 
            Cursor.lockState = CursorLockMode.None;
        } else {
            if (_currentMinigameInstance != null) 
                _currentMinigameInstance.SetActive(false);
            
            ToggleCombatUI(true); 
            Cursor.visible = false; 
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    public void ToggleCrosshair(bool show) { if (crosshairUI) crosshairUI.SetActive(show); }

    public void SetCameraZoom(bool isZooming, float targetFOV, float normalFOV)
    {
        if (mainCamera == null) return;
        float fov = isZooming ? targetFOV : normalFOV;
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, fov, Time.deltaTime * 10f);
    }

    public void SetArrowVisual(bool isActive)
    {
        if (arrowVisualObject && arrowVisualObject.activeSelf != isActive)
            arrowVisualObject.SetActive(isActive);
    }

    // --- 3. ANIMATION TRIGGERS ---
    
    public void TriggerWakeUp()
    {
        if (animator)
        {
            animator.SetTrigger("WakeUp");
            animator.SetInteger("WeaponType", 0); 
        }
    }

    public void TriggerRepair() 
    { 
        if (animator) 
        {
            animator.ResetTrigger("Attack_1");
            animator.ResetTrigger("Attack_2");
            animator.ResetTrigger("Attack_3");
            animator.SetTrigger("Repair"); 
        } 
    }

    public void SetSwimming(bool isSwimming)
    {
        if (animator)
        {
            animator.SetBool("IsSwimming", isSwimming);
            if (isSwimming)
            {
                if (swordObject) swordObject.SetActive(false);
                if (bowObject) bowObject.SetActive(false);
                if (arrowVisualObject) arrowVisualObject.SetActive(false);
            }
        }
    }
    
    public void TriggerClimbUp() { if (animator) animator.SetTrigger("ClimbUp"); }
    public void TriggerClimbDown() { if (animator) animator.SetTrigger("ClimbDown"); }
    public void SetSteering(bool isSteering) { if (animator) animator.SetBool("IsSteering", isSteering); }

    public void TriggerAttack(int step) 
    { 
        if (animator) 
        {
            animator.ResetTrigger("Attack_1");
            animator.ResetTrigger("Attack_2");
            animator.ResetTrigger("Attack_3");
            animator.SetTrigger("Attack_" + step); 
        } 
    }

    public void TriggerParry() 
    { 
        if (animator) 
        { 
            animator.ResetTrigger("Parry"); 
            animator.SetTrigger("Parry"); 
        }
    }

    // [CẬP NHẬT] - Hủy mọi đòn đánh để ép Animator chạy Dash ngay lập tức
    public void TriggerDash() 
    { 
        if (animator) 
        { 
            // Xóa các lệnh tấn công đang xếp hàng
            animator.ResetTrigger("Attack_1");
            animator.ResetTrigger("Attack_2");
            animator.ResetTrigger("Attack_3");
            
            // Đảm bảo không có trigger Dash nào bị kẹt lại
            animator.ResetTrigger("Dash"); 
            
            // [ĐÃ SỬA] CHỈ dùng Play để ép chạy Dash ngay lập tức, KHÔNG dùng SetTrigger nữa
            animator.Play("Dash", 0, 0f); 
        }
    }

    public void TriggerShoot() 
    { 
        if (animator) 
        { 
            animator.ResetTrigger("Shoot"); 
            animator.SetTrigger("Shoot"); 
        }
    }

    public void SetAiming(bool isAiming) 
    { 
        if(animator) animator.SetBool("IsAiming", isAiming); 
    }

    public void TriggerStun() 
    { 
        if (animator) 
        { 
            animator.ResetTrigger("Stun"); 
            animator.SetTrigger("Stun"); 
        }
    }

    public void UpdateMovementAnimation(float speed, float localX, float localZ, bool isAiming, bool isBowMode)
    {
        if (!animator) return;
        animator.SetBool("IsBowMode", isBowMode);
        animator.SetBool("IsAiming", isAiming);
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", localX, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", localZ, 0.1f, Time.deltaTime);
    }
    
    public bool IsHoldingWeapon()
    {
        return (swordObject != null && swordObject.activeSelf) || (bowObject != null && bowObject.activeSelf);
    }

    public int GetCurrentVisualState()
    {
        if (animator != null) 
        {
            return animator.GetInteger("WeaponType");
        }
        return 0; 
    }

    public void RestoreVisualState(int stateToRestore)
    {
        // 1. Trả lại hiển thị vũ khí cũ
        if (swordObject) swordObject.SetActive(stateToRestore == 1);
        if (bowObject) bowObject.SetActive(stateToRestore == 2);
        if (arrowVisualObject) arrowVisualObject.SetActive(false);

        if (animator)
        {
            // 2. Trả lại thông số cho Base Layer
            animator.SetInteger("WeaponType", stateToRestore);
            animator.SetBool("IsBowMode", stateToRestore == 2);
            
            if (stateToRestore == 1) 
                animator.Play("Sword Locomotion", 0, 0f); 
            else if (stateToRestore == 2) 
                animator.Play("Bow Locomotion", 0, 0f);   
            else 
                animator.Play("Unarmed Locomotion", 0, 0f); 

            // 3. [QUAN TRỌNG] Tắt hoạt động của Action Layer (Layer 1)
            // Ép Layer 1 chuyển ngay lập tức về state "Empty" để nhân vật buông thõng tay xuống
            animator.Play("Empty", 1, 0f); 
        }
        
        ToggleCrosshair(stateToRestore == 2);
    }
}