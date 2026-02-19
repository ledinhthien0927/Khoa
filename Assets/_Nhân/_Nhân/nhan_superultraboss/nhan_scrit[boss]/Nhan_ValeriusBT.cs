using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Nhan_ValeriusBT : MonoBehaviour
{
    [Header("--- REFERENCES ---")]
    public PlayerController Nhan_target;
    public Animator Nhan_anim;
    public CharacterController Nhan_cc;
    
    // List chứa 2 cây kiếm
    public List<Nhan_BossWeapon> weaponScripts;    
    
    public Nhan_BossVision vision;          
    public Nhan_BossStats stats; 

    [Header("--- MOVEMENT STATS ---")]
    public float Nhan_moveSpeed = 6.0f;       
    public float Nhan_sprintSpeed = 9.0f;     
    
    [Header("Zig-Zag Settings")]
    public float Nhan_zigZagAmplitude = 4.0f; 
    public float Nhan_zigZagFrequency = 4.0f; 

    [Header("--- COMBAT ---")]
    public float Nhan_attackRange = 2.5f;
    public float Nhan_shootRange = 8.0f;     

    [Header("--- PHASE 2 SETTINGS ---")]
    public float Nhan_hpCostPerDash = 15f;   
    public float Nhan_phase2SpeedMult = 1.5f; 
    public float Nhan_phase2DamageMult = 1.5f;

    [Header("--- SKILLS ---")]
    public GameObject Nhan_projectilePrefab;
    public Transform Nhan_firePoint;
    public float Nhan_shootCooldown = 6f;

    [Header("--- NEW SKILLS (MỚI) ---")]
    public float Nhan_tauntCooldown = 20f;     // Hồi chiêu múa kiếm
    public float Nhan_buffDuration = 10f;      // Thời gian tăng dame sau khi múa
    public float Nhan_dashSlashCooldown = 10f; // Hồi chiêu lướt chém
    public float Nhan_dashSlashSpeed = 35f;    // Tốc độ lướt chém

    [Header("--- COOLDOWNS ---")]
    public float Nhan_dashCooldown = 3.5f;    
    public float Nhan_ambushCooldown = 30f;   
    public float Nhan_invisibleDuration = 1.5f;

    // --- RUNTIME ---
    [HideInInspector] public float _lastShootTime = -10f;
    [HideInInspector] public float Nhan_lastAmbushTime = -100f; 
    [HideInInspector] public float Nhan_lastDashTime = -10f;
    [HideInInspector] public float _lastTauntTime = -20f;      // [MỚI]
    [HideInInspector] public float _lastDashSlashTime = -10f;  // [MỚI]
    
    private bool _isPerformingAction = false;
    private Nhan_Node _rootNode;
    public bool IsPhase2 { get; private set; } = false;
    public bool IsBuffed { get; private set; } = false; // [MỚI] Đang được buff hay ko

    void Start()
    {
        if (Nhan_target == null) Nhan_target = FindFirstObjectByType<PlayerController>();
        Nhan_anim = GetComponent<Animator>();
        Nhan_cc = GetComponent<CharacterController>();
        vision = GetComponent<Nhan_BossVision>();
        stats = GetComponent<Nhan_BossStats>();

        SetupBehaviorTree();
    }

    void Update()
    {
        if (!_isPerformingAction && vision.canSeePlayer && Nhan_target != null)
        {
            Vector3 dir = (Nhan_target.transform.position - transform.position).normalized;
            dir.y = 0;
            float rotSpeed = IsPhase2 ? 50f : 15f; 
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotSpeed);
        }

        if (_rootNode != null) _rootNode.Evaluate();
    }

    public void EnterPhase2()
    {
        IsPhase2 = true;
        Nhan_anim.SetBool("IsPhase2", true);
        ApplyDamageMultiplier(Nhan_phase2DamageMult); // Tăng dame vĩnh viễn P2
    }

    // --- HỆ THỐNG BUFF DAMAGE ---
    public void ApplyTemporaryBuff()
    {
        StartCoroutine(BuffRoutine());
    }

    IEnumerator BuffRoutine()
    {
        IsBuffed = true;
        // Tăng gấp đôi damage hiện tại
        ApplyDamageMultiplier(2.0f);
        Debug.Log("BOSS BUFFED: DAMAGE X2!");
        
        // Hiện effect nếu có (bạn tự thêm VFX vào đây)

        yield return new WaitForSeconds(Nhan_buffDuration);

        // Hết giờ thì chia đôi để về như cũ
        ApplyDamageMultiplier(0.5f);
        IsBuffed = false;
        Debug.Log("BOSS BUFF ENDED");
    }

    void ApplyDamageMultiplier(float mult)
    {
        if (weaponScripts != null)
        {
            foreach (var sword in weaponScripts)
            {
                if (sword != null)
                {
                    sword.damage *= mult;
                    sword.heavyDamage *= mult;
                }
            }
        }
    }

    // --- HÀM BẬT/TẮT HITBOX ---
    public void EnableAllWeapons(bool isHeavy)
    {
        if (weaponScripts == null) return;
        foreach(var sword in weaponScripts)
        {
            if(sword != null) sword.EnableHitbox(isHeavy);
        }
    }

    public void DisableAllWeapons()
    {
        if (weaponScripts == null) return;
        foreach(var sword in weaponScripts)
        {
            if(sword != null) sword.DisableHitbox();
        }
    }

    void SetupBehaviorTree()
    {
        // 1. KỸ NĂNG ĐẶC BIỆT
        Nhan_Action_ShadowAmbush ambushAction = new Nhan_Action_ShadowAmbush(this);
        Nhan_Action_TauntBuff tauntAction = new Nhan_Action_TauntBuff(this); // [MỚI] Múa kiếm

        // 2. KỸ NĂNG TIẾP CẬN/TẤN CÔNG XA
        Nhan_Action_DashSlash dashSlashAction = new Nhan_Action_DashSlash(this); // [MỚI] Lướt chém
        Nhan_Action_BackstepShoot shootAction = new Nhan_Action_BackstepShoot(this);

        // 3. COMBO CẬN CHIẾN
        Nhan_Sequence combatSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckRange(this, 0, Nhan_attackRange),
            new Nhan_Action_RandomAttack(this)
        });

        // 4. DI CHUYỂN
        Nhan_Sequence chaseSeq = new Nhan_Sequence(new List<Nhan_Node> {
             new Nhan_CheckVision(this, true), 
             new Nhan_Action_DashApproach(this)
        });

        // 5. TÌM KIẾM
        Nhan_Sequence investigateSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckVision(this, false), 
            new Nhan_Action_Investigate(this)
        });

        // --- ROOT SELECTOR (Ưu tiên từ trên xuống dưới) ---
        _rootNode = new Nhan_Selector(new List<Nhan_Node> {
            ambushAction,    // 1. Móc lốp (30s)
            tauntAction,     // 2. [MỚI] Múa kiếm buff (20s)
            dashSlashAction, // 3. [MỚI] Lướt chém (10s)
            shootAction,     // 4. Bắn lén
            combatSeq,       // 5. Đánh gần
            chaseSeq,        // 6. Lao tới
            investigateSeq   // 7. Đi tìm
        });
    }

    // --- HELPER ---
    public bool Nhan_IsBusy() => _isPerformingAction;
    public void Nhan_SetBusy(bool busy) => _isPerformingAction = busy;

    public void FireProjectile()
    {
        if (!Nhan_projectilePrefab) return;
        Vector3 spawnPos = Nhan_firePoint ? Nhan_firePoint.position : transform.position + Vector3.up;
        Vector3 targetPos = Nhan_target.transform.position + Vector3.up;
        GameObject bullet = Instantiate(Nhan_projectilePrefab, spawnPos, Quaternion.LookRotation(targetPos - spawnPos));
        
        Collider c1 = bullet.GetComponent<Collider>();
        Collider c2 = GetComponent<Collider>();
        if(c1 && c2) Physics.IgnoreCollision(c1, c2);
    }
    
    public void ToggleInvisibility(bool invisible)
    {
        SkinnedMeshRenderer mesh = GetComponentInChildren<SkinnedMeshRenderer>();
        if(mesh) mesh.enabled = !invisible;
        if(Nhan_cc) Nhan_cc.enabled = !invisible; 
    }
}

