using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Data Source")]
    public EnemyDataSO data;
    private int currentHealth;
    private Animator anim;
    private bool isDead = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (data != null)
        {
            currentHealth = data.maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            int randomIndex = Random.Range(0, 4);
            anim.SetInteger("HitIndex", randomIndex);
            anim.SetTrigger("Hit");
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        anim.SetTrigger("Die");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in allColliders) c.enabled = false;

        StartCoroutine(CleanupCorpse());
    }

    IEnumerator CleanupCorpse()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }
}