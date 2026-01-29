using UnityEngine;

public class BoatWakeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ParticleSystem wakeParticles;
    [SerializeField] private float minSpeedToEmit = 0.5f; // Tốc độ tối thiểu để tạo sóng

    private Vector3 _lastPosition;
    private float _currentSpeed;

    void Start()
    {
        if (wakeParticles == null) 
            wakeParticles = GetComponent<ParticleSystem>();

        _lastPosition = transform.position;
    }

    void Update()
    {
        CalculateSpeed();
        HandleWakeEmission();
    }

    void CalculateSpeed()
    {
        // Tính tốc độ dựa trên quãng đường di chuyển được trong 1 frame
        float distance = Vector3.Distance(transform.position, _lastPosition);
        _currentSpeed = distance / Time.deltaTime;
        
        _lastPosition = transform.position;
    }

    void HandleWakeEmission()
    {
        if (wakeParticles == null) return;

        var emission = wakeParticles.emission;

        // Nếu tốc độ tàu lớn hơn mức tối thiểu -> Bật emission
        if (_currentSpeed > minSpeedToEmit)
        {
            emission.enabled = true;
        }
        else
        {
            emission.enabled = false;
        }
    }
}   