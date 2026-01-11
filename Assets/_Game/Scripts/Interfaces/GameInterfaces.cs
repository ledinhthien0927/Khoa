using UnityEngine;

// --------------------------------------------------------------------------
// PHẦN 1: CÁC KHÁI NIỆM CHUNG (DATA TYPES)
// --------------------------------------------------------------------------

public enum HitResult
{
    Hit,        // Trúng đòn
    Critical,   // Trúng điểm yếu
    Blocked,    // Đỡ được
    Parried,    // Phản đòn
    Miss,       // Trượt
    Ignored     // Bất tử
}

public enum DamageType
{
    Physical,   // Vật lý thông thường
    Heavy,      // Đòn nặng (Hất tung)
    Stun,       // Đòn gây choáng
    Magic,      // Phép thuật
    EarthUp     // [MỚI] Địa chấn (Dùng cho Skill E)
}

[System.Serializable]
public struct DamageInfo
{
    public float amount;            // Lượng sát thương
    public GameObject attacker;     // Người đánh
    public Vector3 hitPoint;        // Điểm va chạm
    public Vector3 hitDirection;    // Hướng đẩy
    public float knockbackForce;    // Lực đẩy
    public DamageType type;         // Loại sát thương
    
    // [MỚI] Biến này gây ra lỗi nếu thiếu
    public float duration;          // Thời gian hiệu ứng (Stun/Hất tung bao lâu?)
}

// --------------------------------------------------------------------------
// PHẦN 2: CÁC GIAO DIỆN (INTERFACES)
// --------------------------------------------------------------------------

public interface IDamageable
{
    HitResult TakeDamage(DamageInfo info);
}