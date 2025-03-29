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

        // Load character-specific attacks
        player1Attacks = p1Character.characterAttacks;
        player2Attacks = p2Character.characterAttacks;

        // If a character doesn't have attacks defined, use default attacks
        if (player1Attacks == null || player1Attacks.Count == 0)
            player1Attacks = GetDefaultAttacks(p1Character.characterName);

        if (player2Attacks == null || player2Attacks.Count == 0)
            player2Attacks = GetDefaultAttacks(p2Character.characterName);

        // Get the player who correctly answered the quiz
        attackingPlayer = GameManager.Instance.GetLastCorrectPlayer();

        SwapPositions(attackingPlayer);
        UpdateHealthDisplays();
        battleStatusText.text = "Player " + attackingPlayer + "'s turn to attack!";

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

    public void ApplyCharacterAnimation(GameObject playerObject, string characterName)
    {
        CharacterAnimation.ApplyCharacterAnimation(playerObject, characterName);
    }

    private void PerformAttack(AttackData attack)
    {
        // Disable attack buttons during animation to prevent multiple clicks
        SetAttackButtonsInteractable(false);

        int targetPlayer = (attackingPlayer == 1) ? 2 : 1;

        // Handle healing differently from damage
        if (attack.attackType == AttackType.Heal)
        {
            // Heal the attacker instead of damaging the opponent
            GameManager.Instance.HealPlayer(attackingPlayer, attack.damage);
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + " and recovered " + attack.damage + " HP!";
        }
        else
        {
            // Standard damage logic
            GameManager.Instance.DamagePlayer(targetPlayer, attack.damage);
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + "!";
        }

        UpdateHealthDisplays();

        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;

        // Get animator but DON'T trigger animation yet for MoveAndHit
        Animator attackerAnimator = attacker.GetComponent<Animator>();

        // Handle attack logic based on type
        StartCoroutine(ShowAttackAnimation(attackerAnimator, attack));
    }

    private IEnumerator ShowAttackAnimation(Animator animator, AttackData attack)
    {
        // Get references to attacker and defender
        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        GameObject defender = (attackingPlayer == 1) ? Player2 : Player1;

        // Handle different attack types
        if (attack.attackType == AttackType.MoveAndHit)
        {
            // For MoveAndHit, we need a special sequence
            yield return StartCoroutine(HandleMoveAndHit(attacker, attack, animator));
        }
        else
        {
            // For all other attacks, trigger animation immediately
            if (animator != null)
            {
                animator.SetTrigger(attack.animationTrigger);
            }

            // Wait for effect delay
            yield return new WaitForSeconds(attack.effectDelay);

            // Show appropriate effect based on attack type
            if (attack.attackType == AttackType.Projectile || attack.attackType == AttackType.Magic)
            {
                yield return StartCoroutine(ShowTravelingEffect(attacker, defender, attack));
            }
            else if (attack.attackType == AttackType.Heal)
            {
                // Special handling for heal effects
                yield return StartCoroutine(ShowDirectEffect(attacker, attack));
            }
            else
            {
                yield return StartCoroutine(ShowDirectEffect(defender, attack));
            }

            // Wait for animation to complete
            if (animator != null)
            {
                float remainingAnimTime = animator.GetCurrentAnimatorStateInfo(0).length -
                                         animator.GetCurrentAnimatorStateInfo(0).normalizedTime *
                                         animator.GetCurrentAnimatorStateInfo(0).length;
                remainingAnimTime = Mathf.Max(remainingAnimTime, 0) + 0.1f;
                yield return new WaitForSeconds(remainingAnimTime);
            }
        }

        // Add a short pause after everything is complete
        yield return new WaitForSeconds(0.5f);

        // Re-enable attack buttons for testing
        SetAttackButtonsInteractable(true);

        //SceneManager.LoadScene("QuizScene");
    }

    // Helper method to get the appropriate effect prefab - add HEAL support
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
            case AttackType.Heal:
                return magicEffectPrefab;
            default:
                return slashEffectPrefab;
        }
    }

    private IEnumerator HandleMoveAndHit(GameObject attacker, AttackData attack, Animator animator)
    {
        Vector3 originalPosition = attacker.transform.position;
        Vector3 attackPosition = new Vector3(0f, -2.290813f, originalPosition.z);

        // First move to attack position
        float moveSpeed = 10f;
        while (Vector3.Distance(attacker.transform.position, attackPosition) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, attackPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Now that we're in position, TRIGGER the animation
        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        // Get defender reference
        GameObject defender = (attackingPlayer == 1) ? Player2 : Player1;

        // Wait for effect delay
        yield return new WaitForSeconds(attack.effectDelay);

        // Show impact effect on defender
        PlayImpactEffect(defender, attack);

        // Wait for animation to complete
        if (animator != null)
        {

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Idle")) // If not idle, wait for current animation
            {
                float animLength = stateInfo.length;
                float normalizedTimeRemaining = 1f - stateInfo.normalizedTime % 1f;
                float timeRemaining = normalizedTimeRemaining * animLength;

                yield return new WaitForSeconds(timeRemaining + 0.4f); // Add buffer
            }
        }

        // Move back to original position
        while (Vector3.Distance(attacker.transform.position, originalPosition) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, originalPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Ensure we're exactly at original position
        attacker.transform.position = originalPosition;
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

        PlayImpactEffect(defender, attack);
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
        PlayImpactEffect(target, attack);

        // Wait for the effect to complete
        yield return new WaitForSeconds(effectDuration);

        // Destroy the effect
        Destroy(attackEffect);
    }


    private void PlayImpactEffect(GameObject target, AttackData attack)
    {
        // You can instantiate a different effect for impact
        // For now, we'll just make the target flash
        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
        if (targetRenderer != null)
        {
            StartCoroutine(FlashSprite(targetRenderer, attack));
        }
    }

    private IEnumerator FlashSprite(SpriteRenderer renderer, AttackData attack)
    {
        Color originalColor = renderer.color;
        for (int i = 0; i < 3; i++)
        {
            renderer.color = attack.flashColor;
            yield return new WaitForSeconds(attack.flashInterval);
            renderer.color = originalColor;
            yield return new WaitForSeconds(attack.flashInterval);
        }
    }

    // In BattleManager.cs, update the ShowAttackOptions method
    private void ShowAttackOptions()
    {
        // Clear previous attack buttons
        foreach (Transform child in attackPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Determine which attack list to use
        List<AttackData> currentAttacks = (attackingPlayer == 1) ? player1Attacks : player2Attacks;

        // Get current character name
        string characterName = (attackingPlayer == 1) ?
            GameManager.Instance.SelectedCharacterP1.characterName :
            GameManager.Instance.SelectedCharacterP2.characterName;

        // Update battle status text to include character name
        battleStatusText.text = characterName + "'s turn to attack!";

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
                buttonText.text = attack.attackName + " (" + attack.damage + " dmg) - " + attack.attackType;
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
    private List<AttackData> GetDefaultAttacks(string characterName)
    {
        // Structure for a new attack:
        // Name, Damage, Description, AnimationTrigger, AttackType, EffectPrefab, EffectOffset, EffectDelay, FlashInterval, FlashColor, HitEffectPrefab, HitEffectOffset
        switch (characterName)
        {
            case "Knight":
                return new List<AttackData>
            {
                new AttackData("Quick Slash", 12, "A swift sword slash.", "Attack1",
                              AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Warrior Slash", 15, "A flaming projectile.", "Attack1",
                              AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Dragon Slash", 20, "A bolt of lightning.", "Attack2",
                              AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Judgement Impact", 20, "A bolt of lightning.", "Special",
                              AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
            };
            case "Archer":
                return new List<AttackData>
            {
                new AttackData("Poison Arrow", 15, "A freezing projectile.", "Attack1",
                              AttackType.Projectile, "PoisonArrow", new Vector3(1.01f, -3.7f, -4.116615f), 0.55f, 0.1f, Color.magenta, "PoisonArrow 1", new Vector3(-2.38f, -3.34f, -4.116615f)),
                new AttackData("Arrow Shower", 20, "A bolt of lightning.", "Attack2",
                              AttackType.DirectHit, "Base", new Vector3(-3.2f, -3.46f, -4.116615f), 2f, 0.1f, Color.red),
                new AttackData("GreenBeam", 15, "A freezing projectile.", "Special",
                              AttackType.DirectHit, "None", new Vector3(-2.16f, -2.62f, -4.116615f), 1.595f, 0.1f, Color.green)
            };
            case "Water":
                return new List<AttackData>
            {
                new AttackData("Heal", 20, "A ball of fire.", "Attack2",
                              AttackType.Heal, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, 0.1f, Color.green),
                new AttackData("WaterBall", 25, "A freezing spike of ice.", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, 0.1f, Color.blue, "IceExplosion", new Vector3(-2.38f, -3.34f, -4.116615f)),
                new AttackData("WaterDance", 30, "A bolt of lightning.", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.7f, 0.2f, Color.blue)
            };
            case "Samurai":
                return new List<AttackData>
            {
                new AttackData("Shield Bash", 14, "A powerful shield attack.", "Attack1",
                              AttackType.DirectHit, "ShieldBash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Holy Strike", 19, "A light-infused attack.", "Attack2",
                              AttackType.DirectHit, "HolyStrike", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Crusader's Charge", 24, "A charging attack.", "Attack3",
                              AttackType.DirectHit, "CrusaderCharge", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red)
            };
            case "Fire":
                return new List<AttackData>
            {
                new AttackData("Fire Slash", 14, "A powerful shield attack.", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.red),
                new AttackData("Spin Slash", 20, "A powerful shield attack.", "Attack2",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.2f, Color.red),
                new AttackData("Fire Combo", 25, "A light-infused attack.", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.9f, 0.38f, Color.red),
                new AttackData("Fire Slam", 40, "A charging attack.", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 2f, 0.2f, Color.red)
            };
            case "Wind":
                return new List<AttackData>
            {
                new AttackData("Wind Slash", 20, "A powerful shield attack.", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, new Color(0.659f, 0.592f, 0.447f)),
                new AttackData("Wind Barrage", 20, "A powerful shield attack.", "Attack2",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.2f, 0.1f, new Color(0.659f, 0.592f, 0.447f)),
                new AttackData("Tornado", 35, "A light-infused attack.", "Attack3",
                              AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.15f, new Color(0.659f, 0.592f, 0.447f)),
                new AttackData("Telporting Slash", 45, "A charging attack.", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.25f, new Color(0.659f, 0.592f, 0.447f))
            };
            case "Necromancer":
                return new List<AttackData>
            {
                new AttackData("Soul Rise", 20, "A powerful shield attack.", "Attack1",
                              AttackType.DirectHit, "SoulRise", new Vector3(-3.26f, -1.92f, -0.05191165f), 0.8f, 0.1f, Color.red),
                new AttackData("Blood Spike", 20, "A powerful shield attack.", "Attack1",
                              AttackType.DirectHit, "BloodSpike", new Vector3(-3.25f, -2.25f, -0.05191165f), 1.2f, 0.1f, Color.red),
                new AttackData("Red Lightning", 35, "A light-infused attack.", "Attack1",
                              AttackType.DirectHit, "RedLightning", new Vector3(-3.33f, -1.7f, -0.05191165f), 1.5f, 0.15f, Color.red),
                new AttackData("Blood Tornado", 45, "A charging attack.", "Attack1",
                              AttackType.DirectHit, "BloodTornado", new Vector3(-3.27f, -0.97f, -0.05191165f), 1.5f, 0.25f, Color.red)
            };
            case "Crystal":
                return new List<AttackData>
            {
                new AttackData("Crystal Crusher", 20, "A powerful shield attack.", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.Lerp(Color.green, Color.blue, Mathf.PingPong(Time.time, 0.5f))),
                new AttackData("Phantom Shatter", 20, "A powerful shield attack.", "Attack1",
                              AttackType.DirectHit, "PhantomShatter", new Vector3(-3.19f, -1.47f, -0.03036325f), 1.2f, 0.1f, Color.Lerp(Color.green, Color.blue, Mathf.PingPong(Time.time, 0.5f))),
                new AttackData("Crystal Eruption", 35, "A light-infused attack.", "Attack3",
                              AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.6f, 0.15f, Color.Lerp(Color.green, Color.blue, Mathf.PingPong(Time.time, 0.5f))),
                new AttackData("Crystalline Surge", 45, "A charging attack.", "Special",
                              AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.65f, 0.15f, Color.Lerp(Color.green, Color.blue, Mathf.PingPong(Time.time, 0.5f)))
            };
            case "Ground":
                return new List<AttackData>
            {
                new AttackData("Quick Punch", 15, "A powerful shield attack.", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f)),
                new AttackData("Punch Combo", 20, "A light-infused attack.", "Attack2",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f)),
                new AttackData("Rock Slide", 30, "A charging attack.", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.35f, new Color32(165, 42, 42, 255)),
                new AttackData("Rock Smash", 50, "A charging attack.", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.8f, 0.25f, new Color32(165, 42, 42, 255))
            };
            default:
                // Default attacks if character name doesn't match
                return new List<AttackData>
            {
                new AttackData("Basic Attack", 10, "A basic attack.", "Attack1",
                              AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Special Attack", 15, "A special attack.", "Attack2",
                              AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red)
            };
        }
    }
}