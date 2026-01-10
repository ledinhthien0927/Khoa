using UnityEngine;

public class AudioManager : MonoBehaviour
{	
	[Header("------- Audio Source ------")]
	[SerializeField] AudioSource musicSoucre;
	[SerializeField] AudioSource SFXSoucre;

	[Header("------- Audio Clip -------")]
	public AudioClip background;
	public AudioClip death;
	public AudioClip checkpoint;
	public AudioClip attack;
	public AudioClip portalIn;
	public AudioClip portalOut;

	[Header("------ UI SFX ------")]
	public AudioClip buttonClickSound;
	public AudioClip uvClickSound;


	public static AudioManager instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);

		}
		else
		{
			Destroy(gameObject);
		}
	
	}


	private void Start()
	{
		if(musicSoucre != null && background != null)
		{
		musicSoucre.clip = background;
		musicSoucre.Play();
		}
	}
	
	/// --------Play SFX chung -------
	public void PlaySFX(AudioClip clip)
	{
		if (clip == null || SFXSoucre == null) return;
		SFXSoucre.PlayOneShot(clip);
	}

	// ------ UI Button -----
	public void Play_Button_Click()
	{
		PlaySFX(buttonClickSound);
	}

	public void Play_UV_Click()
	{
		PlaySFX(uvClickSound);
	}
}
