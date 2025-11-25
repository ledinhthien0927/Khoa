using UnityEngine;

[System.Serializable] // Để hiện dữ liệu trong Inspector cho dễ debug
public class GameModel
{
    [Header("--- Cấu Hình ---")]
    public float thoiGianGiuaCacDot = 600f; // 10 phút
    public int danSoToiDa = 4;

    [Header("--- Dữ Liệu Runtime ---")]
    public float thoiGianDemNguoc;
    public int soDotDaVao = 0;
    public bool dangTrongTranChien = false;
    public int danSoHienTai = 0;
}