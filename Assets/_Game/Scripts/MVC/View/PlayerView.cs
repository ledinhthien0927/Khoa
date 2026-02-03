using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Camera mainCamera;

    // [ĐÃ SỬA] Chỉ giữ lại 1 bộ biến visual duy nhất.
    [Header("Weapon Visuals")]
    [SerializeField] private GameObject swordObject;       
    [SerializeField] private GameObject bowObject;         
    [SerializeField] private GameObject arrowVisualObject; 

    [Header("UI Components")]
    [SerializeField] private GameObject mainHUDCanvas; 
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private GameObject crosshairUI; 
    [SerializeField] private TextMeshProUGUI arrowCountText; 
    [SerializeField] private TextMeshProUGUI hpText;      
    [SerializeField] private TextMeshProUGUI staminaText; 

    private GameObject _currentMinigameInstance; 

    // --- 1. VISUALIZATION ---
    public void UpdateWeaponVisuals(bool hasSword, bool hasBow)
    {
        if (swordObject) swordObject.SetActive(hasSword);
        if (bowObject) bowObject.SetActive(hasBow);
        if (arrowVisualObject) arrowVisualObject.SetActive(false);

        // Cập nhật Animator
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
            if (swordObject) swordObject.SetActive(false);
            if (bowObject) bowObject.SetActive(false);
            if (arrowVisualObject) arrowVisualObject.SetActive(false);
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
                }
                else
                {
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

    // --- 3. ANIMATION TRIGGERS (ĐÃ KHÔI PHỤC ĐẦY ĐỦ) ---
    
    // [MỚI] Intro & Quest
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
            // Reset các trigger tấn công để tránh lỗi kẹt animation
            animator.ResetTrigger("Attack_1");
            animator.ResetTrigger("Attack_2");
            animator.ResetTrigger("Attack_3");
            animator.SetTrigger("Repair"); 
        } 
    }

    // [MỚI] Boat System
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

    // [KHÔI PHỤC] Combat System (QUAN TRỌNG)
    public void TriggerAttack(int step) 
    { 
        if (animator) 
        {
            // Reset các trigger cũ để combo mượt hơn
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

    public void TriggerDash() 
    { 
        if (animator) 
        { 
            animator.ResetTrigger("Dash"); 
            animator.SetTrigger("Dash"); 
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

    // [KHÔI PHỤC] Movement Blend Tree
    public void UpdateMovementAnimation(float speed, float localX, float localZ, bool isAiming, bool isBowMode)
    {
        if (!animator) return;
        animator.SetBool("IsBowMode", isBowMode);
        animator.SetBool("IsAiming", isAiming);
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", localX, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", localZ, 0.1f, Time.deltaTime);
    }
}