using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Nhan_ValeriusBT : MonoBehaviour
{
    [Header("References")]
    public PlayerController Nhan_target;
    public Animator Nhan_anim;
    public CharacterController Nhan_cc;

    [Header("Settings")]
    public float Nhan_moveSpeed = 3.5f;
    public float Nhan_attackRange = 2.0f;
    public float Nhan_kickRange = 1.5f;
    public float Nhan_midRange = 6.0f;

    // Biến trạng thái
    private Nhan_Node _rootNode;
    private float _lastAbilityTime;
    private bool _isPerformingAction = false;
    public bool Nhan_IsParrying { get; private set; } = false; // Để BossStats kiểm tra

    void Start()
    {
        if (Nhan_target == null) Nhan_target = FindFirstObjectByType<PlayerController>();
        Nhan_anim = GetComponent<Animator>();
        Nhan_cc = GetComponent<CharacterController>();

        SetupBehaviorTree();
    }

    void Update()
    {
        if (_rootNode != null) _rootNode.Evaluate();
    }

    // --- CẤU TRÚC CÂY HÀNH VI ---
    void SetupBehaviorTree()
    {
        // 1. NHÁNH PHẢN XẠ (Ưu tiên cao nhất: Player ngắm bắn -> Lướt né)
        Nhan_Sequence reactionSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckPlayerAiming(this),
            new Nhan_Action_ReactionDash(this)
        });

        // 2. NHÁNH PHÒNG THỦ (Random Parry khi bị đánh - Logic giả lập)
        // (Bạn có thể thêm điều kiện CheckIsBeingAttacked nếu muốn kỹ hơn)
        Nhan_Action_NobleParry parryAction = new Nhan_Action_NobleParry(this);

        // 3. NHÁNH CẬN CHIẾN (< 1.5m)
        Nhan_Selector closeCombatSel = new Nhan_Selector(new List<Nhan_Node> {
            new Nhan_Action_DirtyKick(this),    // Ưu tiên đá
            new Nhan_Action_NormalAttack(this)  // Sau đó mới chém
        });
        
        Nhan_Sequence closeRangeSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckRange(this, 0, Nhan_kickRange),
            closeCombatSel
        });

        // 4. NHÁNH TẦM TRUNG (1.5m - 6m) -> Giả đòn
        Nhan_Sequence midRangeSeq = new Nhan_Sequence(new List<Nhan_Node> {
            new Nhan_CheckRange(this, Nhan_kickRange, Nhan_midRange),
            new Nhan_Action_FeintThrust(this)
        });

        // 5. NHÁNH TRUY ĐUỔI (Mặc định)
        Nhan_Action_Chase chaseAction = new Nhan_Action_Chase(this);

        // --- ROOT ---
        _rootNode = new Nhan_Selector(new List<Nhan_Node> {
            reactionSeq,
            // parryAction, // Tạm tắt Parry tự động để test đánh thường trước, mở ra nếu muốn Boss thủ
            closeRangeSeq,
            midRangeSeq,
            chaseAction
        });
    }

    // --- HELPER FUNCTIONS ---
    public bool Nhan_IsBusy() => _isPerformingAction;
    public void Nhan_SetBusy(bool busy) => _isPerformingAction = busy;
    public bool Nhan_IsCooldownReady() => Time.time > _lastAbilityTime + 2.0f;
    public void Nhan_ResetCooldown() => _lastAbilityTime = Time.time;
    public void Nhan_SetParrying(bool isParrying) => Nhan_IsParrying = isParrying;

    public void Nhan_RotateToTarget()
    {
        if (Nhan_target == null) return;
        Vector3 dir = (Nhan_target.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
    }
}

// ==========================================================
// CÁC NODE HÀNH ĐỘNG (ACTIONS & CONDITIONS)
// ==========================================================

