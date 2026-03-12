using UnityEngine;


/// ScriptableObject chứa toàn bộ AudioClip của enemy, chia theo sự kiện tương tác.
/// Tạo asset: Right-click > Create > ScriptableObjects > EnemySoundLibrary
/// Mỗi array cho phép kéo nhiều clip để random pick khi phát → tạo đa dạng âm thanh.
[CreateAssetMenu(fileName = "NewEnemySoundLibrary", menuName = "ScriptableObjects/EnemySoundLibrary")]
public class EnemySoundLibrary : ScriptableObject
{
    // ===========================================
    //  MELEE
    // ===========================================
    [Header("Melee Attack")]
    [Tooltip("Âm thanh khi quái cận chiến chém / đấm player")]
    public AudioClip[] meleeAttackClips;

    // ===========================================
    //  RANGED
    // ===========================================
    [Header("Ranged Shoot")]
    [Tooltip("Âm thanh khi quái bắn đạn")]
    public AudioClip[] rangedShootClips;

    // ===========================================
    //  EXPLOSION
    // ===========================================
    [Header("Explosion")]
    [Tooltip("Âm thanh khi quái tự nổ")]
    public AudioClip[] explosionClips;

    // ===========================================
    //  ALARM / SCREAM
    // ===========================================
    [Header("Alarm Scream")]
    [Tooltip("Âm thanh khi quái báo động hú gọi đồng đội")]
    public AudioClip[] alarmScreamClips;

    // ===========================================
    //  SHIELD
    // ===========================================
    [Header("Shield Block")]
    [Tooltip("Âm thanh khi quái khiên đỡ đòn thành công")]
    public AudioClip[] shieldBlockClips;

    // ===========================================
    //  HURT / DIE
    // ===========================================
    [Header("Hurt")]
    [Tooltip("Âm thanh khi quái bị đánh trúng (chung cho tất cả loại quái)")]
    public AudioClip[] hurtClips;

    [Header("Die")]
    [Tooltip("Âm thanh khi quái chết")]
    public AudioClip[] dieClips;

    // ===========================================
    //  ALERT / DETECTION
    // ===========================================
    [Header("Alert")]
    [Tooltip("Âm thanh khi quái phát hiện player lần đầu")]
    public AudioClip[] alertClips;

    // ===========================================
    //  FOOTSTEP
    // ===========================================
    [Header("Footstep")]
    [Tooltip("Âm thanh bước chân khi quái di chuyển")]
    public AudioClip[] footstepClips;

    // ===========================================
    //  HELPER
    // ===========================================


    /// Lấy ngẫu nhiên 1 clip từ array. Trả về null nếu array rỗng.
    public AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
