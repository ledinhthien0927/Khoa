using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Nhan_ValeriusBT : MonoBehaviour
{
    [Header("--- REFERENCES ---")]
    public PlayerController Nhan_target;
    public Animator Nhan_anim;
    public CharacterController Nhan_cc;
    public Nhan_BossWeapon weaponScript;    // Script trên cây kiếm
    public Nhan_BossVision vision;          // Script tầm nhìn
    public SkinnedMeshRenderer bossMesh;    // Mesh để tàng hình

    [Header("--- MOVEMENT STATS ---")]
    public float Nhan_moveSpeed = 6.0f;       
    public float Nhan_sprintSpeed = 9.0f;     
    
    [Header("Zig-Zag Settings (Né Cung)")]
    public float Nhan_zigZagAmplitude = 4.0f; // Độ rộng
    public float Nhan_zigZagFrequency = 4.0f; // Tốc độ đảo

    [Header("--- COMBAT STATS ---")]
    public float Nhan_attackRange = 2.0f;
    public float Nhan_flankDist = 5.0f;       // Khoảng cách bắt đầu lướt tạt sườn

    [Header("Berserk Combo")]
    public int Nhan_hitsToStrong = 3;         // 3 đòn thường -> 1 đòn mạnh
    public float Nhan_timeToStrong = 5.0f;    // 5 giây -> 1 đòn mạnh

    [Header("--- SKILLS & COOLDOWNS ---")]
    public float Nhan_dashCooldown = 3.5f;    // Hồi chiêu lướt né
    public float Nhan_ambushCooldown = 12f;   // Hồi chiêu Ám Sát (Chui tường)
    public float Nhan_invisibleDuration = 1.5f; // Thời gian tàng hình
    
    // --- BIẾN RUNTIME ---
    [HideInInspector] public float Nhan_lastDashTime = -10f;
    [HideInInspector] public float Nhan_lastAmbushTime = -20f;
    [HideInInspector] public bool _hasFlanked = false;
    [HideInInspector] public int _currentHitCount = 0;
    [HideInInspector] public float _lastStrongAttackTime;
    
    private bool _isPerformingAction = false;
    private Nhan_Node _rootNode;
    
    public bool Nhan_IsParrying { get; private set; } = false;

    void Start()
    {
        // 1. Tìm các Component cần thiết
        if (Nhan_target == null) Nhan_target = FindFirstObjectByType<PlayerController>();
        Nhan_anim = GetComponent<Animator>();
        Nhan_cc = GetComponent<CharacterController>();
        vision = GetComponent<Nhan_BossVision>();
        
        if (bossMesh == null) bossMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        _lastStrongAttackTime = Time.time;

        // 2. Khởi tạo cây hành vi
        SetupBehaviorTree();
    }

    void Update()
    {
        // [CƠ CHẾ XOAY] Chỉ xoay về phía Player nếu ĐANG NHÌN THẤY và KHÔNG BẬN
        if (!_isPerformingAction && vision.canSeePlayer && Nhan_target != null)
        {
            Vector3 dir = (Nhan_target.transform.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f);
        }

        if (_rootNode != null) _rootNode.Evaluate();
    }

    // --- CẤU TRÚC CÂY HÀNH VI (BRAIN) ---
    void SetupBehaviorTree()
    {
        // 1. NHÁNH PHẢN XẠ (Ưu tiên cao nhất: Player ngắm -> Lướt né)
        Nhan_Sequence reactionSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckVision(this, true),     // Phải thấy mới né
            new Nhan_CheckPlayerAiming(this),
            new Nhan_Action_ReactionDash(this)
        });

        // 2. NHÁNH ÁM SÁT (Skill đặc biệt: Mất dấu hoặc Muốn đánh úp -> Chui tường)
        Nhan_Action_ShadowAmbush ambushAction = new Nhan_Action_ShadowAmbush(this);

        // 3. NHÁNH TẤN CÔNG (Khi thấy Player & Đủ gần)
        Nhan_Sequence combatSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckVision(this, true),
            new Nhan_CheckRange(this, 0, Nhan_attackRange),
            new Nhan_Action_BerserkAttack(this)
        });

        // 4. NHÁNH TRUY ĐUỔI (Khi thấy Player & Ở xa)
        Nhan_Sequence chaseSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckVision(this, true),
            new Nhan_Action_SmartApproach(this)
        });

        // 5. NHÁNH TÌM KIẾM (Khi MẤT DẤU -> Đi đến chỗ cuối cùng thấy)
        Nhan_Sequence investigateSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckVision(this, false),
            new Nhan_Action_Investigate(this)
        });

        // --- ROOT ---
        _rootNode = new Nhan_Selector(new List<Nhan_Node> {
            reactionSeq,    // 1. Né đòn
            ambushAction,   // 2. Móc lốp (nếu đủ điều kiện)
            combatSeq,      // 3. Đánh nhau
            chaseSeq,       // 4. Rượt đuổi
            investigateSeq  // 5. Đi tìm
        });
    }

    // --- HELPER FUNCTIONS ---
    public bool Nhan_IsBusy() => _isPerformingAction;
    public void Nhan_SetBusy(bool busy) => _isPerformingAction = busy;

    public bool Nhan_IsPlayerAiming()
    {
        if (Nhan_target == null) return false;
        return Nhan_target.GetView().GetComponent<Animator>().GetBool("IsAiming");
    }

    // Hàm Tàng Hình (Tắt Mesh + Tắt Va Chạm)
    public void ToggleInvisibility(bool invisible)
    {
        if(bossMesh) bossMesh.enabled = !invisible; 
        if(Nhan_cc) Nhan_cc.enabled = !invisible; // Tắt CC để đi xuyên tường
    }
}