// ==========================================================
// CÁC NODE CŨ (GIỮ NGUYÊN)
// ==========================================================
public class Nhan_CheckRange : Nhan_Node {
    private Nhan_ValeriusBT _boss; private float _min, _max;
    public Nhan_CheckRange(Nhan_ValeriusBT boss, float min, float max) { _boss = boss; _min = min; _max = max; }
    public override Nhan_NodeState Evaluate() {
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        return (dist >= _min && dist <= _max) ? Nhan_NodeState.SUCCESS : Nhan_NodeState.FAILURE;
    }
}
public class Nhan_CheckVision : Nhan_Node {
    private Nhan_ValeriusBT _boss; private bool _expectSee;
    public Nhan_CheckVision(Nhan_ValeriusBT boss, bool expectSee) { _boss = boss; _expectSee = expectSee; }
    public override Nhan_NodeState Evaluate() {
        return (_boss.vision.canSeePlayer == _expectSee) ? Nhan_NodeState.SUCCESS : Nhan_NodeState.FAILURE;
    }
}

// ==========================================================
// CÁC NODE MỚI (SKILL)
// ==========================================================

// --- [MỚI] TAUNT BUFF (MÚA KIẾM) ---
public class Nhan_Action_TauntBuff : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_TauntBuff(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        // Cooldown
        if (Time.time < _boss._lastTauntTime + _boss.Nhan_tauntCooldown) return Nhan_NodeState.FAILURE;
        // Chỉ múa khi Player ở hơi xa (để ko bị đánh lúc đang múa)
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        if (dist < 4.0f) return Nhan_NodeState.FAILURE;
        // Random 30%
        if (Random.value > 0.3f) return Nhan_NodeState.FAILURE;

        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }

    System.Collections.IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss._lastTauntTime = Time.time;

        // Trigger Animation Múa
        _boss.Nhan_anim.SetTrigger("Taunt");
        Debug.Log("BOSS: Ngươi quá yếu! (Taunting...)");

        // Đứng im múa trong 2s
        yield return new WaitForSeconds(2.0f);

        // Kích hoạt Buff Damage
        _boss.ApplyTemporaryBuff();

        _boss.Nhan_SetBusy(false);
    }
}

