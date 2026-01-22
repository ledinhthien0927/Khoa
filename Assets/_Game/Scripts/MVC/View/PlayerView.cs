using UnityEngine;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Camera mainCamera;

    [Header("Weapon Models")]
    [SerializeField] private GameObject swordObject; 
    [SerializeField] private GameObject bowObject;   

    [Header("UI Components")]
    // [QUAN TRỌNG] Thêm biến này để chứa toàn bộ HUD (Máu, Stamina, Tên...)
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

    // --- LOGIC ẨN/HIỆN UI (Đã cập nhật để dùng mainHUDCanvas) ---

    public void ToggleCombatUI(bool isVisible)
    {
        // Cách 1: Tắt toàn bộ Canvas cha (Gọn nhất, tắt hết mọi thứ bên trong)
        if (mainHUDCanvas != null) 
        {
            mainHUDCanvas.SetActive(isVisible);
        }
        // Cách 2: Nếu không gán mainHUDCanvas thì mới tắt lẻ tẻ (Dự phòng)
        else 
        {
            if (hpSlider) hpSlider.gameObject.SetActive(isVisible);
            if (staminaSlider) staminaSlider.gameObject.SetActive(isVisible);
            if (arrowCountText && arrowCountText.transform.parent) 
                arrowCountText.transform.parent.gameObject.SetActive(isVisible);
        }

        // Luôn đảm bảo tắt Crosshair khi ẩn UI
        if (!isVisible && crosshairUI) crosshairUI.SetActive(false);

        // Ẩn vũ khí khi không chiến đấu
        if (!isVisible)
        {
            if (swordObject) swordObject.SetActive(false);
            if (bowObject) bowObject.SetActive(false);
        }
        else
        {
            // Hiện lại kiếm mặc định khi UI bật lại
            if (swordObject) swordObject.SetActive(true);
        }
    }

    public void ToggleSmithingUI(bool isOpen, GameObject prefab)
    {
        if (isOpen) {
            // Mở Minigame Rèn
            if (_currentMinigameInstance == null && prefab != null) 
                _currentMinigameInstance = Instantiate(prefab);
            else if (_currentMinigameInstance != null) 
                _currentMinigameInstance.SetActive(true);
            
            // [QUAN TRỌNG] Tắt UI Nhân vật đi
            ToggleCombatUI(false); 
            
            Cursor.visible = true; 
            Cursor.lockState = CursorLockMode.None;
        } else {
            // Đóng Minigame
            if (_currentMinigameInstance != null) 
                _currentMinigameInstance.SetActive(false);
            
            // [QUAN TRỌNG] Bật lại UI Nhân vật
            ToggleCombatUI(true); 
            
            Cursor.visible = false; 
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // --- ANIMATION ---
    public void TriggerClimbUp() { if (animator) animator.SetTrigger("ClimbUp"); }
    public void TriggerClimbDown() { if (animator) animator.SetTrigger("ClimbDown"); }
    public void SetSteering(bool isSteering) { if (animator) animator.SetBool("IsSteering", isSteering); }
    public void UpdateMovementAnim(float speed, bool isStrafing)
    {
        if (!animator) return;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetBool("IsStrafing", isStrafing); 
    }
    public void TriggerAttack(int step) { if (animator) animator.SetTrigger("Attack_" + step); }
    public void TriggerParry() => SetTrigger("Parry");
    public void TriggerDash() => SetTrigger("Dash");
    public void TriggerShoot() => SetTrigger("Shoot");
    public void SetAiming(bool isAiming) { if(animator) animator.SetBool("IsAiming", isAiming); }
    public void TriggerStun() => SetTrigger("Stun");
    public void ResumeAnimator() { if (animator) animator.speed = 1f; }
    private void SetTrigger(string name) { if (animator) { animator.ResetTrigger(name); animator.SetTrigger(name); } }
}