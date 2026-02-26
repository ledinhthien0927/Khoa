using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement; 

[System.Serializable]
public class GameData
{
    // --- CƠ BẢN ---
    public Vector3 position;
    public Quaternion rotation;

    // --- CHỈ SỐ ---
    public float currentHealth;
    public float currentStamina;

    // --- TÀI NGUYÊN ---
    public int currentArrows;
    public int woodCount;
    public int metalCount;

    // --- VŨ KHÍ & ANIMATION ---
    public bool hasSword;
    public bool hasBow;
    public int currentWeaponIndex; 
    public int animatorState;      

    // --- TIẾN ĐỘ NHIỆM VỤ ---
    public int princeQuestState;
    public int villageQuestState;
}

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance;

    [Header("References")]
    public PlayerController player; 

    private string _saveFilePath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    void Start()
    {
        // Tự động Load khi vào game nếu có file save
        if (File.Exists(_saveFilePath))
        {
            LoadGame();
        }
        else
        {
            // NẾU CHƯA CÓ SAVE (New Game / F4 Reset)
            // Lột sạch vũ khí để khớp với đoạn Intro vừa tỉnh dậy
            if (player != null)
            {
                player.model.hasSword = false;
                player.model.hasBow = false;
                player.model.currentWeapon = (WeaponType)0;

                PlayerView view = player.GetView();
                if (view != null)
                {
                    view.UpdateWeaponVisuals(false, false);
                    view.RestoreVisualState(0); 
                }
            }
        }
    }

    void Update()
    {
        // F5: Lưu Game
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();

        // F4: Xóa Save & Reset Game
        if (Input.GetKeyDown(KeyCode.F4)) DeleteSaveFile();
    }

    // ================= LOGIC LƯU GAME =================
    public void SaveGame()
    {
        if (player == null) 
        {
            Debug.LogWarning("Chưa gán Player vào GameSaveManager!");
            return;
        }

        GameData data = new GameData();

        // 1. Lưu Player
        data.position = player.transform.position;
        data.rotation = player.transform.rotation;
        data.currentHealth = player.model.currentHealth;
        data.currentStamina = player.model.currentStamina;
        data.currentArrows = player.model.currentArrows;
        data.woodCount = player.model.woodCount;
        data.metalCount = player.model.metalCount;

        data.hasSword = player.model.hasSword;
        data.hasBow = player.model.hasBow;
        data.currentWeaponIndex = (int)player.model.currentWeapon;

        // Lưu trạng thái hình ảnh (cầm vũ khí gì)
        if (player.GetView() != null)
        {
            data.animatorState = player.GetView().GetCurrentVisualState();
        }

        // 2. Lưu Quest (Check null để tránh lỗi nếu chưa có hệ thống Quest)
        if (QuestManager.Instance != null)
        {
            data.princeQuestState = (int)QuestManager.Instance.princeState;
            data.villageQuestState = (int)QuestManager.Instance.villageState;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_saveFilePath, json);
        
        Debug.Log("<color=green>Đã lưu Game thành công!</color>");
    }

    // ================= LOGIC LOAD GAME =================
    public void LoadGame()
    {
        if (!File.Exists(_saveFilePath)) return;

        try 
        {
            string json = File.ReadAllText(_saveFilePath);
            GameData data = JsonUtility.FromJson<GameData>(json);

            ApplyDataToPlayer(data);
            Debug.Log("<color=cyan>Đã load Game thành công!</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi Load Game: " + e.Message);
        }
    }

    void ApplyDataToPlayer(GameData data)
    {
        if (player == null) return;

        // 1. Khôi phục Quest
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.princeState = (PrinceQuestState)data.princeQuestState;
            QuestManager.Instance.villageState = (VillageQuestState)data.villageQuestState;
            if (QuestUIManager.Instance != null) QuestUIManager.Instance.UpdateQuestUI();
        }

        // 2. Dịch chuyển nhân vật (Phải tắt CharacterController trước khi dịch chuyển)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        
        player.transform.position = data.position;
        player.transform.rotation = data.rotation;
        
        if (cc) cc.enabled = true;

        // 3. Khôi phục chỉ số
        player.model.currentHealth = data.currentHealth;
        player.model.currentStamina = data.currentStamina;
        player.model.currentArrows = data.currentArrows;
        player.model.woodCount = data.woodCount;
        player.model.metalCount = data.metalCount;

        // 4. Khôi phục Vũ khí & Hình ảnh
        player.model.hasSword = data.hasSword;
        player.model.hasBow = data.hasBow;
        player.model.currentWeapon = (WeaponType)data.currentWeaponIndex;

        PlayerView view = player.GetView();
        if (view != null)
        {
            view.UpdateWeaponVisuals(player.model.hasSword, player.model.hasBow);
            
            // Ép model vũ khí hiện ra tay nếu đang chọn
            if (player.model.currentWeapon == WeaponType.Sword && player.model.hasSword)
                view.SwitchWeaponVisuals(WeaponType.Sword);
            else if (player.model.currentWeapon == WeaponType.Bow && player.model.hasBow)
                view.SwitchWeaponVisuals(WeaponType.Bow);

            // Ép Animator về đúng dáng
            view.RestoreVisualState(data.animatorState);
            
            // Cập nhật lại thanh máu, thể lực
            view.UpdateStatsUI(
                player.model.currentHealth, player.model.maxHealth,
                player.model.currentStamina, player.model.maxStamina,
                player.model.currentArrows
            );
        }
    }
    
    // ================= LOGIC XÓA DATA (F4) =================
    public void DeleteSaveFile()
    {
        // 1. Xóa file vật lý
        if (File.Exists(_saveFilePath))
        {
            File.Delete(_saveFilePath);
            Debug.Log("<color=red>ĐÃ XÓA FILE SAVE!</color>");
        }

        // 2. Xóa dữ liệu tạm (nếu có dùng)
        PlayerPrefs.DeleteAll();

        // 3. Xóa cờ Quest hiện tại trên RAM
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.princeState = 0;
            QuestManager.Instance.villageState = 0;
        }

        // 4. Tải lại màn chơi từ đầu
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}