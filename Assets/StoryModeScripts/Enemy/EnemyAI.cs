//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System; // Required for Action and Func

//public class EnemyAI : MonoBehaviour
//{
//    [SerializeField] public EnemyAttackData enemyAttack; // Assign this in the Inspector for each enemy prefab
//    private Animator animator;
//    private SpriteRenderer spriteRenderer;

//    private Action<float> updatePlayerHealthCallback;
//    private Func<float> getPlayerHealthCallback;
//    private Func<float> getPlayerMaxHealthCallback;
//    private Action<string> updateBattleStatusCallback;
//    private Action<GameObject, AttackData> playImpactEffectCallback_PlayerAttack; // For player attacks
//    private Action<GameObject, EnemyAttackData> playImpactEffectCallback_EnemyAttack; // For enemy attacks
//    private Func<SpriteRenderer, AttackData, IEnumerator> flashPlayerSpriteCallback_PlayerAttack; // For player flashes

//    // UIManager reference to directly call correct overloads
//    private UIManager uiManagerRef;


//    // Reverted Initialize to match original signature for EnemyAI, plus UIManager ref
//    public void Initialize(Action<float> updatePlayerHealth, Func<float> getPlayerHealth, Func<float> getPlayerMaxHealth,
//                           Action<string> updateStatus, UIManager uiManager) // Now directly takes UIManager
//    {
//        animator = GetComponent<Animator>();
//        spriteRenderer = GetComponent<SpriteRenderer>();

//        if (animator == null)
//        {
//            Debug.LogError($"Enemy {gameObject.name} is missing an Animator component!");
//        }
//        if (spriteRenderer == null)
//        {
//            Debug.LogError($"Enemy {gameObject.name} is missing a SpriteRenderer component!");
//        }

//        updatePlayerHealthCallback = updatePlayerHealth;
//        getPlayerHealthCallback = getPlayerHealth;
//        getPlayerMaxHealthCallback = getPlayerMaxHealth;
//        updateBattleStatusCallback = updateStatus;
//        uiManagerRef = uiManager; // Store the UIManager reference

//        if (animator != null) animator.SetTrigger("Idle"); // Ensure starting in Idle
//    }


//    public IEnumerator PerformAttack(GameObject playerTarget)
//    {
//        if (enemyAttack == null || !gameObject.activeInHierarchy)
//        {
//            yield break;
//        }

//        updateBattleStatusCallback($"{gameObject.name} is attacking!");

//        if (animator != null)
//        {
//            animator.SetTrigger(enemyAttack.animationTrigger);
//        }

//        yield return new WaitForSeconds(enemyAttack.effectDelay);

//        if (UnityEngine.Random.Range(0f, 1f) > enemyAttack.accuracy)
//        {
//            updateBattleStatusCallback($"{gameObject.name}'s attack missed!");
//            if (GameManager.Instance != null)
//                GameManager.Instance.PlaySFX("Audio/SFX/General/Miss"); // Original Miss sound
//            yield return new WaitForSeconds(enemyAttack.attackDuration - enemyAttack.effectDelay);
//            if (animator != null) animator.SetTrigger("Idle");
//            yield break;
//        }

//        float damageDealt = enemyAttack.damage;
//        bool isCrit = UnityEngine.Random.Range(0f, 1f) < enemyAttack.critChance;

//        if (isCrit)
//        {
//            damageDealt *= 2f;
//            if (GameManager.Instance != null)
//                GameManager.Instance.PlayCritSound();
//            updateBattleStatusCallback($"{gameObject.name} scored a critical hit!");
//        }

//        float currentPlayerHealth = getPlayerHealthCallback();
//        float newPlayerHealth = Mathf.Max(0, currentPlayerHealth - damageDealt);
//        updatePlayerHealthCallback(newPlayerHealth);

//        updateBattleStatusCallback($"{gameObject.name} dealt {Mathf.Round(damageDealt)} damage to you!");

//        if (GameManager.Instance != null && (enemyAttack.soundEffectName == "None" || string.IsNullOrEmpty(enemyAttack.soundEffectName)))
//        {
//            GameManager.Instance.PlaySFX("General/HitSound");
//        }
//        else if (GameManager.Instance != null && !string.IsNullOrEmpty(enemyAttack.soundEffectName))
//        {
//            GameManager.Instance.PlaySFX("Audio/SFX/Enemy/" + enemyAttack.soundEffectName);
//        }

//        if (playerTarget != null && playerTarget.GetComponent<SpriteRenderer>() != null)
//        {
//            uiManagerRef.PlayImpactEffect(playerTarget, enemyAttack); // Calls UIManager's EnemyAttackData overload
//            monoBehaviourInstance.StartCoroutine(uiManagerRef.FlashSprite(playerTarget.GetComponent<SpriteRenderer>(), enemyAttack)); // Calls UIManager's EnemyAttackData overload
//        }

//        yield return new WaitForSeconds(enemyAttack.attackDuration - enemyAttack.effectDelay);
//        if (animator != null) animator.SetTrigger("Idle");
//    }

//    // This method is called from StoryBattleManager for death logic
//    public IEnumerator PlayDeathAnimation()
//    {
//        if (animator != null)
//        {
//            animator.SetTrigger("Death");
//            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
//            float deathAnimationLength = 0f;

//            // Wait one frame for the trigger to activate a state
//            yield return null;
//            stateInfo = animator.GetCurrentAnimatorStateInfo(0); // Re-get state info after waiting

//            if (stateInfo.IsName("Death"))
//            {
//                deathAnimationLength = stateInfo.length;
//            }
//            else
//            {
//                foreach (var clip in animator.runtimeAnimatorController.animationClips)
//                {
//                    if (clip.name.Equals("Death"))
//                    {
//                        deathAnimationLength = clip.length;
//                        break;
//                    }
//                }
//            }

//            if (deathAnimationLength <= 0f) deathAnimationLength = 1.0f; // Fallback

//            yield return new WaitForSeconds(deathAnimationLength);
//        }
//        else
//        {
//            yield return new WaitForSeconds(0.5f); // Small delay if no animator
//        }

//        gameObject.SetActive(false); // Deactivate after death animation
//    }

//    // Call this when enemy takes damage
//    public void PlayHitAnimation()
//    {
//        if (animator != null)
//        {
//            animator.SetTrigger("Hit");
//        }
//    }

//    private MonoBehaviour monoBehaviourInstance // Helper to get MonoBehaviour context for Coroutines
//    {
//        get { return FindObjectOfType<StoryBattleManager>(); } // Assumes StoryBattleManager is always in scene
//    }
//}