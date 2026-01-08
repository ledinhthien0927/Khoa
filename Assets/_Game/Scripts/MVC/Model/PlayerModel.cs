using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    [Header("Movement Config - Di chuyển")]
    public float moveSpeed = 6f;
    public float accelerationTime = 0.25f;
    public float decelerationTime = 0.4f;
    public float rotationSpeed = 720f;

    [Header("Jump Smash Config - Nhảy Dậm")]
    public float dashWindupTime = 0.1f; 
    public float dashAirTime = 0.5f;
    public float dashRecoveryTime = 0.3f;
    public float dashMoveSpeed = 12f;
    public AnimationCurve jumpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    public float jumpHeightMultiplier = 2.0f;

    [Header("Combat Config - Chiến đấu")]
    public float dashCooldown = 1.5f;
    public float blockCooldown = 1.5f;     
    public float maxBlockDuration = 2.0f;  
    public float counterWindow = 1.0f;     
    public float counterDuration = 1.2f;   
    public float[] attackDurations = { 0.6f, 0.7f, 1.1f };
    public float comboResetTime = 3.0f;   
    public float minComboDelay = 0.1f;    

    [Header("Damage Settings - Sát thương")]
    public LayerMask enemyLayer;      
    public float attackRange = 2.0f;  
    public float damageAmount = 10f;  
    public float[] knockbackForces = { 20f, 40f, 60f, 80f }; 

    [Header("VFX Settings - Hiệu ứng hình ảnh")] // --- [MỚI] ---
    public GameObject vfxCombo3;      // Kéo Prefab hiệu ứng Đòn 3 vào đây
    public GameObject vfxJumpSmash;   // Kéo Prefab hiệu ứng Đập đất vào đây
    public GameObject vfxCounter;     // Kéo Prefab hiệu ứng Phản kích vào đây

    [Header("Runtime State - Trạng thái")]
    public Vector3 currentVelocity;
    public Vector3 smoothDampVelocity;
    public bool isAttacking;
    public bool isBlocking;
    public bool isDashing;
    public bool isCounterReady;
    public int currentComboStep = 0;
    public float lastAttackTime;
    public float lastDashTime;
    public float nextBlockTime; 
    public float blockStartTime;
}