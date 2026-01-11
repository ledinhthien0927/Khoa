using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    [SerializeField] Image musicOnIcon;
    [SerializeField] Image musicOffIcon;
    [SerializeField] AudioSource musicSource;

    private bool muted;

    void Start()
    {
        muted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        Apply();
    }

    public void OnButtonPress()
    {
        muted = !muted;
        PlayerPrefs.SetInt("MusicMuted", muted ? 1 : 0);
        Apply();
    }

    private void Apply()
    {
        musicSource.mute = muted;
        musicOnIcon.enabled = !muted;
        musicOffIcon.enabled = muted;
    }
}
