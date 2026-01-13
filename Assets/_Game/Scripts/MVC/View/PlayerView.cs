using UnityEngine;
using UnityEngine.UI; 
using TMPro;          
using System; 

public class PlayerView : MonoBehaviour
{
    [Header("Animation Components")]
    [SerializeField] private Animator animator;
    
    [Header("UI Components (Cooldowns)")]
    [SerializeField] private CooldownUI dashUI;
    [SerializeField] private CooldownUI skillEUI;
    [SerializeField] private CooldownUI skillRUI;

    [Header("UI Components (Stacks)")]
    [SerializeField] private TextMeshProUGUI skillRStackText; // [MỚI] Kéo Text hiển thị số Stack vào đây

    [System.Serializable]
    public struct CooldownUI
    {
        public Image cooldownImage;    
        public TextMeshProUGUI cooldownText; 
    }

    public Action<int> OnAttackImpact; 
    private bool _canFreeze = false;

    // --- 1. MOVEMENT & LOCOMOTION ---
    public void UpdateMovementAnimation(float horizontal, float vertical)
    {
        if (animator == null) return;
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", vertical, 0.1f, Time.deltaTime);
    }

    // --- 2. BASIC COMBAT TRIGGERS ---
    public void TriggerAttack()
    {
        if (animator) 
        {
            _canFreeze = false; 
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("CounterAttack"); 
            animator.ResetTrigger("SkillE");
            animator.ResetTrigger("SkillR_Prep");
            animator.ResetTrigger("SkillR_Cancel");
            animator.SetTrigger("Attack");
        }
    }

    public void TriggerCounterAttack() { if (animator) { _canFreeze = false; animator.ResetTrigger("Attack"); animator.SetTrigger("CounterAttack"); } }
    public void TriggerSkillE() { if (animator) { _canFreeze = false; animator.ResetTrigger("Attack"); animator.SetTrigger("SkillE"); } }
    public void TriggerDash() { if (animator) { _canFreeze = false; animator.SetTrigger("Dash"); } }
    public void SetBlocking(bool isBlocking) { if (animator) animator.SetBool("IsBlocking", isBlocking); }

    // --- 3. ANIMATION EVENTS ---
    public void AE_TriggerImpact(int type) { OnAttackImpact?.Invoke(type); }

    // --- 4. SKILL R LOGIC ---
    public void TriggerSkillR_Prep() { if (animator) { _canFreeze = true; animator.ResetTrigger("Attack"); animator.ResetTrigger("SkillR_Cancel"); animator.SetTrigger("SkillR_Prep"); animator.speed = 1; } }
    public void TriggerSkillR_Cancel() { _canFreeze = false; if (animator) animator.SetTrigger("SkillR_Cancel"); }
    public void AE_PauseAnimator() { if (animator && _canFreeze) { animator.speed = 0f; } }
    public void ResumeAnimator() { _canFreeze = false; if (animator) animator.speed = 1f; }

    // --- 5. HỆ THỐNG UI COOLDOWN & STACK ---
    
    public void UpdateCooldowns(float dashTimeLeft, float dashMax, float eTimeLeft, float eMax, float rTimeLeft, float rMax, int rStacks)
    {
        UpdateSingleUI(dashUI, dashTimeLeft, dashMax);
        UpdateSingleUI(skillEUI, eTimeLeft, eMax);
        UpdateSkillRUI(skillRUI, rTimeLeft, rMax, rStacks);
    }

    private void UpdateSingleUI(CooldownUI ui, float timeLeft, float maxTime)
    {
        if (ui.cooldownImage == null) return;

        if (timeLeft > 0)
        {
            ui.cooldownImage.fillAmount = timeLeft / maxTime;
            if (ui.cooldownText != null)
            {
                ui.cooldownText.text = timeLeft.ToString("F1"); 
                ui.cooldownText.gameObject.SetActive(true);
            }
        }
        else
        {
            ui.cooldownImage.fillAmount = 0;
            if (ui.cooldownText != null) ui.cooldownText.gameObject.SetActive(false);
        }
    }

    private void UpdateSkillRUI(CooldownUI ui, float timeLeft, float maxTime, int stacks)
    {
        // [MỚI] Cập nhật Text hiển thị số Stack
        if (skillRStackText != null)
        {
            skillRStackText.text = stacks.ToString();
        }

        if (ui.cooldownImage == null) return;

        // Xử lý vòng tròn hồi chiêu (Chỉ hiện khi đang hồi stack và chưa đầy)
        // Nếu đã Max Stack -> Không cần hiện cooldown (hoặc tùy bạn muốn)
        bool isFullStack = (stacks >= 5); // Có thể thay số 5 bằng maxStacks từ Model nếu muốn truyền thêm
        
        if (isFullStack) 
        {
            ui.cooldownImage.fillAmount = 0;
            if (ui.cooldownText != null) ui.cooldownText.gameObject.SetActive(false);
        }
        else
        {
            if (timeLeft > 0)
            {
                // Đang hồi Stack tiếp theo
                ui.cooldownImage.fillAmount = timeLeft / maxTime;
                if (ui.cooldownText != null)
                {
                    ui.cooldownText.text = timeLeft.ToString("F1");
                    ui.cooldownText.gameObject.SetActive(true);
                }
            }
            else
            {
                // Vừa hồi xong 1 stack nhưng chưa dùng -> không có cooldown chạy
                ui.cooldownImage.fillAmount = 0;
                if (ui.cooldownText != null) ui.cooldownText.gameObject.SetActive(false);
            }
        }
    }
}