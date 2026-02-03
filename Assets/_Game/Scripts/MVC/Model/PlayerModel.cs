using UnityEngine;

// Enum trạng thái
public enum PlayerState { Idle, Moving, Dashing, Attacking, Parrying, ParryingRecovery, Aiming, Stunned, Dead, Swimming } 
public enum WeaponType1 { Sword, Bow }

[System.Serializable]
public class PlayerModel
{
    [Header("Stats & Resources")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f;    
    public float staminaRegenDelay = 1.0f;  

    [Header("Movement Config")]
    public float walkSpeed = 6f;      // Tốc độ đi bộ
    public float runSpeed = 10f;      // [KHÔI PHỤC] Tốc độ chạy (Shift)
    public float aimMoveSpeed = 2f;   // Tốc độ khi ngắm bắn
    public float rotationSpeed = 15f;       
    
    [Header("Dash Settings")]
    public float dashForce = 20f;     // [KHÔI PHỤC] Tăng lên 20 để lướt xa hơn
    public float dashDuration = 0.4f;
    public float dashCost = 20f;      // Giảm tốn stamina chút
    public float dashIFrameDuration = 0.3f; 

    [Header("Smoothing")]
    public float accelerationTime = 0.1f; // [KHÔI PHỤC] Giảm xuống để nhân vật phản hồi nhanh hơn
    public float decelerationTime = 0.1f; // Dừng lại nhanh hơn

    [Header("Swimming Config")]
    public float swimSpeed = 4f;            
    public float waterLevelY = 0f;          
    public float swimThreshold = 1.2f;      
    public float surfaceBuoyancy = 0.5f;    
    public float buoyancySpeed = 2.0f;      

    [Header("Inventory & Weapons")]
    public WeaponType currentWeapon = WeaponType.Sword;
    public int currentArrows = 5;
    public int maxArrows = 10;
    
    // [QUAN TRỌNG] Biến lưu trạng thái sở hữu vũ khí
    public bool hasSword = false; 
    public bool hasBow = false;   
    
    // Nguyên liệu
    public int woodCount = 0;
    public int metalCount = 0;   

    [Header("Sword Combat")]
    public float comboResetTime = 1.2f;
    public float parryWindow = 0.5f;        
    public float parryRecovery = 0.5f;      
    public float damageSwordBase = 15f; // [KHÔI PHỤC] Tăng damage lên chút
    public LayerMask enemyLayer;      
    public float attackRange = 2.0f;  

    [Header("Bow Combat")]
    public float arrowShootSpeed = 100f;    
    public float reloadTime = 0.8f;         
    public float damageBow = 20f;
    public float zoomFOV = 40f;             
    public float normalFOV = 60f;

    [Header("Interaction & Smithing")]
    public LayerMask interactionLayer;      
    public float interactionRange = 3.0f; // Tăng tầm tương tác lên 3m cho dễ bấm F
    public GameObject smithingMinigamePrefab; 
    public string interactionUIName = "UI_Prompt"; 
    public bool isSmithing;                   

    [Header("Visuals & Effects")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public GameObject vfxParrySparks;

    [Header("Runtime State")]
    public Vector3 currentVelocity;      
    public Vector3 smoothDampVelocity;   
    public PlayerState currentState;
    public int currentComboStep = 0;
    public float lastActionTime;            
    public bool isInvincible;    
}