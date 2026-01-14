using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Global Cache")]
    public Transform PlayerTransform; // Cache vị trí Player

    [Header("Combat Token Settings")]
    public int MaxConcurrentAttackers = 2; // Giới hạn số quái đánh cùng lúc
    private int _currentAttackers = 0;

    private List<EnemyController> _enemies = new List<EnemyController>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Tìm Player 1 lần duy nhất lúc game chạy
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerTransform = player.transform;
            Debug.Log("EnemyManager: Player Cached!");
        }
        else
        {
            Debug.LogError("EnemyManager: Quên gắn Tag 'Player' rồi kìa!");
        }
    }

    // Đăng ký/Hủy đăng ký Enemy
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!_enemies.Contains(enemy)) _enemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyController enemy)
    {
        if (_enemies.Contains(enemy)) _enemies.Remove(enemy);
    }

    // VÒNG LẶP MANUAL UPDATE (Quan trọng)
    void Update()
    {
        if (PlayerTransform == null) return;

        float dt = Time.deltaTime;
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (_enemies[i] != null && _enemies[i].gameObject.activeSelf)
            {
                _enemies[i].ManualUpdate(dt);
            }
        }
    }

    // HỆ THỐNG TOKEN
    public bool RequestAttackToken()
    {
        if (_currentAttackers < MaxConcurrentAttackers)
        {
            _currentAttackers++;
            return true;
        }
        return false;
    }

    public void ReturnAttackToken()
    {
        _currentAttackers--;
        if (_currentAttackers < 0) _currentAttackers = 0;
    }
}