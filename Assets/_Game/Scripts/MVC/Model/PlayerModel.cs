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
    public float blockCooldown = 1.5f;     
    public float maxBlockDuration = 2.0f;  
    public float counterWindow = 1.0f;     
    public float counterDuration = 1.2f;   
    public float[] attackDurations = { 0.6f, 0.7f, 1.1f };
    public float comboResetTime = 3.0f;   
    public float minComboDelay = 0.1f;    

    [Header("Skill E - Địa Chấn")]
    public float skillECooldown = 5.0f;    
    public float skillEDuration = 1.5f;    
    public float skillERange = 8.0f;       
    [Range(0, 180)]
    public float skillEAngle = 45.0f;      
    public float skillEKnockupForce = 15f; 
    public float skillEStunTime = 2.0f;    
    
    // [MỚI] Góc xoay sửa lỗi VFX (Nếu VFX bay sang phải thì nhập -90 hoặc 90 vào đây)
    public Vector3 skillEVfxRotation = new Vector3(0, -90, 0); 

    [Header("Damage Settings")]
    public LayerMask enemyLayer;      
    public float attackRange = 2.0f;  
    public float damageAmount = 10f;  
    public float[] knockbackForces = { 20f, 40f, 60f, 80f }; 

    [Header("VFX Settings")]
    public GameObject vfxCombo3;
    public GameObject vfxJumpSmash;
    public GameObject vfxCounter;
    public GameObject vfxSkillE; 

    [Header("Runtime State")]
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
    public float lastSkillETime; 
}