// ==========================================================
// CÁC NODE ĐIỀU KIỆN (CONDITIONS)
// ==========================================================

public class Nhan_CheckRange : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    private float _min, _max;
    public Nhan_CheckRange(Nhan_ValeriusBT boss, float min, float max) { _boss = boss; _min = min; _max = max; }
    public override Nhan_NodeState Evaluate()
    {
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        return (dist >= _min && dist <= _max) ? Nhan_NodeState.SUCCESS : Nhan_NodeState.FAILURE;
    }
}

public class Nhan_CheckPlayerAiming : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_CheckPlayerAiming(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        return _boss.Nhan_IsPlayerAiming() ? Nhan_NodeState.SUCCESS : Nhan_NodeState.FAILURE;
    }
}

public class Nhan_CheckVision : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    private bool _expectSee;
    public Nhan_CheckVision(Nhan_ValeriusBT boss, bool expectSee) { _boss = boss; _expectSee = expectSee; }
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.vision.canSeePlayer == _expectSee) return Nhan_NodeState.SUCCESS;
        return Nhan_NodeState.FAILURE;
    }
}

// ==========================================================
// CÁC NODE HÀNH ĐỘNG (ACTIONS)
// ==========================================================

// --- 1. REACTION DASH (Lướt né phản xạ) ---
public class Nhan_Action_ReactionDash : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_ReactionDash(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        if (Time.time < _boss.Nhan_lastDashTime + _boss.Nhan_dashCooldown) return Nhan_NodeState.FAILURE;

        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }

    System.Collections.IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_lastDashTime = Time.time;
        _boss.Nhan_anim.SetTrigger("Dash");

        Vector3 dashDir = (Random.value > 0.5f) ? _boss.transform.right : -_boss.transform.right;
        
        float timer = 0;
        while(timer < 0.2f)
        {
            _boss.Nhan_cc.Move(dashDir * 30f * Time.deltaTime); // Dash cực nhanh
            
            // Xoay về Player
            Vector3 lookDir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
            lookDir.y = 0;
            _boss.transform.rotation = Quaternion.LookRotation(lookDir);

            timer += Time.deltaTime;
            yield return null;
        }
        _boss.Nhan_SetBusy(false);
    }
}

