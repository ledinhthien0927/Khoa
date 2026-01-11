using System.Collections;
using UnityEngine;

public class HammerController : MonoBehaviour
{
    public static HammerController Instance;

    [Header("Settings")]
    public float moveSpeed = 15f;
    public float strikeSpeed = 0.2f;

    [Header("Alignment (Quan trọng)")]
    // Chỉnh cái này để Đầu búa trúng phôi thay vì Cán búa
    // Gợi ý: Y thường phải cao lên, Z hoặc X phải lùi lại
    public Vector3 impactOffset = new Vector3(0, 0.5f, 0); 

    [Header("Animation Angles")]
    public float raiseOffset = -45f; 
    public float hitOffset = 45f;    

    private Vector3 restPosition;       
    private Quaternion restRotation;    
    private bool isStriking = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        restPosition = transform.position;
        restRotation = transform.rotation;
    }

    public void StrikeAt(Vector3 targetPosition)
    {
        StopAllCoroutines();
        StartCoroutine(StrikeRoutine(targetPosition));
    }

    IEnumerator StrikeRoutine(Vector3 targetPos)
    {
        isStriking = true;

        // --- CÔNG THỨC MỚI: Vị trí đích = Vị trí vòng tròn + Độ lệch tùy chỉnh ---
        Vector3 hitPos = targetPos + impactOffset; 
        
        // 1. Bay tới (Move)
        while (Vector3.Distance(transform.position, hitPos) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, hitPos, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, restRotation, Time.deltaTime * 10f);
            yield return null;
        }
        transform.position = hitPos;

        // 2. Tính góc quay
        Quaternion startRot = restRotation;
        
        // LƯU Ý: Nếu búa xoay sai trục, đổi Vector3.right thành Vector3.forward hoặc Vector3.up ở đây
        Quaternion raiseRot = restRotation * Quaternion.AngleAxis(raiseOffset, Vector3.forward);
        Quaternion hitRot = restRotation * Quaternion.AngleAxis(hitOffset, Vector3.forward);

        // 3. Giơ lên
        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / strikeSpeed;
            transform.rotation = Quaternion.Lerp(startRot, raiseRot, t);
            yield return null;
        }

        // 4. Đập xuống (Lúc này đầu búa sẽ chạm phôi nếu impactOffset chỉnh chuẩn)
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / (strikeSpeed * 0.5f);
            transform.rotation = Quaternion.Lerp(raiseRot, hitRot, t);
            yield return null;
        }

        // --- Giữ yên lâu hơn chút để bạn kịp nhìn (0.2s) ---
        yield return new WaitForSeconds(0.2f);

        // 5. Bay về
        while (Vector3.Distance(transform.position, restPosition) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, restPosition, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Lerp(transform.rotation, restRotation, Time.deltaTime * 5f);
            yield return null;
        }
        
        transform.position = restPosition;
        transform.rotation = restRotation;
        
        isStriking = false;
    }
}