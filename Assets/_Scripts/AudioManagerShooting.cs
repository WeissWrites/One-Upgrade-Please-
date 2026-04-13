using UnityEngine;

public class AudioManagerShooting : MonoBehaviour
{
    public static AudioManagerShooting Instance;
    public AudioSource audioSource;

    void Awake() { Instance = this; }

    public void PlayFiringSound(AudioClip clip, Vector3 position, float pitch)
    {
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
    }
}
