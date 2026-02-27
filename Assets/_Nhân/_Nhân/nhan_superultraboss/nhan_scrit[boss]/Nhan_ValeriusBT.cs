using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Nhan_ValeriusBT : MonoBehaviour
{
    [Header("--- REFERENCES ---")]
    public PlayerController Nhan_target;
    public Animator Nhan_anim;
    public CharacterController Nhan_cc;
    
    // Quản lý 2 kiếm
    public List<Nhan_BossWeapon> weaponScripts;    
    public Nhan_BossVision vision;          
    public Nhan_BossStats stats; 

    [Header("--- MOVEMENT STATS ---")]
    public float Nhan_moveSpeed = 4.0f;       
    public float Nhan_sprintSpeed = 7.0f;     // Tốc độ chạy đuổi theo player

    [Header("--- COMBAT ---")]
    // [ĐÃ SỬA] Giảm tầm đánh xuống cực ngắn để Boss phải áp sát người mới chém
    public float Nhan_attackRange = 1.3f;    
    public float Nhan_shootRange = 8.0f;     

    [Header("--- SKILLS COOLDOWNS ---")]
    // [ĐÃ SỬA] Tăng thời gian hồi chiêu để bớt spam
    public float Nhan_ambushCooldown = 60f;    // 1 phút mới tàng hình 1 lần
    public float Nhan_dashSlashCooldown = 45f; // Lướt chém xuyên người 45s/lần
    public float Nhan_tauntCooldown = 30f;     // Múa kiếm 30s/lần
    public float Nhan_shootCooldown = 15f;     // Bắn đạn 15s/lần

    [Header("--- PHASE 2 SETTINGS ---")]
    public float Nhan_phase2SpeedMult = 1.3f; 
    public float Nhan_phase2DamageMult = 1.5f;
    public float Nhan_dashSlashSpeed = 20f;    

    [Header("--- PROJECTILE ---")]
    public GameObject Nhan_projectilePrefab;
    public Transform Nhan_firePoint;

    // --- RUNTIME ---
    [HideInInspector] public float _lastShootTime = -10f;
    [HideInInspector] public float Nhan_lastAmbushTime = -100f; 
    [HideInInspector] public float _lastTauntTime = -20f;      
    [HideInInspector] public float _lastDashSlashTime = -10f;  
    
    private bool _isPerformingAction = false;
    private Nhan_Node _rootNode;
    public bool IsPhase2 { get; private set; } = false;

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
            float rotSpeed = IsPhase2 ? 20f : 10f; 
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotSpeed);
        }

        if (_rootNode != null) _rootNode.Evaluate();
    }

    public void EnterPhase2()
    {
        IsPhase2 = true;
        Nhan_anim.SetBool("IsPhase2", true);
        EnableBuff(Nhan_phase2DamageMult); 
    }

    public void ApplyTemporaryBuff()
    {
        StartCoroutine(BuffRoutine());
    }

    IEnumerator BuffRoutine()
    {
        EnableBuff(2.0f);
        yield return new WaitForSeconds(10f);
        EnableBuff(0.5f);
    }

    void EnableBuff(float mult)
    {
        if (weaponScripts == null) return;
        foreach (var sword in weaponScripts)
        {
            if (sword != null) { sword.damage *= mult; sword.heavyDamage *= mult; }
        }
    }

    public void EnableAllWeapons(bool isHeavy)
    {
        if (weaponScripts == null) return;
        foreach(var sword in weaponScripts) { if(sword != null) sword.EnableHitbox(isHeavy); }
    }

    public void DisableAllWeapons()
    {
        if (weaponScripts == null) return;
        foreach(var sword in weaponScripts) { if(sword != null) sword.DisableHitbox(); }
    }

    void SetupBehaviorTree()
    {
        Nhan_Action_ShadowAmbush ambushAction = new Nhan_Action_ShadowAmbush(this);
        Nhan_Action_TauntBuff tauntAction = new Nhan_Action_TauntBuff(this); 
        Nhan_Action_DashSlash dashSlashAction = new Nhan_Action_DashSlash(this); 
        Nhan_Action_BackstepShoot shootAction = new Nhan_Action_BackstepShoot(this);

        Nhan_Sequence combatSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckRange(this, 0, Nhan_attackRange),
            new Nhan_Action_RandomAttack(this)
        });

        // [ĐÃ SỬA] Dùng hành động Chạy bộ bình thường để tiếp cận, KHÔNG lướt nhảy nữa
        Nhan_Sequence chaseSeq = new Nhan_Sequence(new List<Nhan_Node> {
             new Nhan_CheckVision(this, true), 
             new Nhan_Action_NormalChase(this) 
        });

        Nhan_Sequence investigateSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckVision(this, false), 
            new Nhan_Action_NormalChase(this) // Dùng chung hàm chạy
        });

        _rootNode = new Nhan_Selector(new List<Nhan_Node> {
            ambushAction,    
            tauntAction,     
            dashSlashAction, 
            shootAction,     
            combatSeq,       
            chaseSeq,        
            investigateSeq   
        });
    }

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
        // Bỏ vụ xuyên tường
    }
}

