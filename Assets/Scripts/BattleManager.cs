using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


//CHANGE ATTACKTRIGGERS IN ATTACKDATA FOR EACH CHARACTER;

public class BattleManager : MonoBehaviour
{
    [SerializeField] public GameObject Player1;
    [SerializeField] public GameObject Player2;

    [SerializeField] private Text player1HealthText;
    [SerializeField] private Text player2HealthText;
    [SerializeField] private Text battleStatusText;

    [SerializeField] private GameObject attackPanel;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private Transform foregroundPosition;
    [SerializeField] private Transform backgroundPosition;

    private List<AttackData> player1Attacks = new List<AttackData>();
    private List<AttackData> player2Attacks = new List<AttackData>();
    private int attackingPlayer = 1;

    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private GameObject projectileEffectPrefab;
    [SerializeField] private GameObject magicEffectPrefab;
    [SerializeField] private GameObject areaEffectPrefab;
    [SerializeField] private GameObject directHitEffectPrefab;

    private void OnEnable() => GameManager.OnGameManagerReady += InitializeBattle;
    private void OnDisable() => GameManager.OnGameManagerReady -= InitializeBattle;

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.SelectedCharacterP1 != null)
        {
            InitializeBattle();
        }
    }

    private void InitializeBattle()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance is null in BattleManager.");
            return;
        }

        Character p1Character = GameManager.Instance.SelectedCharacterP1;
        Character p2Character = GameManager.Instance.SelectedCharacterP2;

        SetCharacter(Player1, p1Character);
        SetCharacter(Player2, p2Character);

        // Get the player who correctly answered the quiz
        attackingPlayer = GameManager.Instance.GetLastCorrectPlayer();

        SwapPositions(attackingPlayer);
        UpdateHealthDisplays();
        battleStatusText.text = "Player " + attackingPlayer + "'s turn to attack!";

        InitializeExampleAttacks();
        ShowAttackOptions();
    }


    private void SetCharacter(GameObject playerObject, Character character)
    {
        if (character == null) return;

        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = character.characterSprite;

        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            string overridePath = "Animations/" + character.characterName + "Override";
            AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);
            if (overrideController != null)
                animator.runtimeAnimatorController = overrideController;
            else
                Debug.LogWarning("No Animator Override Controller found for " + character.characterName);
        }
    }

    private void SwapPositions(int attackingPlayer)
    {
        Vector3 forwardPos = foregroundPosition.position;
        Vector3 backPos = backgroundPosition.position;

        // Only change their positions, not their identities
        if (attackingPlayer == 1)
        {
            Player1.transform.position = forwardPos;
            Player2.transform.position = backPos;
            FlipSprites(Player1, true);
            FlipSprites(Player2, false);
        }
        else
        {
            Player2.transform.position = forwardPos;
            Player1.transform.position = backPos;
            FlipSprites(Player1, false);
            FlipSprites(Player2, true);
        }
    }

    private void FlipSprites(GameObject player, bool faceRight)
    {
        SpriteRenderer sprite = player.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.flipX = faceRight;
    }

    private void UpdateHealthDisplays()
    {
        // Get health values
        int p1Health = GameManager.Instance.GetPlayerHealth(1);
        int p2Health = GameManager.Instance.GetPlayerHealth(2);

        // Show health with context of who's attacking/defending
        if (attackingPlayer == 1)
        {
            player1HealthText.text = "Attacker (P1) HP: " + p1Health;
            player2HealthText.text = "Defender (P2) HP: " + p2Health;
        }
        else
        {
            player1HealthText.text = "Attacker (P2) HP: " + p2Health;
            player2HealthText.text = "Defender (P1) HP: " + p1Health;
        }
    }


    private void PerformAttack(AttackData attack)
    {
        // Disable attack buttons during animation to prevent multiple clicks
        SetAttackButtonsInteractable(false);

        int targetPlayer = (attackingPlayer == 1) ? 2 : 1;
        GameManager.Instance.DamagePlayer(targetPlayer, attack.damage);

        battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + "!";
        UpdateHealthDisplays();

        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;

        // Trigger the attack animation
        Animator attackerAnimator = attacker.GetComponent<Animator>();
        if (attackerAnimator != null)
        {
            attackerAnimator.SetTrigger(attack.animationTrigger);
        }

        // Show attack animation and effects
        StartCoroutine(ShowAttackAnimation(attackerAnimator, attack));
    }



    public void ApplyCharacterAnimation(GameObject playerObject, string characterName)
    {
        CharacterAnimation.ApplyCharacterAnimation(playerObject, characterName);
    }

    private IEnumerator ShowAttackAnimation(Animator animator, AttackData attack)
    {
        // Get references to attacker and defender
        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        GameObject defender = (attackingPlayer == 1) ? Player2 : Player1;

        // Wait for the specific delay for this attack before showing effect
        yield return new WaitForSeconds(attack.effectDelay);

        // Show the appropriate effect based on attack type
        if (attack.attackType == AttackType.Projectile || attack.attackType == AttackType.Magic)
        {
            yield return StartCoroutine(ShowTravelingEffect(attacker, defender, attack));
        }
        else
        {
            yield return StartCoroutine(ShowDirectEffect(defender, attack));
        }

        // Wait for attacker's animation to fully complete
        if (animator != null)
        {
            // Get remaining time in current animation
            float remainingAnimTime = animator.GetCurrentAnimatorStateInfo(0).length -
                                     animator.GetCurrentAnimatorStateInfo(0).normalizedTime *
                                     animator.GetCurrentAnimatorStateInfo(0).length;

            // Add a small buffer to ensure animation completes
            remainingAnimTime = Mathf.Max(remainingAnimTime, 0) + 0.1f;
            yield return new WaitForSeconds(remainingAnimTime);
        }

        // Add a short pause after everything is complete
        yield return new WaitForSeconds(0.5f);

        // Re-enable attack buttons if staying in this scene
        // SetAttackButtonsInteractable(true);

        // Finally transition to next scene
        SceneManager.LoadScene("QuizScene");
    }

    // Helper method to enable/disable all attack buttons
    private void SetAttackButtonsInteractable(bool interactable)
    {
        foreach (Transform child in attackPanel.transform)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    // For projectiles that travel from attacker to defender
    private IEnumerator ShowTravelingEffect(GameObject attacker, GameObject defender, AttackData attack)
    {
        // Select appropriate effect prefab
        GameObject effectPrefab = GetEffectPrefabForAttack(attack);

        // Spawn the attack effect
        GameObject attackEffect = Instantiate(effectPrefab);

        // Position the effect at the attacker's position WITH the custom offset
        attackEffect.transform.position = attack.effectOffset;

        // Calculate the target hit position (defender position + hit offset)
        Vector3 targetPosition = attack.targetHitOffset;

        // Get the direction vector from current position to target position
        Vector3 direction = targetPosition - attack.effectOffset;

        // Animate the effect moving towards the target position
        float speed = 8f; // Adjust speed as needed
        float distanceCovered = 0;
        float totalDistance = direction.magnitude;
        Vector3 normalizedDirection = direction.normalized;

        while (distanceCovered < totalDistance)
        {
            float step = speed * Time.deltaTime;
            attackEffect.transform.position += normalizedDirection * step;
            distanceCovered += step;
            yield return null;
        }

        // Position exactly at the target hit position
        attackEffect.transform.position = targetPosition;

        // Play projectile hit animation if it exists
        if (!string.IsNullOrEmpty(attack.hitEffectPrefabName))
        {
            Destroy(attackEffect);
            string pathToHitEffect = "Effects/" + attack.hitEffectPrefabName;
            GameObject hitEffectPrefab = Resources.Load<GameObject>(pathToHitEffect);
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, targetPosition, Quaternion.identity);

                Animator hitAnimator = hitEffect.GetComponent<Animator>();
                if (hitAnimator != null)
                {
                    float hitAnimationLength = 2f; // Default duration

                    // Try to get actual animation length
                    AnimatorClipInfo[] clipInfo = hitAnimator.GetCurrentAnimatorClipInfo(0);
                    if (clipInfo.Length > 0)
                    {
                        hitAnimationLength = clipInfo[0].clip.length;
                    }

                    yield return new WaitForSeconds(hitAnimationLength);

                    Destroy(hitEffect);
                }
                else
                {
                    Debug.Log("No animator");
                    yield return new WaitForSeconds(0.5f);
                    Destroy(hitEffect);
                }
            }
            else
            {
                Debug.Log("Failed to load prefab: Effects/" + attack.hitEffectPrefabName);
            }
        }
        else
        {
            Debug.Log("No hit effect");
            Destroy(attackEffect);
        }

        PlayImpactEffect(defender);
    }


    private IEnumerator ShowDirectEffect(GameObject target, AttackData attack)
    {
        // Select appropriate effect prefab
        // Select appropriate effect prefab
        GameObject effectPrefab = GetEffectPrefabForAttack(attack);

        // Spawn the effect directly on the target WITH the custom offset
        GameObject attackEffect = Instantiate(effectPrefab,
                                            attack.effectOffset,
                                            Quaternion.identity);

        // For slashes, position slightly offset from the target
        if (attack.attackType == AttackType.Slash)
        {
            // Position slightly in front of the target
            attackEffect.transform.position += new Vector3(attackingPlayer == 1 ? 0.5f : -0.5f, 0, 0);
        }

        // Get the animation/particle duration
        float effectDuration = 0.5f;
        Animator effectAnimator = attackEffect.GetComponent<Animator>();
        if (effectAnimator != null)
        {
            effectDuration = effectAnimator.GetCurrentAnimatorStateInfo(0).length;
        }
        ParticleSystem particleSystem = attackEffect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            effectDuration = particleSystem.main.duration;
        }

        // Make the target flash
        PlayImpactEffect(target);

        // Wait for the effect to complete
        yield return new WaitForSeconds(effectDuration);

        // Destroy the effect
        Destroy(attackEffect);
    }

    // Helper method to get the appropriate effect prefab
    private GameObject GetEffectPrefabForAttack(AttackData attack)
    {
        if (!string.IsNullOrEmpty(attack.effectPrefabName))
        {
            GameObject customEffect = Resources.Load<GameObject>("Effects/" + attack.effectPrefabName);
            if (customEffect != null)
                return customEffect;
        }

        // Fall back to default effects if custom one not found
        switch (attack.attackType)
        {
            case AttackType.Slash:
                return slashEffectPrefab;
            case AttackType.Projectile:
                return projectileEffectPrefab;
            case AttackType.Magic:
                return magicEffectPrefab;
            case AttackType.AreaEffect:
                return areaEffectPrefab;
            case AttackType.DirectHit:
                return directHitEffectPrefab;
            default:
                return slashEffectPrefab;
        }
    }

    private void PlayImpactEffect(GameObject target)
    {
        // You can instantiate a different effect for impact
        // For now, we'll just make the target flash
        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
        if (targetRenderer != null)
        {
            StartCoroutine(FlashSprite(targetRenderer));
        }
    }

    private IEnumerator FlashSprite(SpriteRenderer renderer)
    {
        Color originalColor = renderer.color;
        renderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;
    }

    private void ShowAttackOptions()
    {
        // Clear previous attack buttons
        foreach (Transform child in attackPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Determine which attack list to use
        List<AttackData> currentAttacks = (attackingPlayer == 1) ? player1Attacks : player2Attacks;

        float startY = -75; // Starting Y position
        float xPosition = -692; // Fixed X position
        float yStep = -100; // Distance between buttons

        int index = 0;
        foreach (AttackData attack in currentAttacks)
        {
            GameObject buttonObj = Instantiate(attackButtonPrefab, attackPanel.transform);

            // Ensure components are enabled
            Button button = buttonObj.GetComponent<Button>();
            Image image = buttonObj.GetComponent<Image>();
            if (button != null) button.enabled = true;
            if (image != null) image.enabled = true;

            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = attack.attackName + " (" + attack.damage + " dmg)";
                buttonText.enabled = true;
            }
            buttonText.gameObject.SetActive(true);

            button.onClick.AddListener(() => {
                PerformAttack(attack);
            });

            // Position adjustment
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(xPosition, startY + (yStep * index));

            // Ensure the button is interactive
            button.interactable = true;
            buttonObj.SetActive(true);

            index++;
        }
    }
    private void InitializeExampleAttacks()
    {
        player1Attacks = new List<AttackData>
    {
        // Format: name, damage, description, animation trigger, type, 
        // effect prefab, position offset, effect delay, hit effect prefab, hit offset
        new AttackData("Quick Slash", 12, "A swift sword slash.", "Attack1",
                      AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f),

        new AttackData("Warrior Slash", 15, "A flaming projectile.", "Attack1",
                      AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f),

        new AttackData("Dragon Slash", 20, "A bolt of lightning.", "Attack2",
                      AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 0.8f),

        new AttackData("Judgement Impact", 20, "A bolt of lightning.", "Attack3",
                      AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f),
    };

        player2Attacks = new List<AttackData>
    {
        new AttackData("Poison Arrow", 15, "A freezing projectile.", "Attack1",
                      AttackType.Projectile, "PoisonArrow", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, "PoisonArrow 1", new Vector3(-2.38f, -3.34f, -4.116615f)),

        new AttackData("Arrow Shower", 20, "A bolt of lightning.", "Attack2",
                      AttackType.DirectHit, "LightningEffect", new Vector3(-3.2f, -3.46f, -4.116615f), 2f),

        new AttackData("Special", 15, "A freezing projectile.", "Attack3",
                      AttackType.DirectHit, "BeamHitEffect 2", new Vector3(-2.16f, -2.62f, -4.116615f), 1.595f)
    };
    }
}