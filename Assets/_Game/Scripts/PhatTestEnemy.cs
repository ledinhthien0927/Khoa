using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PhatTestEnemy : MonoBehaviour, IDamageable
{
    private Rigidbody _rb;
    private Renderer _renderer;
    private Animator _animator;
    private Color _originalColor;
    private bool _isStunned;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponentInChildren<Renderer>();
        _animator = GetComponent<Animator>();
        if (_renderer != null) _originalColor = _renderer.material.color;
        _rb.constraints = RigidbodyConstraints.FreezeRotation; 
    }

    void LateUpdate()
    {
        if (transform.rotation.x != 0 || transform.rotation.z != 0)
        {
            Vector3 currentRot = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, currentRot.y, 0);
        }
    }

    public HitResult TakeDamage(DamageInfo info)
    {
        if (_isStunned) return HitResult.Ignored;

        StartCoroutine(FlashColor(Color.red, 0.2f));

        if (_animator != null) _animator.enabled = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Xử lý Lực đẩy (Hỗ trợ Skill E, R, JumpSmash)
        if (info.hitDirection == Vector3.up)
        {
            _rb.AddForce(Vector3.up * info.knockbackForce, ForceMode.VelocityChange);
        }
        else
        {
            Vector3 finalForce = info.hitDirection + (Vector3.up * 0.2f); 
            _rb.AddForce(finalForce.normalized * info.knockbackForce, ForceMode.VelocityChange);
        }

        // Xử lý Thời gian Stun
        // UltimateR, EarthUp và Stun dùng biến duration riêng
        float recoverTime = (info.type == DamageType.UltimateR || info.type == DamageType.EarthUp || info.type == DamageType.Stun) 
                            ? info.duration 
                            : 0.6f;
        
        StartCoroutine(RecoverRoutine(recoverTime));

        if (info.type == DamageType.Stun || info.type == DamageType.EarthUp || info.type == DamageType.UltimateR) 
            StartCoroutine(StunEffectRoutine(recoverTime));

        return HitResult.Hit;
    }

    IEnumerator RecoverRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        _rb.linearVelocity = Vector3.zero;
        if (_animator != null) _animator.enabled = true;
    }

    IEnumerator StunEffectRoutine(float time)
    {
        _isStunned = true;
        if (_renderer) _renderer.material.color = Color.yellow;
        yield return new WaitForSeconds(time);
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