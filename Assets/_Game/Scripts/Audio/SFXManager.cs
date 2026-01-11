using UnityEngine;
using UnityEngine.UI;

public class SFXManager : MonoBehaviour
{
    [SerializeField] Image SFXOnIcon;
    [SerializeField] Image SFXOffIcon;
    [SerializeField] AudioSource SFXSource;

    private bool muted;

    void Start()
    {
        muted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        Apply();
    }

    public void OnButtonPress()
    {
        muted = !muted;
        PlayerPrefs.SetInt("SFXMuted", muted ? 1 : 0);
        Apply();
    }

    void Apply()
    {
        SFXSource.mute = muted;
        SFXOnIcon.enabled = !muted;
        SFXOffIcon.enabled = muted;
    }
}
