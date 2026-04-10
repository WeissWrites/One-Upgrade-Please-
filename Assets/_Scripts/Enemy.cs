using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 20;

    public void TakeDamage(int amount)
    {
        // Deal Damage
        health -= amount;
        // Kill opponent
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}