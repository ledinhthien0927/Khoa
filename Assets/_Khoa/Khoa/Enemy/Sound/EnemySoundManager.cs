using UnityEngine;
using System.Collections.Generic;


// Singleton quản lý toàn bộ âm thanh của enemy khi tương tác với player.
// Gọi từ bất kỳ đâu: EnemySoundManager.Instance.PlayMeleeAttack(position);
//
// SETUP:
// 1. Tạo EnemySoundLibrary scriptable
// 2. Kéo AudioClip vào các field trong Library
// 3. Tạo GameObject trống trong scene, gắn script này, kéo Library vào field "soundLibrary"
public class EnemySoundManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════
    public static EnemySoundManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════
    //  SETTINGS
    // ═══════════════════════════════════════════════════════
    [Header("Sound Library")]
    [Tooltip("Kéo EnemySoundLibrary ScriptableObject asset vào đây")]
    [SerializeField] private EnemySoundLibrary soundLibrary;

    [Header("Pool Settings")]
    [Tooltip("Số lượng AudioSource tối đa được phát cùng lúc")]
    [SerializeField] private int maxConcurrentSounds = 8;

    [Header("Volume")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng chung cho toàn bộ enemy sound (0 = mute, 1 = max)")]
    public float masterVolume = 1f;

    // ═══════════════════════════════════════════════════════
    //  POOL
    // ═══════════════════════════════════════════════════════
    private List<AudioSource> audioSourcePool;
    private Transform poolParent;

    // ═══════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        // Tạo parent object để gom nhóm AudioSource cho gọn Hierarchy
        poolParent = new GameObject("[EnemySoundPool]").transform;
        poolParent.SetParent(transform);

        audioSourcePool = new List<AudioSource>(maxConcurrentSounds);

        for (int i = 0; i < maxConcurrentSounds; i++)
        {
            GameObject obj = new GameObject($"EnemyAudioSource_{i}");
            obj.transform.SetParent(poolParent);

            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;  // 3D sound
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 30f;

            audioSourcePool.Add(source);
        }
    }


    /// Lấy 1 AudioSource rảnh từ pool. Trả về null nếu tất cả đều đang bận.
    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (!audioSourcePool[i].isPlaying)
                return audioSourcePool[i];
        }

        // Tất cả đều bận → ghi log cảnh báo (không crash)
        Debug.LogWarning("[EnemySoundManager] Hết AudioSource trong pool! Tăng maxConcurrentSounds nếu cần.");
        return null;
    }

    // ===========================================================
    //  CORE — PHÁT ÂM THANH (PRIVATE)
    // ===========================================================


    // Phát 1 clip random từ array tại vị trí cho trước.
    private void PlayRandomFromArray(AudioClip[] clips, Vector3 position, float volume)
    {
        if (soundLibrary == null)
        {
            Debug.LogWarning("[EnemySoundManager] SoundLibrary chưa được gán!");
            return;
        }

        AudioClip clip = soundLibrary.GetRandomClip(clips);
        if (clip == null) return;

        PlayClipAtPosition(clip, position, volume);
    }

    // ===========================================================
    //  PUBLIC API — GỌI TỪ BẤT KỲ ĐÂU
    // ===========================================================

    #region ──── MELEE ────


    /// Phát âm thanh tấn công cận chiến tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayMeleeAttack(transform.position);

    public void PlayMeleeAttack(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.meleeAttackClips, position, volume);
    }

    #endregion

    #region ──── RANGED ────


    /// Phát âm thanh bắn đạn tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayRangedShoot(firePoint.position);
    public void PlayRangedShoot(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.rangedShootClips, position, volume);
    }

    #endregion

    #region ──── EXPLOSION ────

 
    /// Phát âm thanh nổ tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayExplosion(transform.position);
    public void PlayExplosion(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.explosionClips, position, volume);
    }

    #endregion

    #region ──── ALARM / SCREAM ────

  
    /// Phát âm thanh hú báo động tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayAlarmScream(transform.position);

    public void PlayAlarmScream(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.alarmScreamClips, position, volume);
    }

    #endregion

    #region ──── SHIELD ────

 
    /// Phát âm thanh đỡ khiên tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayShieldBlock(transform.position);

    public void PlayShieldBlock(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.shieldBlockClips, position, volume);
    }

    #endregion

    #region ──── HURT / DIE ────

 
    /// Phát âm thanh bị đánh tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayHurt(transform.position);

    public void PlayHurt(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.hurtClips, position, volume);
    }


    /// Phát âm thanh chết tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayDie(transform.position);

    public void PlayDie(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.dieClips, position, volume);
    }

    #endregion

    #region ──── ALERT ────

    /// Phát âm thanh phát hiện player tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayAlert(transform.position);
 
    public void PlayAlert(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.alertClips, position, volume);
    }

    #endregion

    #region ──── FOOTSTEP ────


    /// Phát âm thanh bước chân tại vị trí.
    /// Ví dụ: EnemySoundManager.Instance.PlayFootstep(transform.position);

    public void PlayFootstep(Vector3 position, float volume = 1f)
    {
        if (soundLibrary == null) return;
        PlayRandomFromArray(soundLibrary.footstepClips, position, volume);
    }

    #endregion

    #region ──── GENERIC ────


    /// Phát bất kỳ AudioClip nào tại vị trí. Dùng khi muốn phát clip custom 
    /// không nằm trong Library.
    /// Ví dụ: EnemySoundManager.Instance.PlayClipAtPosition(myClip, pos, 0.8f);
    public void PlayClipAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume * masterVolume;
        source.Play();
    }

    #endregion

    // ===========================================================
    //  UTILITY
    // ===========================================================


    /// Dừng toàn bộ âm thanh enemy đang phát.
    /// Ví dụ: EnemySoundManager.Instance.StopAll();
    public void StopAll()
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            audioSourcePool[i].Stop();
        }
    }

    /// Kiểm tra xem SoundLibrary đã được gán chưa (dùng để debug).

    public bool IsReady()
    {
        return soundLibrary != null;
    }
}
