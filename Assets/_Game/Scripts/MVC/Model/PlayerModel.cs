using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    [Header("Movement Config")]
    public float moveSpeed = 6f;
    public float accelerationTime = 0.25f;
    public float decelerationTime = 0.4f;
    public float rotationSpeed = 720f;

    [Header("Jump Smash Config")]
    public float dashWindupTime = 0.1f; 
    public float dashAirTime = 0.5f;
    public float dashRecoveryTime = 0.3f;
    public float dashMoveSpeed = 12f;
    public AnimationCurve jumpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    public float jumpHeightMultiplier = 2.0f;

    [Header("Combat Config")]
    public float dashCooldown = 1.5f;
    
    [Tooltip("Thời gian Animation: Đòn 1, Đòn 2, Đòn 3")]
    public float[] attackDurations = { 0.6f, 0.7f, 1.1f };
    
    // [ĐIỀU CHỈNH 1] Tăng thời gian giữ combo lên 3s để bấm thong thả vẫn ăn combo
    public float comboResetTime = 3.0f;   
    
    // [ĐIỀU CHỈNH 2] Giảm delay xuống 0.1s để bấm nhanh vẫn nhận lệnh (nhạy hơn)
    public float minComboDelay = 0.1f;   

    [Header("Game Feel")]
    public float hitStopDuration = 0.1f;

    [Header("Runtime State")]
    public Vector3 currentVelocity;
    public Vector3 smoothDampVelocity;
    
    public bool isAttacking;
    public bool isBlocking;
    public bool isDashing;
    
    public int currentComboStep = 0;
    public float lastAttackTime;
    public float lastDashTime;
}