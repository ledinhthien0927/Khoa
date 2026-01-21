using UnityEngine;

public class AncientChest : MonoBehaviour
{
    bool opened = false;

    public void EnableChest()
    {
        gameObject.SetActive(true);
    }

    [System.Obsolete]
    public void Interact()
    {
        if (opened) return;

        opened = true;
        Debug.Log("Chest opened");

        FindObjectOfType<VillageChiefNPC>()?.OnChestOpened();
    }
}