// --- 2. BERSERK ATTACK (Tấn công điên cuồng) ---
public class Nhan_Action_BerserkAttack : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_BerserkAttack(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }

    System.Collections.IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);

        // A. Xoay mặt về Player ngay lập tức (Để không chém vào không khí)
        Vector3 dir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) _boss.transform.rotation = Quaternion.LookRotation(dir);

        // B. Tính toán Combo
        bool timeCondition = Time.time > _boss._lastStrongAttackTime + _boss.Nhan_timeToStrong;
        bool hitCondition = _boss._currentHitCount >= _boss.Nhan_hitsToStrong;

        if (timeCondition || hitCondition)
        {
            // === ĐÒN MẠNH (HEAVY) ===
            _boss.Nhan_anim.SetTrigger("FeintStrike");
            _boss._currentHitCount = 0;
            _boss._lastStrongAttackTime = Time.time;
            
            yield return new WaitForSeconds(0.4f); // Chờ vung tay
            if(_boss.weaponScript) _boss.weaponScript.EnableHitbox(true);
            yield return new WaitForSeconds(0.5f); // Thời gian gây damage
            if(_boss.weaponScript) _boss.weaponScript.DisableHitbox();
        }
        else
        {
            // === ĐÒN THƯỜNG (LIGHT) ===
            _boss.Nhan_anim.SetTrigger("Attack");
            _boss._currentHitCount++;
            
            yield return new WaitForSeconds(0.2f); // Chờ vung tay
            if(_boss.weaponScript) _boss.weaponScript.EnableHitbox(false);
            yield return new WaitForSeconds(0.3f); // Thời gian gây damage
            if(_boss.weaponScript) _boss.weaponScript.DisableHitbox();
        }

        _boss._hasFlanked = false; // Reset tạt sườn
        _boss.Nhan_SetBusy(false);
    }
}

// --- 3. SMART APPROACH (Di chuyển thông minh) ---
public class Nhan_Action_SmartApproach : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_SmartApproach(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;

        Vector3 targetPos = _boss.Nhan_target.transform.position;
        Vector3 directionToTarget = (targetPos - _boss.transform.position).normalized;
        float dist = Vector3.Distance(_boss.transform.position, targetPos);

        // A. NẾU PLAYER NGẮM -> CHẠY ZIG-ZAG
        if (_boss.Nhan_IsPlayerAiming())
        {
            float zigZag = Mathf.Sin(Time.time * _boss.Nhan_zigZagFrequency) * _boss.Nhan_zigZagAmplitude;
            Vector3 moveDir = (directionToTarget + (_boss.transform.right * zigZag)).normalized;

            _boss.Nhan_cc.Move(moveDir * _boss.Nhan_moveSpeed * Time.deltaTime);
            _boss.Nhan_anim.SetFloat("Speed", 1f);

            // Xoay người theo hướng lắc (Visual Trick)
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
                Quaternion shakeRot = Quaternion.LookRotation(moveDir);
                _boss.transform.rotation = Quaternion.Slerp(_boss.transform.rotation, Quaternion.Lerp(targetRot, shakeRot, 0.5f), Time.deltaTime * 10f);
            }
        }
        // B. NẾU PLAYER CẦM KIẾM -> LAO THẲNG & TẠT SƯỜN
        else
        {
            if (dist < _boss.Nhan_flankDist && dist > _boss.Nhan_attackRange && !_boss._hasFlanked)
            {
                _boss.StartCoroutine(PerformFlank());
                return Nhan_NodeState.SUCCESS;
            }

            _boss.Nhan_cc.Move(directionToTarget * _boss.Nhan_sprintSpeed * Time.deltaTime);
            _boss.Nhan_anim.SetFloat("Speed", 1f);
            
            directionToTarget.y = 0;
            if(directionToTarget != Vector3.zero)
                _boss.transform.rotation = Quaternion.Slerp(_boss.transform.rotation, Quaternion.LookRotation(directionToTarget), Time.deltaTime * 20f);
        }

        return Nhan_NodeState.RUNNING;
    }

    System.Collections.IEnumerator PerformFlank()
    {
        _boss.Nhan_SetBusy(true);
        _boss._hasFlanked = true;
        _boss.Nhan_anim.SetTrigger("Dash");

        Vector3 flankDir = (Random.value > 0.5f) ? _boss.transform.right : -_boss.transform.right;
        float timer = 0;
        while(timer < 0.2f)
        {
            _boss.Nhan_cc.Move(flankDir * 35f * Time.deltaTime);
            Vector3 lookDir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
            lookDir.y = 0;
            _boss.transform.rotation = Quaternion.LookRotation(lookDir);
            timer += Time.deltaTime;
            yield return null;
        }
        _boss.Nhan_SetBusy(false);
    }
}