// ==========================================================
// ĐIỀU KIỆN 
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
// HÀNH ĐỘNG DI CHUYỂN BÌNH THƯỜNG (KHÔNG LƯỚT)
// ==========================================================
public class Nhan_Action_NormalChase : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_NormalChase(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;

        Vector3 targetPos = _boss.vision.canSeePlayer ? _boss.Nhan_target.transform.position : _boss.vision.lastKnownPosition;
        float dist = Vector3.Distance(_boss.transform.position, targetPos);

        if (dist > _boss.Nhan_attackRange)
        {
            Vector3 dir = (targetPos - _boss.transform.position).normalized;
            dir.y = 0;
            
            float speed = _boss.IsPhase2 ? _boss.Nhan_sprintSpeed * _boss.Nhan_phase2SpeedMult : _boss.Nhan_sprintSpeed;
            _boss.Nhan_cc.Move(dir * speed * Time.deltaTime); 
            
            // Set animation chạy
            _boss.Nhan_anim.SetFloat("Speed", 1f);
            
            if(dir != Vector3.zero)
                _boss.transform.rotation = Quaternion.Slerp(_boss.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
            
            return Nhan_NodeState.RUNNING;
        }

        return Nhan_NodeState.SUCCESS;
    }
}

// ==========================================================
// TẤN CÔNG THƯỜNG (CÓ BƯỚC TỚI THEO NHỊP ANIMATION)
// ==========================================================
public class Nhan_Action_RandomAttack : Nhan_Node {
    private Nhan_ValeriusBT _boss; 
    public Nhan_Action_RandomAttack(Nhan_ValeriusBT b) => _boss = b;
    
    public override Nhan_NodeState Evaluate() { 
        if(_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE; 
        _boss.StartCoroutine(Execute()); 
        return Nhan_NodeState.SUCCESS; 
    }
    
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); 
        
        // 1. Dừng chạy bộ, xoay mặt về Player
        _boss.Nhan_anim.SetFloat("Speed", 0f);
        Vector3 d = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized; 
        d.y = 0; 
        if(d != Vector3.zero) _boss.transform.rotation = Quaternion.LookRotation(d);
        
        // 2. Random chiêu và kích hoạt Animation
        _boss.Nhan_anim.SetInteger("AttackIndex", Random.Range(1,4)); 
        _boss.Nhan_anim.SetTrigger("Attack");

        // 3. Thời gian lấy đà vung kiếm (Windup)
        float windup = _boss.IsPhase2 ? 0.1f : 0.2f;
        yield return new WaitForSeconds(windup);

        // 4. BẬT KIẾM & BƯỚC TỚI CHÉM
        _boss.EnableAllWeapons(_boss.IsPhase2); 
        
        float attackTimer = 0;
        float attackDuration = 0.4f; // Thời gian duy trì đòn chém
        
        // Tốc độ bước tới khi chém (bạn có thể tăng/giảm số 3.0f này để khớp với animation)
        float stepForwardSpeed = _boss.IsPhase2 ? 4.5f : 3.0f; 

        while (attackTimer < attackDuration) {
            // Đẩy Boss tiến về phía trước mặt
            _boss.Nhan_cc.Move(_boss.transform.forward * stepForwardSpeed * Time.deltaTime);
            
            attackTimer += Time.deltaTime;
            yield return null; // Đợi frame tiếp theo
        }

        // 5. TẮT KIẾM
        _boss.DisableAllWeapons();
        
        // 6. Đứng khựng lại thu kiếm
        yield return new WaitForSeconds(_boss.IsPhase2 ? 0.3f : 0.6f); 
        
        _boss.Nhan_SetBusy(false);
    }
}

