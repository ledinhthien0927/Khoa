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
		musicSoucre.clip = background;
		musicSoucre.Play();

	}
	
	public void PlaySFX(AudioClip clip)
	{
		SFXSoucre.PlayOneShot(clip);
	}
}
