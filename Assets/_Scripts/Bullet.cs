using UnityEngine;

public class Bullet : MonoBehaviour
{
    void OnTriggerEnter(Collider objectWeHit)
    {
        Destroy(gameObject);
    }
    public class SelfDestruct : MonoBehaviour
    {
        void Start() { Destroy(gameObject, 5f); }
    }
}