// --- [MỚI] DASH SLASH (LƯỚT CHÉM) ---
public class Nhan_Action_DashSlash : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_DashSlash(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        if (Time.time < _boss._lastDashSlashTime + _boss.Nhan_dashSlashCooldown) return Nhan_NodeState.FAILURE;
        
        // Tầm sử dụng: Trung bình (4m - 10m)
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        if (dist < 3.0f || dist > 12.0f) return Nhan_NodeState.FAILURE;

        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }

    System.Collections.IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss._lastDashSlashTime = Time.time;

        // 1. Chuẩn bị (Xoay mặt về Player)
        Vector3 dir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
        dir.y = 0;
        _boss.transform.rotation = Quaternion.LookRotation(dir);

        _boss.Nhan_anim.SetTrigger("DashAttack"); 
        
        // Delay 1 xíu để animation lấy đà
        yield return new WaitForSeconds(0.2f);

        // 2. Lao lên & Bật kiếm
        _boss.EnableAllWeapons(true); // Hitbox mạnh
        
        float dashTimer = 0;
        float dashDuration = 0.4f; // Thời gian lướt
        Vector3 dashDir = _boss.transform.forward;

        while (dashTimer < dashDuration)
        {
            // Lướt cực nhanh
            _boss.Nhan_cc.Move(dashDir * _boss.Nhan_dashSlashSpeed * Time.deltaTime);
            dashTimer += Time.deltaTime;
            yield return null;
        }

        // 3. Kết thúc
        _boss.DisableAllWeapons();
        
        // Khựng lại 1 chút sau khi chém
        yield return new WaitForSeconds(0.5f);

        _boss.Nhan_SetBusy(false);
    }
}

// ==========================================================
// CÁC ACTION CŨ (Ambush, Attack, Shoot...) - ĐÃ RÚT GỌN ĐỂ DỄ NHÌN
// (Bạn giữ nguyên logic cũ, chỉ cần đảm bảo tên class khớp)
// ==========================================================

public class Nhan_Action_ShadowAmbush : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_ShadowAmbush(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() {
        if (_boss.Nhan_IsBusy() || Time.time < _boss.Nhan_lastAmbushTime + _boss.Nhan_ambushCooldown) return Nhan_NodeState.FAILURE;
        if (_boss.vision.canSeePlayer && Random.value > 0.2f) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); _boss.Nhan_lastAmbushTime = Time.time;
        _boss.Nhan_anim.SetTrigger("Dash"); yield return new WaitForSeconds(0.2f);
        _boss.ToggleInvisibility(true);
        float t = 0; while(t < _boss.Nhan_invisibleDuration) {
            Vector3 target = _boss.Nhan_target.transform.position - _boss.Nhan_target.transform.forward * 1.5f; target.y = _boss.Nhan_target.transform.position.y;
            Vector3 dir = (target - _boss.transform.position).normalized;
            _boss.transform.position += dir * 15f * Time.deltaTime;
            if(dir != Vector3.zero) _boss.transform.rotation = Quaternion.LookRotation(dir);
            if(Vector3.Distance(_boss.transform.position, target) < 1f) break;
            t += Time.deltaTime; yield return null;
        }
        _boss.ToggleInvisibility(false);
        Vector3 finalDir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized; finalDir.y=0;
        if(finalDir!=Vector3.zero) _boss.transform.rotation=Quaternion.LookRotation(finalDir);
        _boss.Nhan_anim.SetTrigger("FeintStrike"); yield return new WaitForSeconds(0.3f);
        _boss.EnableAllWeapons(true); yield return new WaitForSeconds(0.5f); _boss.DisableAllWeapons();
        _boss.Nhan_SetBusy(false);
    }
}

