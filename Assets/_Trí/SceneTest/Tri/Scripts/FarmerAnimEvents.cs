using UnityEngine;

public class FarmerAnimEvents : MonoBehaviour
{
    public FarmerNPC farmer;

    // Animation Event gọi các hàm này
    public void SpawnDirt()
    {
        Debug.Log("SpawnDirt EVENT");
        farmer.PlayDirt();
    }

    public void ScatterSeed()
    {
        Debug.Log("ScatterSeed EVENT");
        farmer.PlaySeed();
    }

    public void PourWater()
    {
        Debug.Log("PourWater EVENT");
        farmer.PlayWater();
    }
}