using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu bạn dùng TextMeshPro

public class PlayerView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Camera mainCamera;

    [Header("Weapon Models")]
    [SerializeField] private GameObject swordObject; 
    [SerializeField] private GameObject bowObject;   

    [Header("UI Components")]
    [SerializeField] private GameObject mainHUDCanvas; 
    
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private GameObject crosshairUI; 
    [SerializeField] private Text arrowCountText;    

    private GameObject _currentMinigameInstance; 

    // --- CẬP NHẬT UI ---
    
    public void UpdateStatsUI(float hp, float maxHp, float stamina, float maxStamina, int arrows)
    {
        if (hpSlider) hpSlider.value = hp / maxHp;
        if (staminaSlider) staminaSlider.value = stamina / maxStamina;
        if (arrowCountText) arrowCountText.text = arrows.ToString();
    }

    public void SwitchWeaponVisuals(WeaponType type)
    {
        if (swordObject) swordObject.SetActive(type == WeaponType.Sword);
        if (bowObject) bowObject.SetActive(type == WeaponType.Bow);
        
        if (animator) animator.SetBool("IsBowMode", type == WeaponType.Bow);
        
        ToggleCrosshair(false);
    }

    public void ToggleCrosshair(bool show)
    {
        if (crosshairUI) crosshairUI.SetActive(show);
    }

    public void SetCameraZoom(bool isZooming, float targetFOV, float normalFOV)
    {
        if (mainCamera == null) return;
        float fov = isZooming ? targetFOV : normalFOV;
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, fov, Time.deltaTime * 10f);
    }

    // --- LOGIC ẨN/HIỆN UI ---

    public void ToggleCombatUI(bool isVisible)
    {
        if (mainHUDCanvas != null) mainHUDCanvas.SetActive(isVisible);
        else 
        {
            if (hpSlider) hpSlider.gameObject.SetActive(isVisible);
            if (staminaSlider) staminaSlider.gameObject.SetActive(isVisible);
            if (arrowCountText && arrowCountText.transform.parent) 
                arrowCountText.transform.parent.gameObject.SetActive(isVisible);
        }

        if (!isVisible && crosshairUI) crosshairUI.SetActive(false);

        if (!isVisible) {
            if (swordObject) swordObject.SetActive(false);
            if (bowObject) bowObject.SetActive(false);
        } else {
            if (swordObject) swordObject.SetActive(true);
        }
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

    // --- ANIMATION (ĐÃ SỬA TÊN VÀ THAM SỐ) ---
    
    public void TriggerClimbUp() { if (animator) animator.SetTrigger("ClimbUp"); }
    public void TriggerClimbDown() { if (animator) animator.SetTrigger("ClimbDown"); }
    public void SetSteering(bool isSteering) { if (animator) animator.SetBool("IsSteering", isSteering); }

    // [FIX LỖI TẠI ĐÂY] Đổi tên thành UpdateMovementAnimation và thêm 5 tham số
    public void UpdateMovementAnimation(float speed, float localX, float localZ, bool isAiming, bool isBowMode)
    {
        if (!animator) return;
        animator.SetBool("IsBowMode", isBowMode);
        animator.SetBool("IsAiming", isAiming);
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", localX, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", localZ, 0.1f, Time.deltaTime);
    }
    
    public void TriggerAttack(int step) { if (animator) animator.SetTrigger("Attack_" + step); }
    public void TriggerParry() { if (animator) { animator.ResetTrigger("Parry"); animator.SetTrigger("Parry"); }}
    public void TriggerDash() { if (animator) { animator.ResetTrigger("Dash"); animator.SetTrigger("Dash"); }}
    public void TriggerShoot() { if (animator) { animator.ResetTrigger("Shoot"); animator.SetTrigger("Shoot"); }}
    public void SetAiming(bool isAiming) { if(animator) animator.SetBool("IsAiming", isAiming); }
    public void TriggerStun() { if (animator) { animator.ResetTrigger("Stun"); animator.SetTrigger("Stun"); }}
    public void ResumeAnimator() { if (animator) animator.speed = 1f; }
}