public class Nhan_Action_DashApproach : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_DashApproach(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() { if(_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE; _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS; }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); if(_boss.IsPhase2 && _boss.stats) _boss.stats.BurnHealth(_boss.Nhan_hpCostPerDash);
        _boss.Nhan_anim.SetTrigger("Dash");
        Vector3 dir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
        Vector3 side = (Random.value>0.5f)?_boss.transform.right:-_boss.transform.right;
        Vector3 move = (dir + side * (_boss.IsPhase2?0.2f:0.8f)).normalized;
        float speed = _boss.IsPhase2 ? _boss.Nhan_sprintSpeed*_boss.Nhan_phase2SpeedMult : _boss.Nhan_sprintSpeed;
        float t=0; while(t<0.2f){ _boss.Nhan_cc.Move(move*speed*Time.deltaTime); 
        Vector3 ld=(_boss.Nhan_target.transform.position-_boss.transform.position).normalized; ld.y=0; _boss.transform.rotation=Quaternion.LookRotation(ld);
        t+=Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(_boss.IsPhase2?0.1f:0.3f); _boss.Nhan_SetBusy(false);
    }
}

public class Nhan_Action_RandomAttack : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_RandomAttack(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() { if(_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE; _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS; }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); Vector3 d = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized; d.y=0; if(d!=Vector3.zero)_boss.transform.rotation=Quaternion.LookRotation(d);
        _boss.Nhan_anim.SetInteger("AttackIndex", Random.Range(1,4)); _boss.Nhan_anim.SetTrigger("Attack");
        yield return new WaitForSeconds(_boss.IsPhase2?0.1f:0.2f);
        _boss.EnableAllWeapons(_boss.IsPhase2); yield return new WaitForSeconds(0.3f); _boss.DisableAllWeapons();
        yield return new WaitForSeconds(_boss.IsPhase2?0.2f:0.5f); _boss.Nhan_SetBusy(false);
    }
}

public class Nhan_Action_BackstepShoot : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_BackstepShoot(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() {
        if(_boss.Nhan_IsBusy() || Time.time < _boss._lastShootTime + _boss.Nhan_shootCooldown) return Nhan_NodeState.FAILURE;
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        if(dist>_boss.Nhan_shootRange||dist<2f || Random.value>0.4f) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); _boss._lastShootTime = Time.time;
        _boss.Nhan_anim.SetTrigger("Dash"); float t=0; while(t<0.3f){ _boss.Nhan_cc.Move(-_boss.transform.forward*20f*Time.deltaTime); t+=Time.deltaTime; yield return null; }
        _boss.Nhan_anim.SetTrigger("Shoot"); yield return new WaitForSeconds(0.2f); _boss.FireProjectile(); yield return new WaitForSeconds(0.5f); _boss.Nhan_SetBusy(false);
    }
}

public class Nhan_Action_Investigate : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_Investigate(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() {
        if(_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        Vector3 t = _boss.vision.lastKnownPosition;
        if(Vector3.Distance(_boss.transform.position, t)>1.5f) {
            Vector3 d = (t - _boss.transform.position).normalized; _boss.Nhan_cc.Move(d*_boss.Nhan_moveSpeed*Time.deltaTime);
            _boss.Nhan_anim.SetFloat("Speed", 1f); if(d!=Vector3.zero) _boss.transform.rotation=Quaternion.Slerp(_boss.transform.rotation, Quaternion.LookRotation(d), Time.deltaTime*5f);
            return Nhan_NodeState.RUNNING;
        } else { _boss.Nhan_lastAmbushTime = -100f; return Nhan_NodeState.SUCCESS; }
    }
}