// --- 4. INVESTIGATE (Đi tìm khi mất dấu) ---
public class Nhan_Action_Investigate : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_Investigate(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;

        Vector3 target = _boss.vision.lastKnownPosition;
        float dist = Vector3.Distance(_boss.transform.position, target);

        if (dist > 1.5f)
        {
            Vector3 dir = (target - _boss.transform.position).normalized;
            _boss.Nhan_cc.Move(dir * _boss.Nhan_moveSpeed * Time.deltaTime);
            _boss.Nhan_anim.SetFloat("Speed", 1f);
            if(dir != Vector3.zero)
                _boss.transform.rotation = Quaternion.Slerp(_boss.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            return Nhan_NodeState.RUNNING;
        }
        else
        {
            // Đến nơi mà ko thấy -> Kích hoạt Ám Sát ngay lập tức
            _boss.Nhan_lastAmbushTime = -100f; // Reset cooldown giả
            return Nhan_NodeState.SUCCESS;
        }
    }
}

// --- 5. SHADOW AMBUSH (Sửa đổi: Tàng hình + Chạy siêu tốc xuyên vật thể) ---
public class Nhan_Action_ShadowAmbush : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_ShadowAmbush(Nhan_ValeriusBT boss) => _boss = boss;

    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        
        // Check Cooldown
        if (Time.time < _boss.Nhan_lastAmbushTime + _boss.Nhan_ambushCooldown) 
            return Nhan_NodeState.FAILURE;

        // Logic kích hoạt: 
        // 1. Không thấy Player (để đi tìm)
        // 2. HOẶC Random 30% khi đang đánh nhau để tạo bất ngờ
        if (_boss.vision.canSeePlayer && Random.value > 0.3f) return Nhan_NodeState.FAILURE;

        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }

    System.Collections.IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_lastAmbushTime = Time.time;

        // 1. BẮT ĐẦU ẨN THÂN
        Debug.Log("BOSS: Shadow Step - Ghost Mode!");
        _boss.Nhan_anim.SetTrigger("Dash"); 
        yield return new WaitForSeconds(0.2f);
        
        _boss.ToggleInvisibility(true); // Tắt Mesh (Tàng hình)
        
        // Tắt CharacterController để có thể đi xuyên tường/nhà (Ghosting)
        if(_boss.Nhan_cc) _boss.Nhan_cc.enabled = false;

        // 2. DI CHUYỂN TỐC ĐỘ CAO (120% Sprint Speed)
        float timer = 0;
        float maxDuration = _boss.Nhan_invisibleDuration; // Thời gian tối đa
        float ghostSpeed = _boss.Nhan_sprintSpeed * 1.2f; // 120% tốc độ chạy nhanh nhất

        while (timer < maxDuration)
        {
            // Mục tiêu: Vị trí ngay sau lưng Player (1.5m)
            Vector3 targetPos = _boss.Nhan_target.transform.position - (_boss.Nhan_target.transform.forward * 1.5f);
            
            // Giữ độ cao Y bằng với Player (để không bị chìm xuống đất hoặc bay lên trời)
            targetPos.y = _boss.Nhan_target.transform.position.y;

            // Tính khoảng cách và hướng
            float dist = Vector3.Distance(_boss.transform.position, targetPos);
            Vector3 dir = (targetPos - _boss.transform.position).normalized;

            // Di chuyển trực tiếp (Xuyên địa hình vì đã tắt CC)
            _boss.transform.position += dir * ghostSpeed * Time.deltaTime;

            // Xoay mặt theo hướng đang lao tới
            if (dir != Vector3.zero)
                _boss.transform.rotation = Quaternion.LookRotation(dir);

            // Nếu đã đến rất gần vị trí sau lưng (< 1m) -> Dừng chạy sớm
            if (dist < 1.0f) break;

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. XUẤT HIỆN SAU LƯNG
        // Bật lại va chạm vật lý
        if(_boss.Nhan_cc) _boss.Nhan_cc.enabled = true; 
        
        _boss.ToggleInvisibility(false); // Hiện hình

        // Xoay mặt thẳng vào lưng Player để đâm
        Vector3 finalDir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
        finalDir.y = 0;
        if(finalDir != Vector3.zero) _boss.transform.rotation = Quaternion.LookRotation(finalDir);

        // 4. TẤN CÔNG (Đâm lén)
        _boss.Nhan_anim.SetTrigger("FeintStrike");

        yield return new WaitForSeconds(0.3f); // Chờ vung tay
        if(_boss.weaponScript) _boss.weaponScript.EnableHitbox(true); // Bật kiếm
        yield return new WaitForSeconds(0.5f); // Thời gian gây damage
        if(_boss.weaponScript) _boss.weaponScript.DisableHitbox(); // Tắt kiếm

        _boss.Nhan_SetBusy(false);
    }

}