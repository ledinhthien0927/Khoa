using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameIntroManager : MonoBehaviour
{
    [Header("--- Setup References ---")]
    [Tooltip("Kéo script PlayerController của nhân vật vào đây")]
    public PlayerController playerController;
    
    [Tooltip("Kéo Animator của nhân vật vào đây (Để ép nằm xuống lúc đầu)")]
    public Animator playerAnimator;

    [Tooltip("Kéo GameObject chứa ThirdPersonCamera vào đây")]
    public GameObject tppCameraObject; 
    
    [Tooltip("Kéo Camera góc nhìn thứ nhất (Gắn ở xương đầu) vào đây")]
    public GameObject fppCameraObject; 
    
    [Tooltip("Kéo ảnh UI màu đen (Fade Screen) vào đây")]
    public Image blackScreenUI;

    [Header("--- Post Processing (Blur Effect) ---")]
    public Volume postProcessVolume;
    private DepthOfField dofComponent; 

    [Header("--- Audio Setup (Âm thanh) ---")]
    public AudioSource audioSource;
    public AudioClip introDialogueClip;

    [Header("--- Settings (Thời gian & Hiệu ứng) ---")]
    public float eyeOpenDuration = 3.0f;  
    public float standUpDelay = 1.0f;     
    public float standUpAnimTime = 4.0f;  
    public float dialogueDuration = 3.0f; 
    
    [Tooltip("Góc xoay tối đa khi nhân vật nhìn sang hai bên (Độ)")]
    public float lookAroundAngle = 25f; 

    void Start()
    {
        // =========================================================
        // 1. KIỂM TRA FILE SAVE
        // =========================================================
        string saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(saveFilePath))
        {
            // Nếu có save, bỏ qua Intro hoàn toàn
            SkipIntro();
            return;
        }

        // =========================================================
        // 2. SETUP NEW GAME (BẮT ĐẦU BẰNG FPP)
        // =========================================================
        
        // Khóa điều khiển
        if (playerController) playerController.enabled = false;

        // Ẩn toàn bộ UI chiến đấu/thanh máu
        if (playerController != null)
        {
            PlayerView view = playerController.GetView();
            if (view != null) view.ToggleCombatUI(false); 
        }

        // Ẩn UI Nhiệm vụ (nếu có)
        if (QuestUIManager.Instance != null)
            QuestUIManager.Instance.SetQuestUIVisible(false);

        // Bật FPP Camera, tắt TPP Camera
        if (tppCameraObject) tppCameraObject.SetActive(false);
        if (fppCameraObject) fppCameraObject.SetActive(true);

        // Kéo UI đen lên đè mọi thứ và tô đen kịt
        if (blackScreenUI)
        {
            blackScreenUI.transform.SetAsLastSibling(); 
            Color c = blackScreenUI.color;
            c.a = 1f; 
            blackScreenUI.color = c;
            blackScreenUI.gameObject.SetActive(true);
        }

        // Bật hiệu ứng mờ mắt
        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out DepthOfField tmpDof))
        {
            dofComponent = tmpDof;
            dofComponent.active = true;
            dofComponent.mode.value = DepthOfFieldMode.Gaussian;
            dofComponent.gaussianStart.value = 0f;
            dofComponent.gaussianEnd.value = 0.5f; 
        }

        // Bắt đầu đạo diễn kịch bản
        StartCoroutine(IntroSequenceRoutine());
    }

    // --- HÀM HỖ TRỢ: BỎ QUA INTRO KHI CÓ FILE SAVE ---
    void SkipIntro()
    {
        if (tppCameraObject) tppCameraObject.SetActive(true);
        if (fppCameraObject) fppCameraObject.SetActive(false);
        if (blackScreenUI) blackScreenUI.gameObject.SetActive(false);
        
        if (playerController) 
        {
            playerController.enabled = true;
            PlayerView view = playerController.GetView();
            if (view != null) view.ToggleCombatUI(true);
        }

        if (QuestUIManager.Instance != null) QuestUIManager.Instance.SetQuestUIVisible(true);
        
        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out DepthOfField tmpDof))
        {
            tmpDof.active = false;
        }

        Destroy(this.gameObject);
    }

    // --- COROUTINE: ĐẠO DIỄN KỊCH BẢN CHÍNH ---
    IEnumerator IntroSequenceRoutine()
    {
        // --------------------------------------------------------
        // BÍ QUYẾT FIX LỖI ANIMATOR: CHỜ 0.1 GIÂY
        // --------------------------------------------------------
        // Chờ các script khác chạy xong việc ép nhân vật về dáng đứng
        yield return new WaitForSeconds(0.1f);

        // SAU ĐÓ mới vung búa ép nhân vật nằm sấp xuống (Chắc chắn thành công 100%)
        if (playerAnimator)
        {
            // LƯU Ý: Phải viết đúng tên State "LyingIdle" như trong Animator
            playerAnimator.Play("LyingIdle", 0, 0f); 
            playerAnimator.Update(0f); // Ép cập nhật frame ngay lập tức
        }

        // Chờ thêm 0.9 giây nữa cho đủ 1 giây bóng tối ban đầu
        yield return new WaitForSeconds(0.9f);

        // --------------------------------------------------------
        // GIAI ĐOẠN 1: MỞ MẮT (Fade In + Giảm Blur)
        // --------------------------------------------------------
        float timer = 0;
        Color startColor = blackScreenUI.color;
        float startBlurEndVal = dofComponent != null ? dofComponent.gaussianEnd.value : 0.5f;
        float targetBlurEndVal = 50f;

        while (timer < eyeOpenDuration)
        {
            timer += Time.deltaTime;
            float blend = timer / eyeOpenDuration;
            float smoothBlend = Mathf.SmoothStep(0f, 1f, blend);

            if (blackScreenUI)
            {
                startColor.a = Mathf.Lerp(1f, 0f, smoothBlend);
                blackScreenUI.color = startColor;
            }

            if (dofComponent != null)
                dofComponent.gaussianEnd.value = Mathf.Lerp(startBlurEndVal, targetBlurEndVal, smoothBlend);

            yield return null;
        }
        
        if (blackScreenUI) blackScreenUI.gameObject.SetActive(false);

        // --------------------------------------------------------
        // GIAI ĐOẠN 2: ĐỨNG DẬY
        // --------------------------------------------------------
        yield return new WaitForSeconds(standUpDelay);
        
        if (playerController)
        {
            PlayerView view = playerController.GetView();
            if (view != null) view.TriggerWakeUp();
            yield return new WaitForSeconds(standUpAnimTime); 
        }

        // --------------------------------------------------------
        // GIAI ĐOẠN 3: THOẠI & NHÌN XUNG QUANH LẮC CAMERA
        // --------------------------------------------------------
        Debug.Log("Nhân vật: 'Ư... nhức đầu quá... Mình đang dạt vào hòn đảo quái quỷ nào đây?'");
        
        // Phát tiếng thoại nếu có
        if (audioSource != null && introDialogueClip != null)
        {
            audioSource.PlayOneShot(introDialogueClip);
        }

        float totalWaitTime = (introDialogueClip != null) ? introDialogueClip.length : dialogueDuration;
        float dialogueTimer = 0;

        Quaternion initialCamRot = fppCameraObject != null ? fppCameraObject.transform.localRotation : Quaternion.identity;

        // Vòng lặp xoay camera trái - phải nhìn quanh
        while (dialogueTimer < totalWaitTime)
        {
            dialogueTimer += Time.deltaTime;
            
            if (fppCameraObject != null)
            {
                float angleY = Mathf.Sin((dialogueTimer / totalWaitTime) * Mathf.PI * 2f) * lookAroundAngle;
                fppCameraObject.transform.localRotation = initialCamRot * Quaternion.Euler(0, angleY, 0);
            }
            
            yield return null;
        }

        // Trả camera về thẳng phía trước
        if (fppCameraObject != null)
        {
            fppCameraObject.transform.localRotation = initialCamRot;
        }

        // --------------------------------------------------------
        // GIAI ĐOẠN 4: CHỚP MẮT (TRÁO CAMERA CỰC MƯỢT)
        // --------------------------------------------------------
        
        // 4.1. Nhắm mắt (Đen rụp)
        if (blackScreenUI)
        {
            blackScreenUI.gameObject.SetActive(true);
            float t = 0;
            while (t < 0.25f) 
            {
                t += Time.deltaTime;
                Color c = blackScreenUI.color;
                c.a = Mathf.Lerp(0f, 1f, t / 0.25f);
                blackScreenUI.color = c;
                yield return null;
            }
        }

        // 4.2. Tráo Camera trong bóng tối
        if (fppCameraObject) fppCameraObject.SetActive(false);
        if (tppCameraObject) tppCameraObject.SetActive(true);
        if (dofComponent != null) dofComponent.active = false; 

        // 4.3. Mở mắt ra (Sáng dần lên)
        if (blackScreenUI)
        {
            float t = 0;
            while (t < 0.5f) 
            {
                t += Time.deltaTime;
                Color c = blackScreenUI.color;
                c.a = Mathf.Lerp(1f, 0f, t / 0.5f);
                blackScreenUI.color = c;
                yield return null;
            }
            blackScreenUI.gameObject.SetActive(false);
        }

        // --------------------------------------------------------
        // GIAI ĐOẠN 5: TRẢ LẠI ĐIỀU KHIỂN & BẬT UI
        // --------------------------------------------------------
        if (playerController) 
        {
            playerController.enabled = true;
            PlayerView view = playerController.GetView();
            if (view != null) view.ToggleCombatUI(true); 
        }

        if (QuestUIManager.Instance != null)
            QuestUIManager.Instance.SetQuestUIVisible(true);

        Destroy(this.gameObject);
    }
}