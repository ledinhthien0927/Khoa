using UnityEngine;
using UnityEngine.UI; 
using TMPro;          
using System; 

public class PlayerView : MonoBehaviour
{
    [Header("Animation Components")]
    [SerializeField] private Animator animator;
    
    [Header("Main UI")]
    [SerializeField] private GameObject mainHUDCanvas; // Canvas tổng
    [SerializeField] private GameObject skillPanel;    // [MỚI] Panel chứa các skill (để ẩn khi lái tàu)

    [Header("UI Components (Cooldowns)")]
    [SerializeField] private CooldownUI dashUI;
    [SerializeField] private CooldownUI skillEUI;
    [SerializeField] private CooldownUI skillRUI;
    [SerializeField] private TextMeshProUGUI skillRStackText;

    [System.Serializable]
    public struct CooldownUI {
        public Image cooldownImage;    
        public TextMeshProUGUI cooldownText; 
    }

    public Action<int> OnAttackImpact; 
    private bool _canFreeze = false;
    private GameObject _currentMinigameInstance; 

    // --- [MỚI] HÀM ẨN/HIỆN UI SKILL ---
    public void ToggleCombatUI(bool isVisible)
    {
        if (skillPanel != null) skillPanel.SetActive(isVisible);
    }

    // --- [MỚI] CÁC ANIMATION CHO THUYỀN ---
    public void TriggerClimbUp() { if (animator) animator.SetTrigger("ClimbUp"); }
    public void TriggerClimbDown() { if (animator) animator.SetTrigger("ClimbDown"); }
    public void SetSteering(bool isSteering) { if (animator) animator.SetBool("IsSteering", isSteering); }

    // --- CÁC HÀM CŨ GIỮ NGUYÊN ---
    public void ToggleSmithingUI(bool isOpen, GameObject prefab)
    {
        if (isOpen) {
            if (_currentMinigameInstance == null && prefab != null) _currentMinigameInstance = Instantiate(prefab);
            else if (_currentMinigameInstance != null) _currentMinigameInstance.SetActive(true);
            if (mainHUDCanvas != null) mainHUDCanvas.SetActive(false);
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        } else {
            if (_currentMinigameInstance != null) _currentMinigameInstance.SetActive(false);
            if (mainHUDCanvas != null) mainHUDCanvas.SetActive(true);
            Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void UpdateMovementAnimation(float horizontal, float vertical) {
        if (animator == null) return;
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", vertical, 0.1f, Time.deltaTime);
    }

    public void TriggerAttack() { if (animator) { _canFreeze = false; animator.ResetTrigger("Attack"); animator.ResetTrigger("CounterAttack"); animator.ResetTrigger("SkillE"); animator.ResetTrigger("SkillR_Prep"); animator.ResetTrigger("SkillR_Cancel"); animator.SetTrigger("Attack"); } }
    public void TriggerCounterAttack() { if (animator) { _canFreeze = false; animator.ResetTrigger("Attack"); animator.SetTrigger("CounterAttack"); } }
    public void TriggerSkillE() { if (animator) { _canFreeze = false; animator.ResetTrigger("Attack"); animator.SetTrigger("SkillE"); } }
    public void TriggerDash() { if (animator) { _canFreeze = false; animator.SetTrigger("Dash"); } }
    public void SetBlocking(bool isBlocking) { if (animator) animator.SetBool("IsBlocking", isBlocking); }
    public void AE_TriggerImpact(int type) { OnAttackImpact?.Invoke(type); }
    public void TriggerSkillR_Prep() { if (animator) { _canFreeze = true; animator.ResetTrigger("Attack"); animator.ResetTrigger("SkillR_Cancel"); animator.SetTrigger("SkillR_Prep"); animator.speed = 1; } }
    public void TriggerSkillR_Cancel() { _canFreeze = false; if (animator) animator.SetTrigger("SkillR_Cancel"); }
    public void AE_PauseAnimator() { if (animator && _canFreeze) { animator.speed = 0f; } }
    public void ResumeAnimator() { _canFreeze = false; if (animator) animator.speed = 1f; }

    public void UpdateCooldowns(float dashTimeLeft, float dashMax, float eTimeLeft, float eMax, float rTimeLeft, float rMax, int rStacks) {
        UpdateSingleUI(dashUI, dashTimeLeft, dashMax);
        UpdateSingleUI(skillEUI, eTimeLeft, eMax);
        UpdateSkillRUI(skillRUI, rTimeLeft, rMax, rStacks);
    }
    private void UpdateSingleUI(CooldownUI ui, float timeLeft, float maxTime) {
        if (ui.cooldownImage == null) return;
        if (timeLeft > 0) { ui.cooldownImage.fillAmount = timeLeft / maxTime; if (ui.cooldownText != null) { ui.cooldownText.text = timeLeft.ToString("F1"); ui.cooldownText.gameObject.SetActive(true); } } 
        else { ui.cooldownImage.fillAmount = 0; if (ui.cooldownText != null) ui.cooldownText.gameObject.SetActive(false); }
    }
    private void UpdateSkillRUI(CooldownUI ui, float timeLeft, float maxTime, int stacks) {
        if (skillRStackText != null) skillRStackText.text = stacks.ToString();
        if (ui.cooldownImage == null) return;
        bool isFullStack = (stacks >= 5); 
        if (isFullStack) { ui.cooldownImage.fillAmount = 0; if (ui.cooldownText != null) ui.cooldownText.gameObject.SetActive(false); } 
        else { if (timeLeft > 0) { ui.cooldownImage.fillAmount = timeLeft / maxTime; if (ui.cooldownText != null) { ui.cooldownText.text = timeLeft.ToString("F1"); ui.cooldownText.gameObject.SetActive(true); } } else { ui.cooldownImage.fillAmount = 0; if (ui.cooldownText != null) ui.cooldownText.gameObject.SetActive(false); } }
    }
}