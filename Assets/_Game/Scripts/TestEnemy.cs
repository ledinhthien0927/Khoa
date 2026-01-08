using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class TestEnemy : MonoBehaviour, IDamageable
{
    private Rigidbody _rb;
    private Renderer _renderer;
    private Animator _animator; // Thêm biến Animator
    private Color _originalColor;
    private bool _isStunned;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponentInChildren<Renderer>(); // Tìm ở cả con (vì Model thường là con)
        _animator = GetComponent<Animator>();
        
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    public HitResult TakeDamage(DamageInfo info)
    {
        if (_isStunned) return HitResult.Ignored;

        // 1. HIỆU ỨNG MÀU
        StartCoroutine(FlashColor(Color.red, 0.2f));

        // 2. TẮT ANIMATOR TẠM THỜI (Để vật lý hoạt động)
        // Nếu không tắt, Animation sẽ cố giữ nhân vật đứng yên tại chỗ
        if (_animator != null) _animator.enabled = false;

        // 3. XỬ LÝ VẬT LÝ
        _rb.linearVelocity = Vector3.zero; // Reset quán tính cũ
        _rb.angularVelocity = Vector3.zero;

        // Đẩy!
        if (info.hitDirection == Vector3.up)
        {
            // Hất tung
            _rb.AddForce(Vector3.up * info.knockbackForce, ForceMode.Impulse);
        }
        else
        {
            // Đẩy lùi (Giữ nguyên Y để nó nảy lên một chút cho đẹp)
            Vector3 forceDir = info.hitDirection + Vector3.up * 0.2f; 
            _rb.AddForce(forceDir.normalized * info.knockbackForce, ForceMode.Impulse);
        }

        // 4. HỒI PHỤC (Sau khi bị đẩy thì đứng dậy)
        // Nếu là Stun thì lâu hơn, nếu đẩy thường thì 0.5s
        float recoverTime = (info.type == DamageType.Stun) ? 2.0f : 0.6f;
        StartCoroutine(RecoverRoutine(recoverTime));

        if (info.type == DamageType.Stun) StartCoroutine(StunEffectRoutine());

        return HitResult.Hit;
    }

    // Coroutine hồi phục trạng thái
    IEnumerator RecoverRoutine(float time)
    {
        yield return new WaitForSeconds(time);

        // Dừng vật lý trôi
        _rb.linearVelocity = Vector3.zero;
        
        // Bật lại Animator để nó đứng dậy/múa tiếp
        if (_animator != null) _animator.enabled = true;
    }

    IEnumerator StunEffectRoutine()
    {
        _isStunned = true;
        if (_renderer) _renderer.material.color = Color.yellow;
        yield return new WaitForSeconds(2.0f);
        if (_renderer) _renderer.material.color = _originalColor;
        _isStunned = false;
    }

    IEnumerator FlashColor(Color color, float time)
    {
        if (_renderer) _renderer.material.color = color;
        yield return new WaitForSeconds(time);
        if (!_isStunned && _renderer) _renderer.material.color = _originalColor;
    }
}