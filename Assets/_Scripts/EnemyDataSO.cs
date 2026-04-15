using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Base Stats")]
    public string enemyName;
    public int maxHealth;

    [Header("Rewards")]
    public int scoreValue;

}