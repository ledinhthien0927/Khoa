using UnityEngine;
using UnityEngine.UI; // [QUAN TRỌNG] Cần dòng này để dùng Slider

namespace _Khoa.Khoa
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        private float currentHealth;

        [Header("UI")]
        public Slider healthSlider; // [MỚI] Kéo thanh Slider vào ô này trong Unity

        [Header("State")]
        public bool isDead = false;
        public bool isInvulnerable = false;

        private Animator anim;

        void Start()
        {
            currentHealth = maxHealth;
            anim = GetComponent<Animator>();

            // [MỚI] Cài đặt giá trị ban đầu cho thanh máu
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        public HitResult TakeDamage(DamageInfo info)
        {
            if (isDead) return HitResult.Ignored;

            if (isInvulnerable && info.type != DamageType.UltimateR)
            {
                return HitResult.Ignored;
            }

            currentHealth -= info.amount;

            // [MỚI] Cập nhật thanh máu ngay khi bị đánh
            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            Debug.Log($"Enemy bị đánh bởi {info.attacker.name}. Mất {info.amount} HP. Còn lại: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die(info);
                return HitResult.Hit;
            }

            if (info.type == DamageType.Heavy || info.type == DamageType.EarthUp)
            {
                if (anim != null) anim.SetTrigger("Knockback");
            }
            else
            {
                if (anim != null) anim.SetTrigger("Hurt");
            }

            ApplyKnockback(info);

            return HitResult.Hit;
        }

        private void ApplyKnockback(DamageInfo info)
        {
            if (info.knockbackForce > 0)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.AddForce(info.hitDirection * info.knockbackForce, ForceMode.Impulse);
                }
            }
        }

        private void Die(DamageInfo finalHit)
        {
            isDead = true;
            Debug.Log("Enemy đã bị tiêu diệt!");

            if (anim != null) anim.SetTrigger("Die");
            
            // [MỚI] Ẩn thanh máu đi cho đẹp
            if (healthSlider != null) healthSlider.gameObject.SetActive(false);

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, 3f);
        }
    }
}