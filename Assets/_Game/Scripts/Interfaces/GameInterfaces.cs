using UnityEngine;

// --------------------------------------------------------------------------
// PHẦN 1: CÁC KHÁI NIỆM CHUNG (DATA TYPES)
// --------------------------------------------------------------------------

/// <summary>
/// Kết quả trả về sau khi một đòn đánh được tung ra.
/// </summary>
public enum HitResult
{
    Hit,        // Trúng đòn bình thường (Mất máu)
    Critical,   // Trúng điểm yếu hoặc đòn kết liễu (Mất nhiều máu)
    Blocked,    // Đỡ được (Mất ít máu/Stamina + Hiệu ứng Block)
    Parried,    // Phản đòn thành công (Không mất máu + Đối phương bị choáng)
    Miss,       // Đánh trượt hoặc Đối phương đang lướt (I-Frame)
    Ignored     // Bất tử (Do cốt truyện hoặc đang trong cắt cảnh)
}

/// <summary>
/// Loại sát thương (Để xác định hiệu ứng VFX hoặc âm thanh phù hợp)
/// </summary>
public enum DamageType
{
    Physical,   // Vật lý thông thường
    Heavy,      // Đòn nặng (Gây đẩy lùi/Stun cao - Dùng cho Búa đòn 3)
    Stun,       // Đòn gây choáng (Dùng cho Shoulder Bash)
    Magic       // Phép thuật (nếu Boss có dùng)
}

/// <summary>
/// Gói tin chứa TOÀN BỘ thông tin về đòn đánh.
/// Dùng struct này để code gọn gàng hơn, sau này muốn thêm thông tin cũng dễ.
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    public float amount;            // Lượng sát thương gốc
    public GameObject attacker;     // Ai là người đánh? (Player hay Boss)
    public Vector3 hitPoint;        // Vị trí va chạm (để tạo hiệu ứng máu/lửa)
    public Vector3 hitDirection;    // Hướng đòn đánh (để tính đẩy lùi)
    public float knockbackForce;    // Lực đẩy lùi (Lớn thì bay xa)
    public DamageType type;         // Loại sát thương
}

// --------------------------------------------------------------------------
// PHẦN 2: CÁC GIAO DIỆN (INTERFACES)
// --------------------------------------------------------------------------

/// <summary>
/// Bất kỳ ai muốn nhận sát thương (Player, Boss, Thùng gỗ...) đều phải dùng Interface này.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Hàm nhận sát thương.
    /// </summary>
    /// <param name="info">Gói tin chứa thông tin sát thương</param>
    /// <returns>Kết quả va chạm (Hit/Block/Miss...)</returns>
    HitResult TakeDamage(DamageInfo info);
}

/// <summary>
/// Interface để kiểm tra trạng thái (Boss dùng để soi Player và ngược lại)
/// </summary>
public interface ICombatState
{
    bool IsInvincible(); // Đang bất tử? (Đang Dash/Roll)
    bool IsBlocking();   // Đang đỡ đòn?
    bool IsStunned();    // Đang bị choáng?
}