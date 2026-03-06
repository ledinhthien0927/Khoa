using UnityEngine;
using System.Collections; 

public class ShieldMonster : MonsterController
{
    [Header("Shield Settings")]
    public float blockDuration = 2f;       
    public float blockAngle = 70f;         
    
    [Tooltip("Thời gian chờ để chạy xong hoạt ảnh hạ khiên trước khi được phép đi tiếp")]
    public float shieldLowerDelay = 0.5f; 

    private bool isBlocking = false;
    private bool isLoweringShield = false; 
    private float blockTimer = 0f;

    public override HitResult TakeDamage(DamageInfo info)
    {
        if (isDead || isReturning) return HitResult.Ignored;

        float hitAngle = Vector3.Angle(transform.forward, -info.hitDirection);
        bool isFrontalHit = hitAngle <= blockAngle;

        // 1. ĐANG THỦ HOẶC ĐANG CẤT KHIÊN MÀ BỊ ĐÁNH TỪ TRƯỚC MẶT -> Đỡ đòn thành công
        if ((isBlocking || isLoweringShield) && isFrontalHit)
        {
            blockTimer = blockDuration; 
            isBlocking = true;
            isLoweringShield = false; // Hủy ngay hành động cất khiên để tiếp tục thủ
            
            if (anim != null) 
            {
                anim.SetBool("isBlocking", true);
                anim.SetTrigger("BlockHit"); 
            }
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(info.hitDirection * (info.knockbackForce * 0.5f), ForceMode.Impulse);
            }

            return HitResult.Ignored; 
        }

        // 2. KHÔNG THỦ HOẶC BỊ ĐÁNH LÉN TỪ SAU LƯNG -> Nhận sát thương thực sự (gọi hàm lớp cha)
        HitResult result = base.TakeDamage(info);

        // [SỬA LỖI KẸT CHÂN]: Khi đã bị chém trúng người (mất máu/giật mình)
        // Bắt buộc phải reset trạng thái khiên về 0 để tránh xung đột Animation
        if (isBlocking || isLoweringShield)
        {
            isBlocking = false;
            isLoweringShield = false;
            if (anim != null) anim.SetBool("isBlocking", false);
        }

        // 3. CHỈ BẬT KHIÊN NẾU: Đánh từ đằng trước + Còn sống + ĐÃ NHÌN THẤY PLAYER (isAlerted) + KHÔNG LÙNG SỤC (!isSearching)
        if (isFrontalHit && currentHealth > 0 && isAlerted && !isSearching)
        {
            isBlocking = true;
            isLoweringShield = false;
            blockTimer = blockDuration;
            
            if (anim != null) anim.SetBool("isBlocking", true);
        }

        return result;
    }

    protected override void Update()
    {
        base.Update(); 

        if (isDead) return;

        // Chỉ đếm ngược thời gian thủ nếu KHÔNG TRONG LÚC đang lúi húi cất khiên
        if (isBlocking && !isLoweringShield)
        {
            blockTimer -= Time.deltaTime;
            
            if (blockTimer <= 0f)
            {
                // Thay vì thả ra ngay lập tức, ta chạy quá trình hạ khiên có độ trễ
                StartCoroutine(LowerShieldRoutine());
            }
        }
    }

    private IEnumerator LowerShieldRoutine()
    {
        isLoweringShield = true; // Đánh dấu là đang bắt đầu hạ khiên
        
        // 1. Tắt biến trong Animator để nhân vật bắt đầu diễn hoạt cảnh bỏ khiên xuống
        if (anim != null) anim.SetBool("isBlocking", false);
        
        // 2. Bắt nó đứng im chờ một lúc (khớp với thời gian animation hạ khiên thực tế)
        yield return new WaitForSeconds(shieldLowerDelay);

        // 3. (An toàn) Nếu trong lúc đang chờ mà bị Player chém tiếp thì cờ này đã bị tắt ở TakeDamage, ta hủy lệnh đi tiếp
        if (!isLoweringShield) yield break;

        // 4. Nếu an toàn cất xong, mới thực sự mở khóa di chuyển
        isBlocking = false;
        isLoweringShield = false;
        Debug.Log($"<color=cyan>[SHIELD DOWN]</color> {gameObject.name} đã hạ khiên XONG và bắt đầu di chuyển.");
    }

    public override void OnCombatBehavior(Transform player)
    {
        // Bị khóa chân nếu ĐANG THỦ hoặc ĐANG TRONG LÚC CẤT KHIÊN
        if (isBlocking || isLoweringShield)
        {
            StopMoving();                   
            RotateTowards(player.position); 
            return; 
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (data != null && dist <= data.attackRange)
        {
            StopMoving();
            RotateTowards(player.position);

            if (CanAttack())
            {
                if (anim != null) anim.SetTrigger("attack");
            }
        }
        else
        {
            MoveToPosition(player.position, false);
        }
    }
}