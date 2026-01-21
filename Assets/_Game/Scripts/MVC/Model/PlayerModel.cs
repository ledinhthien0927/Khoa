using UnityEngine;

public enum PlayerState { Idle, Moving, Dashing, Attacking, Parrying, ParryingRecovery, Aiming, Stunned, Dead }
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
    public float aimMoveSpeed = 3f;         
    public float rotationSpeed = 15f;       
    public float dashForce = 15f;
    public float dashDuration = 0.4f;
    public float dashCost = 25f;
    public float dashIFrameDuration = 0.3f; 

    [Header("Inventory & Weapons")]
    public WeaponType currentWeapon = WeaponType.Sword;
    public int currentArrows = 5;
    public int maxArrows = 10;
    
    [Header("Sword Combat")]
    public float comboResetTime = 1.2f;
    public float parryWindow = 0.5f;        
    public float parryRecovery = 0.5f;      
    public float damageSwordBase = 10f;

    [Header("Bow Combat")]
    public float reloadTime = 0.8f;         
    public float damageBow = 15f;
    public float zoomFOV = 40f;             
    public float normalFOV = 60f;

    [Header("Interaction")]
    public LayerMask interactionLayer;      
    public float interactionRange = 2.0f;

    [Header("Runtime State")]
    public PlayerState currentState;
    public int currentComboStep = 0;
    public float lastActionTime;            
    public bool isInvincible;               
    public bool isWeaponSwapping;

    [Header("Visuals & Effects")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public GameObject vfxParrySparks;
}