using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    // Kéo cái nút Start của bạn vào ô này
    [SerializeField] private GameObject startButton; 
    
    [Header("Loading Settings")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private string sceneName = "Level1"; // Tên scene muốn chuyển tới

    public void StartGame()
    {
        // 1. Ẩn nút Start đi ngay lập tức
        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        // 2. Tạo màn hình Loading
        GameObject loaderObj = Instantiate(loadingScreenPrefab);
        
        // Giữ Loading Screen sống qua scene mới
        DontDestroyOnLoad(loaderObj);

        // 3. Truyền tên Scene cần load vào script của Prefab
        LoadingController controller = loaderObj.GetComponent<LoadingController>();
        if (controller != null)
        {
            controller.sceneToLoad = sceneName;
        }
    }
}