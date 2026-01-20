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
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private GameObject crosshairUI; 
    [SerializeField] private Text arrowCountText;    

    // --- CẬP NHẬT UI & VISUALS ---
    
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

    // --- [QUAN TRỌNG] HÀM FIX LỖI MAP MANAGER ---
    // Hàm này ẩn UI chiến đấu khi lên tàu
    public void ToggleCombatUI(bool isVisible)
    {
        if (hpSlider) hpSlider.gameObject.SetActive(isVisible);
        if (staminaSlider) staminaSlider.gameObject.SetActive(isVisible);
        if (crosshairUI) crosshairUI.SetActive(false); // Luôn tắt crosshair khi đi tàu
        
        // Ẩn text tên
        if (arrowCountText && arrowCountText.transform.parent) 
            arrowCountText.transform.parent.gameObject.SetActive(isVisible);

        // Ẩn vũ khí khi lái tàu cho đẹp
        if (!isVisible)
        {
            if (swordObject) swordObject.SetActive(false);
            if (bowObject) bowObject.SetActive(false);
        }
        else
        {
            // Hiện lại vũ khí mặc định (ví dụ là Kiếm) khi xuống tàu
            if (swordObject) swordObject.SetActive(true);
        }
    }

    // Các Animation leo trèo/lái tàu (MapManager gọi)
    public void TriggerClimbUp() { if (animator) animator.SetTrigger("ClimbUp"); }
    public void TriggerClimbDown() { if (animator) animator.SetTrigger("ClimbDown"); }
    public void SetSteering(bool isSteering) { if (animator) animator.SetBool("IsSteering", isSteering); }


    // --- ANIMATION CHIẾN ĐẤU ---

    public void UpdateMovementAnim(float speed, bool isStrafing)
    {
        if (!animator) return;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetBool("IsStrafing", isStrafing); 
    }

    public void TriggerAttack(int step)
    {
        if (animator) animator.SetTrigger("Attack_" + step); 
    }

    public void TriggerParry() => SetTrigger("Parry");
    public void TriggerDash() => SetTrigger("Dash");
    public void TriggerShoot() => SetTrigger("Shoot");
    public void SetAiming(bool isAiming) { if(animator) animator.SetBool("IsAiming", isAiming); }
    public void TriggerStun() => SetTrigger("Stun");

    public void ResumeAnimator() { if (animator) animator.speed = 1f; }

    private void SetTrigger(string name)
    {
        if (animator) { animator.ResetTrigger(name); animator.SetTrigger(name); }
    }
}