// ==========================================================
// KỸ NĂNG: ÁM SÁT (TÀNG HÌNH)
// ==========================================================
public class Nhan_Action_ShadowAmbush : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_ShadowAmbush(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() {
        if (_boss.Nhan_IsBusy() || Time.time < _boss.Nhan_lastAmbushTime + _boss.Nhan_ambushCooldown) return Nhan_NodeState.FAILURE;
        // Chỉ tàng hình khi Random trúng (20%) HOẶC khi không thấy Player
        if (_boss.vision.canSeePlayer && Random.value > 0.2f) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); _boss.Nhan_lastAmbushTime = Time.time;
        _boss.Nhan_anim.SetTrigger("Dash"); yield return new WaitForSeconds(0.2f);
        _boss.ToggleInvisibility(true);
        float t = 0; while(t < 1.5f) {
            Vector3 target = _boss.Nhan_target.transform.position - _boss.Nhan_target.transform.forward * 1.5f; target.y = _boss.Nhan_target.transform.position.y;
            Vector3 dir = (target - _boss.transform.position).normalized;
            
            // Dùng CC Move để đụng tường thì khựng lại, không bị xuyên
            _boss.Nhan_cc.Move(dir * 15f * Time.deltaTime);
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

// ==========================================================
// KỸ NĂNG: LƯỚT CHÉM (DASH SLASH)
// ==========================================================
public class Nhan_Action_DashSlash : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_DashSlash(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        if (Time.time < _boss._lastDashSlashTime + _boss.Nhan_dashSlashCooldown) return Nhan_NodeState.FAILURE;
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        if (dist > 2.5f) return Nhan_NodeState.FAILURE; // Phải đứng rất gần mới được dùng
        _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); _boss._lastDashSlashTime = Time.time;
        Vector3 dir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized; dir.y = 0;
        _boss.transform.rotation = Quaternion.LookRotation(dir);
        _boss.Nhan_anim.SetTrigger("DashAttack"); yield return new WaitForSeconds(0.2f);
        _boss.EnableAllWeapons(true); 
        float t = 0; Vector3 dashDir = _boss.transform.forward;
        while (t < 0.3f) {
            _boss.Nhan_cc.Move(dashDir * _boss.Nhan_dashSlashSpeed * Time.deltaTime); // Kẹt tường sẽ dừng
            t += Time.deltaTime; yield return null;
        }
        _boss.DisableAllWeapons(); yield return new WaitForSeconds(0.5f);
        _boss.Nhan_SetBusy(false);
    }
}

// ==========================================================
// KỸ NĂNG: MÚA KIẾM (TAUNT) & BẮN ĐẠN LÙI
// ==========================================================
public class Nhan_Action_TauntBuff : Nhan_Node {
    private Nhan_ValeriusBT _boss; public Nhan_Action_TauntBuff(Nhan_ValeriusBT b) => _boss = b;
    public override Nhan_NodeState Evaluate() {
        if (_boss.Nhan_IsBusy() || Time.time < _boss._lastTauntTime + _boss.Nhan_tauntCooldown) return Nhan_NodeState.FAILURE;
        if (Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position) < 4.0f || Random.value > 0.3f) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute()); return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); _boss._lastTauntTime = Time.time;
        _boss.Nhan_anim.SetTrigger("Taunt"); yield return new WaitForSeconds(2.0f);
        _boss.ApplyTemporaryBuff(); _boss.Nhan_SetBusy(false);
    }
}

public class Nhan_Action_BackstepShoot : Nhan_Node {
    private Nhan_ValeriusBT _boss; 
    private float _thinkDelay = 0f; // Biến chống spam check mỗi frame

    public Nhan_Action_BackstepShoot(Nhan_ValeriusBT b) => _boss = b;

    public override Nhan_NodeState Evaluate() {
        if(_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        
        // 1. Kiểm tra Cooldown chính (Ví dụ: 15s mới được bắn 1 lần)
        if (Time.time < _boss._lastShootTime + _boss.Nhan_shootCooldown) 
            return Nhan_NodeState.FAILURE;

        // 2. Kiểm tra Mini-Cooldown (Chống spam tỷ lệ mỗi frame)
        if (Time.time < _thinkDelay) 
            return Nhan_NodeState.FAILURE;

        // 3. Tầm bắn (Chỉ bắn khi đứng cách 3m đến 10m)
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        if(dist > 10f || dist < 3.0f) 
            return Nhan_NodeState.FAILURE;

        // 4. Tỷ lệ kích hoạt 30%. Nếu "Xịt", Boss sẽ quên chiêu này trong 2 giây rồi mới xét lại.
        if (Random.value > 0.3f) {
            _thinkDelay = Time.time + 2.0f; 
            return Nhan_NodeState.FAILURE;
        }

        _boss.StartCoroutine(Execute()); 
        return Nhan_NodeState.SUCCESS;
    }

    System.Collections.IEnumerator Execute() {
        _boss.Nhan_SetBusy(true); 
        _boss._lastShootTime = Time.time;

        // Xoay mặt nhìn Player trước khi nhảy lùi
        Vector3 lookDir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
        lookDir.y = 0;
        if(lookDir != Vector3.zero) _boss.transform.rotation = Quaternion.LookRotation(lookDir);

        _boss.Nhan_anim.SetTrigger("Dash"); 
        
        // Lướt lùi về sau
        float t = 0; 
        Vector3 backDir = -_boss.transform.forward; 
        while(t < 0.3f) { 
            _boss.Nhan_cc.Move(backDir * 15f * Time.deltaTime); 
            t += Time.deltaTime; 
            yield return null; 
        }

        // Kích hoạt bắn
        _boss.Nhan_anim.SetTrigger("Shoot"); 
        yield return new WaitForSeconds(0.2f); // Đợi tay vung ra
        
        _boss.FireProjectile(); 
        
        yield return new WaitForSeconds(0.6f); // Đứng yên tạo dáng sau khi bắn
        
        _boss.Nhan_SetBusy(false);
    }
}