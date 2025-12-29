using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Cài đặt")]
    [SerializeField] private GameObject loadingScreenPrefab;
    [SerializeField] private string sceneName = "Level1"; // Tên scene muốn load

    public void StartGame()
    {
        // 1. Tạo màn hình loading
        GameObject loaderObj = Instantiate(loadingScreenPrefab);
        
        // 2. Giữ nó lại khi chuyển scene
        DontDestroyOnLoad(loaderObj);

        // 3. Truyền tên Scene cần load vào script của Prefab
        LoadingController controller = loaderObj.GetComponent<LoadingController>();
        
        if (controller != null)
        {
            controller.sceneToLoad = sceneName;
        }
        else
        {
            Debug.LogError("Lỗi: Prefab Loading chưa gắn script 'LoadingController'!");
        }
    }
}