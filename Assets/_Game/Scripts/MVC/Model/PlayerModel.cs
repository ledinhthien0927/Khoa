using UnityEngine;

// Thêm Swimming vào Enum
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
    public float walkSpeed = 6f;
    public float aimMoveSpeed = 2f; 
    public float accelerationTime = 0.25f;
    public float decelerationTime = 0.4f;
    public float rotationSpeed = 15f;       
    public float dashForce = 15f;
    public float dashDuration = 0.4f;
    public float dashCost = 25f;
    public float dashIFrameDuration = 0.3f; 

    // --- [MỚI] CẤU HÌNH BƠI LỘI ---
    [Header("Swimming Config")]
    public float swimSpeed = 4f;            // Tốc độ bơi
    public float waterLevelY = 0f;          // Độ cao mặt nước (Mặc định 0)
    public float swimThreshold = 1.2f;      // Độ sâu ngập để bắt đầu bơi (1.2m ~ ngang bụng)
    public float surfaceBuoyancy = 0.5f;    // Khoảng cách từ mặt nước đến mắt (để đầu ngoi lên)
    public float buoyancySpeed = 2.0f;      // Tốc độ nổi lên
    // -----------------------------

    [Header("Inventory & Weapons")]
    public WeaponType currentWeapon = WeaponType.Sword;
    public int currentArrows = 5;
    public int maxArrows = 10;
    
    [Header("Sword Combat")]
    public float comboResetTime = 1.2f;
    public float parryWindow = 0.5f;        
    public float parryRecovery = 0.5f;      
    public float damageSwordBase = 10f;
    public LayerMask enemyLayer;      
    public float attackRange = 2.0f;  

    [Header("Bow Combat")]
    public float arrowShootSpeed = 100f;    
    public float reloadTime = 0.8f;         
    public float damageBow = 15f;
    public float zoomFOV = 40f;             
    public float normalFOV = 60f;

    [Header("Interaction & Smithing")]
    public LayerMask interactionLayer;      
    public float interactionRange = 2.0f;
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