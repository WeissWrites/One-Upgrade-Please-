using UnityEngine;

public class Casing : MonoBehaviour
{
    public AudioClip hitFloorSound;
    private AudioSource source;

    void Awake() { source = GetComponent<AudioSource>(); }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Terrain"))
        {
            source.PlayOneShot(hitFloorSound);
            Destroy(gameObject, 2.5f);
        }
    }
}
