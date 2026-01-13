using UnityEngine;

// --------------------------------------------------------------------------
// PHẦN 1: CÁC KHÁI NIỆM CHUNG
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
    Physical,   // Vật lý thường
    Heavy,      // Đòn nặng (Hất tung)
    Stun,       // Gây choáng
    Magic,      // Phép thuật
    EarthUp,    // Địa chấn (Skill E)
    UltimateR   // Chiêu cuối (Skill R) - [MỚI]
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
    public float duration;          // Thời gian hiệu ứng (Stun/Hất tung bao lâu?)
}

// --------------------------------------------------------------------------
// PHẦN 2: INTERFACES
// --------------------------------------------------------------------------

public interface IDamageable
{
    HitResult TakeDamage(DamageInfo info);
}