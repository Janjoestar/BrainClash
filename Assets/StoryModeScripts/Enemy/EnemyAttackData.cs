//using UnityEngine;

//[CreateAssetMenu(fileName = "NewEnemyAttack", menuName = "Battle/Enemy Attack Data")]
//public class EnemyAttackData : ScriptableObject
//{
//    public string attackName = "Basic Attack";
//    public float damage = 10f;
//    [Range(0f, 1f)]
//    public float accuracy = 0.9f;
//    [Range(0f, 1f)]
//    public float critChance = 0.1f;
//    public string animationTrigger = "Attack1"; // E.g., "Attack1", "Bite", "Claw"
//    public string soundEffectName = "EnemyAttackSound"; // Path under Resources/Audio/SFX/Enemy/
//    public string hitEffectPrefabName = "DefaultEnemyHit"; // Path under Resources/Effects/
//    public Vector3 effectOffset = Vector3.zero;
//    public float effectDelay = 0.2f; // Delay before hit effect appears after animation trigger
//    public Color flashColor = Color.red;
//    public float flashInterval = 0.1f;
//    public float attackDuration = 1.0f; // Expected duration of the attack animation
//}