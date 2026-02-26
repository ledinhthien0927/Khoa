using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoatRepairStation : MonoBehaviour
{
    [Header("Requirements - Yêu cầu")]
    public int requiredWood = 3; 
    public int requiredMetal = 2;

    [Header("Boat Objects - Biến hình")]
    public GameObject brokenBoat; 
    public GameObject fixedBoat;  

    [Header("Cinematic Settings")]
    public float repairTime = 4.0f;  
    [Tooltip("Kéo thả 3 điểm Transform trên thuyền vào đây để nhân vật dịch chuyển tới")]
    public List<Transform> repairPoints; 
    public GameObject hammerSmokeVFX; 

    [Header("Quest Items (Gỗ, Sắt trên Scene)")]
    public List<GameObject> questItems;

    private bool isPlayerNearby;
    private PlayerController playerRef;
    private bool isRepaired = false;

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        if (QuestManager.Instance != null)
        {
            if ((int)QuestManager.Instance.princeState >= (int)PrinceQuestState.ShipDone)
            {
                isRepaired = true;
                if (brokenBoat) brokenBoat.SetActive(false);
                if (fixedBoat) fixedBoat.SetActive(true);
                ToggleQuestItems(false);
                this.enabled = false; 
            }
            else
            {
                if (brokenBoat) brokenBoat.SetActive(true);
                if (fixedBoat) fixedBoat.SetActive(false);

                bool isDoingQuest = (QuestManager.Instance.princeState == PrinceQuestState.ShipQuest || 
                                     QuestManager.Instance.princeState == PrinceQuestState.ShipDoing);
                ToggleQuestItems(isDoingQuest);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerRef = other.GetComponent<PlayerController>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerRef = null;
        }
    }

    void Update()
    {
        if (isPlayerNearby && !isRepaired && Input.GetKeyDown(KeyCode.F))
        {
            if (QuestManager.Instance.princeState != PrinceQuestState.ShipQuest && 
                QuestManager.Instance.princeState != PrinceQuestState.ShipDoing)
            {
                Debug.Log("Chưa nhận nhiệm vụ sửa thuyền!");
                return;
            }
            TryRepair();
        }
    }

    void TryRepair()
    {
        if (playerRef == null) return;

        if (playerRef.model.woodCount >= requiredWood && playerRef.model.metalCount >= requiredMetal)
        {
            StartCoroutine(ProcessRepair());
        }
        else
        {
            Debug.Log("Thiếu nguyên liệu! Cần đi nhặt thêm.");
        }
    }

    IEnumerator ProcessRepair()
    {
        isRepaired = true;

        // --- 1. CHUẨN BỊ ---
        // Sử dụng hàm SetTravelMode của bạn để khóa điều khiển, tắt CharacterController và ẩn UI Combat
        playerRef.SetTravelMode(true); 
        
        // Bắt đầu cầm búa và chạy Anim
        playerRef.GetView().EquipHammer(true);
        playerRef.GetView().TriggerRepair();

        // Chuẩn bị thuyền lành (Tàng hình)
        fixedBoat.SetActive(true);
        SetBoatAlpha(fixedBoat, 0f);
        SetBoatAlpha(brokenBoat, 1f);

        // --- 2. DỊCH CHUYỂN QUA CÁC ĐIỂM (50% thời gian đầu) ---
        if (repairPoints.Count >= 3)
        {
            float phaseOneTime = repairTime * 0.5f;
            float timePerPoint = phaseOneTime / 2f; 

            // Điểm 1 (Bắt đầu)
            TeleportAndVFX(repairPoints[0]);
            yield return new WaitForSeconds(timePerPoint);

            // Điểm 2 (Mốc 25%)
            TeleportAndVFX(repairPoints[1]);
            yield return new WaitForSeconds(timePerPoint);

            // Điểm 3 (Mốc 50%) - Chuẩn bị Fade thuyền
            TeleportAndVFX(repairPoints[2]);
        }
        else
        {
            Debug.LogWarning("Vui lòng gắn đủ 3 điểm repairPoints!");
        }

        // --- 3. FADE THUYỀN (50% thời gian còn lại) ---
        float fadeDuration = repairTime * 0.5f; 
        float elapsed = 0f;

        // Ép 2 chiếc thuyền sang chế độ Transparent bằng code trước khi bắt đầu mờ đi
        SetupMaterialBlendMode(brokenBoat, true);
        SetupMaterialBlendMode(fixedBoat, true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration; // t chạy từ 0 -> 1

            SetBoatAlpha(brokenBoat, 1f - t); // Thuyền hỏng mờ dần
            SetBoatAlpha(fixedBoat, t);       // Thuyền lành rõ dần

            yield return null; 
        }

        // --- 4. KẾT THÚC ---
        SetBoatAlpha(brokenBoat, 0f);
        SetBoatAlpha(fixedBoat, 1f);
        brokenBoat.SetActive(false); 

        // [QUAN TRỌNG NHẤT] Trả chiếc thuyền lành về lại trạng thái Opaque để hiển thị chuẩn khối và đổ bóng nét
        SetupMaterialBlendMode(fixedBoat, false);

        // Bỏ búa, mở khóa người chơi
        playerRef.GetView().EquipHammer(false);
        playerRef.SetTravelMode(false); // Trả lại quyền điều khiển và UI

        // Cập nhật Quest
        if (QuestManager.Instance) QuestManager.Instance.SetPrince(PrinceQuestState.ShipDone);
        if (QuestUIManager.Instance) QuestUIManager.Instance.AutoClickMainQuest();
        
        ToggleQuestItems(false);
        this.enabled = false;
    }

    private void TeleportAndVFX(Transform targetTransform)
    {
        playerRef.transform.position = targetTransform.position;
        playerRef.transform.rotation = targetTransform.rotation;

        if (hammerSmokeVFX)
        {
            // Cộng thêm Vector3.up để khói bay ngang tầm tay/búa thay vì ở dưới chân
            Vector3 vfxPos = targetTransform.position + Vector3.up * 1.2f; 
            Destroy(Instantiate(hammerSmokeVFX, vfxPos, Quaternion.identity), 2f);
        }
    }

    private void SetBoatAlpha(GameObject boatObj, float alpha)
    {
        Renderer[] renderers = boatObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                if (mat.HasProperty("_Color")) 
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor")) 
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    public void ToggleQuestItems(bool isActive)
    {
        foreach (GameObject item in questItems)
        {
            if (item != null) item.SetActive(isActive);
        }
    }
    private void SetupMaterialBlendMode(GameObject boatObj, bool isTransparent)
    {
        Renderer[] renderers = boatObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            foreach (Material mat in ren.materials)
            {
                if (isTransparent)
                {
                    // Chuyển sang Transparent (Fade)
                    if (mat.HasProperty("_Mode")) // Standard Shader
                    {
                        mat.SetFloat("_Mode", 2); // 2 là Fade
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                    else if (mat.HasProperty("_Surface")) // URP Shader
                    {
                        mat.SetFloat("_Surface", 1); // 1 là Transparent
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                }
                else
                {
                    // Trả về Opaque (Đặc)
                    if (mat.HasProperty("_Mode")) 
                    {
                        mat.SetFloat("_Mode", 0); // 0 là Opaque
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.DisableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = -1;
                    }
                    else if (mat.HasProperty("_Surface")) 
                    {
                        mat.SetFloat("_Surface", 0);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    }
                }
            }
        }
    }
}