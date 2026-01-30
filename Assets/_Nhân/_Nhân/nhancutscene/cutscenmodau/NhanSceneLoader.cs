using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc để quản lý Scene

public class NhanSceneLoader : MonoBehaviour
{
    public string tenSceneTiepTheo = "Level1"; // Điền tên Scene vào đây

    // Hàm này sẽ được Timeline gọi
    public void LoadScene()
    {
        SceneManager.LoadScene(tenSceneTiepTheo);
    }
}