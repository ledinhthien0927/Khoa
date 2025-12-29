using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingController : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text progressText;
    public Slider progressSlider;

    [Header("Settings")]
    public float fakeDuration = 3.0f;
    
    // Biến này sẽ được truyền từ bên ngoài vào
    [HideInInspector] public string sceneToLoad; 

    private void Start()
    {
        // Khi Prefab vừa sinh ra là chạy ngay
        StartCoroutine(LoadSceneProcess());
    }

    IEnumerator LoadSceneProcess()
    {
        // Đảm bảo game không bị pause
        Time.timeScale = 1.0f;
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fakeDuration);

            // Cập nhật UI
            if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100) + "%";
            if (progressSlider != null) progressSlider.value = progress;

            // Logic 99% chuyển cảnh
            if (progress >= 0.99f && operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // QUAN TRỌNG: Tự hủy chính GameObject này sau khi load xong
        Destroy(gameObject);
    }
}