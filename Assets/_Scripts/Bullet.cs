using UnityEngine;

public class Bullet : MonoBehaviour
{
    void OnTriggerEnter(Collider objectWeHit)
    {
        Destroy(gameObject);
    }
}