// --- CONDITIONS ---
public class Nhan_CheckPlayerAiming : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_CheckPlayerAiming(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        // Kiểm tra Player có đang ngắm không (Dựa vào Animator của Player View)
        bool isAiming = _boss.Nhan_target.GetView().GetComponent<Animator>().GetBool("IsAiming");
        return isAiming ? Nhan_NodeState.SUCCESS : Nhan_NodeState.FAILURE;
    }
}

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

// --- ACTIONS ---

// 1. CHASE
public class Nhan_Action_Chase : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_Chase(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        float dist = Vector3.Distance(_boss.transform.position, _boss.Nhan_target.transform.position);
        
        if (dist > _boss.Nhan_attackRange)
        {
            Vector3 dir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
            _boss.Nhan_cc.Move(dir * _boss.Nhan_moveSpeed * Time.deltaTime);
            _boss.Nhan_anim.SetFloat("Speed", 1f);
            _boss.Nhan_RotateToTarget();
            return Nhan_NodeState.RUNNING;
        }
        
        _boss.Nhan_anim.SetFloat("Speed", 0f);
        return Nhan_NodeState.SUCCESS;
    }
}

// 2. REACTION DASH
public class Nhan_Action_ReactionDash : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_ReactionDash(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.RUNNING;
        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_anim.SetTrigger("Dash");
        _boss.Nhan_cc.Move(_boss.transform.right * 5f); // Lướt sang phải 1 chút ngay lập tức
        yield return new WaitForSeconds(0.5f);
        _boss.Nhan_SetBusy(false);
    }
}

// 3. DIRTY KICK
public class Nhan_Action_DirtyKick : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_DirtyKick(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy() || Random.value > 0.4f) return Nhan_NodeState.FAILURE; // 40% tỷ lệ đá
        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_anim.SetTrigger("Kick");
        yield return new WaitForSeconds(0.8f);
        _boss.Nhan_SetBusy(false);
    }
}

// 4. NORMAL ATTACK
public class Nhan_Action_NormalAttack : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_NormalAttack(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy()) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_RotateToTarget();
        _boss.Nhan_anim.SetTrigger("Attack");
        yield return new WaitForSeconds(1.0f);
        _boss.Nhan_SetBusy(false);
    }
}

// 5. FEINT THRUST (Giả đòn)
public class Nhan_Action_FeintThrust : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_FeintThrust(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy() || !_boss.Nhan_IsCooldownReady()) return Nhan_NodeState.FAILURE;
        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_ResetCooldown();

        _boss.Nhan_anim.SetTrigger("FeintStart");
        _boss.Nhan_RotateToTarget();
        yield return new WaitForSeconds(0.5f); // Giả vờ

        _boss.Nhan_anim.SetTrigger("FeintStrike");
        // Lao nhanh tới
        float timer = 0;
        Vector3 dashDir = (_boss.Nhan_target.transform.position - _boss.transform.position).normalized;
        while(timer < 0.2f)
        {
            _boss.Nhan_cc.Move(dashDir * 15f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitForSeconds(0.8f);
        _boss.Nhan_SetBusy(false);
    }
}

// 6. NOBLE PARRY
public class Nhan_Action_NobleParry : Nhan_Node
{
    private Nhan_ValeriusBT _boss;
    public Nhan_Action_NobleParry(Nhan_ValeriusBT boss) => _boss = boss;
    public override Nhan_NodeState Evaluate()
    {
        if (_boss.Nhan_IsBusy() || Random.value > 0.1f) return Nhan_NodeState.FAILURE; // Tỷ lệ thấp
        _boss.StartCoroutine(Execute());
        return Nhan_NodeState.SUCCESS;
    }
    IEnumerator Execute()
    {
        _boss.Nhan_SetBusy(true);
        _boss.Nhan_SetParrying(true);
        _boss.Nhan_anim.SetBool("IsParrying", true);
        
        yield return new WaitForSeconds(1.5f);
        
        _boss.Nhan_anim.SetBool("IsParrying", false);
        _boss.Nhan_SetParrying(false);
        _boss.Nhan_SetBusy(false);
    }
}