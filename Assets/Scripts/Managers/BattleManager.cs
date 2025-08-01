﻿using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [SerializeField] public GameObject Player1;
    [SerializeField] public GameObject Player2;

    [SerializeField] private GameObject battlePanel;
    [SerializeField] private Text player1HealthText;
    [SerializeField] private Text player2HealthText;
    [SerializeField] private Text battleStatusText;

    [SerializeField] private GameObject attackPanel;
    [SerializeField] private GameObject attackButtonPrefab;
    [SerializeField] private Transform foregroundPosition;
    [SerializeField] private Transform backgroundPosition;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource backgroundMusicSource;

    private List<AttackData> player1Attacks = new List<AttackData>();
    private List<AttackData> player2Attacks = new List<AttackData>();
    private int attackingPlayer = 1;

    [SerializeField] private GameObject attackHoverPrefab;
    [SerializeField] private float hoverDelay = 0.5f; // Time to wait before showing hover

    // Add these private fields:
    private GameObject currentHoverInstance;
    private Coroutine hoverCoroutine;

    [Header("End Screen Elements")]
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private GameObject emblemBottom;
    [SerializeField] private GameObject emblemMiddle;
    [SerializeField] private GameObject emblemBorder;
    [SerializeField] private GameObject winnerSprite;
    [SerializeField] private Text damageDealtText;
    [SerializeField] private Text damageTakenText;
    [SerializeField] private Text healingDoneText;
    [SerializeField] private Text healthLeftText;

    // Local status effect lists removed, will use GameManager.Instance

    private void OnEnable() => GameManager.OnGameManagerReady += InitializeBattle;
    private void OnDisable() => GameManager.OnGameManagerReady -= InitializeBattle;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.SelectedCharacterP1 != null)
        {
            InitializeBattle();

            // Check if a player was defeated in the quiz phase
            int defeatedPlayer = GameManager.Instance.GetDefeatedPlayerInQuiz();
            if (defeatedPlayer > 0)
            {
                // Show end screen immediately
                ShowEndScreen(defeatedPlayer);
            }
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

        Debug.Log($"Player 1 is taking {GameManager.player1DamageMultiplier} times more damage");
        Debug.Log($"Player 2 is taking {GameManager.player2DamageMultiplier} times more damage");

        SetCharacter(Player1, p1Character);
        SetCharacter(Player2, p2Character);

        player1Attacks = p1Character.characterAttacks;
        player2Attacks = p2Character.characterAttacks;

        if (player1Attacks == null || player1Attacks.Count == 0)
            player1Attacks = AttackDataManager.Instance.GetAttacksForCharacter(p1Character.characterName);

        if (player2Attacks == null || player2Attacks.Count == 0)
            player2Attacks = AttackDataManager.Instance.GetAttacksForCharacter(p2Character.characterName);

        attackingPlayer = GameManager.Instance.GetLastCorrectPlayer();

        SwapPositions(attackingPlayer);
        UpdateHealthDisplays();
        battleStatusText.text = "Player " + attackingPlayer + "'s turn to attack!";


        ShowAttackOptions();
    }

    private GameObject GetEffectPrefabForAttack(AttackData attack)
    {
        return AttackDataManager.Instance.GetEffectPrefabForAttack(attack);
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

    private void CheckForGameOver()
    {
        float p1Health = GameManager.Instance.GetPlayerHealth(1);
        float p2Health = GameManager.Instance.GetPlayerHealth(2);

        if (p1Health <= 0 || p2Health <= 0)
        {
            StartCoroutine(HandlePlayerDeath(p1Health <= 0 ? 1 : 2));
        }
        else
        {
            // Update cooldowns for *both* players before switching turns
            GameManager.Instance.UpdatePlayerCooldowns(1); // Update P1 cooldowns
            GameManager.Instance.UpdatePlayerCooldowns(2); // Update P2 cooldowns

            SceneManager.LoadScene("QuizScene");
        }
    }

    private IEnumerator HandlePlayerDeath(int defeatedPlayer)
    {
        GameObject defeatedPlayerObj = defeatedPlayer == 1 ? Player1 : Player2;
        Animator animator = defeatedPlayerObj.GetComponent<Animator>();

        if (animator != null)
        {
            animator.SetTrigger("Death");

            yield return null;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            while (stateInfo.normalizedTime < 1.0f)
            {
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }

            yield return new WaitForSeconds(0.75f);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        ShowEndScreen(defeatedPlayer);
    }

    private void ShowEndScreen(int defeatedPlayer)
    {
        if (endScreenPanel != null)
        {
            battlePanel.SetActive(false);
            endScreenPanel.SetActive(true);

            GameManager.Instance.PlaySFX("General/Win");

            int winnerPlayer = defeatedPlayer == 1 ? 2 : 1;
            Character winnerCharacter = winnerPlayer == 1 ?
                GameManager.Instance.SelectedCharacterP1 :
                GameManager.Instance.SelectedCharacterP2;

            winnerText.text = winnerCharacter.characterName + " WINS!";
            emblemBottom.GetComponent<SpriteRenderer>().color = winnerCharacter.characterColor;
            emblemMiddle.GetComponent<SpriteRenderer>().color = winnerCharacter.primaryColor;
            emblemBorder.GetComponent<SpriteRenderer>().color = winnerCharacter.secondaryColor;
            winnerSprite.GetComponent<SpriteRenderer>().sprite = winnerCharacter.characterSprite;

            damageDealtText.text = "Damage Dealt: " + GameManager.Instance.GetDamageDealt(winnerPlayer);
            damageTakenText.text = "Damage Taken: " + GameManager.Instance.GetDamageTaken(winnerPlayer);
            healingDoneText.text = "Healing Done: " + GameManager.Instance.GetHealingDone(winnerPlayer);
            healthLeftText.text = "Health Left: " + GameManager.Instance.GetPlayerHealth(winnerPlayer);

            SpriteRenderer spriteRenderer = winnerSprite.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sprite = winnerCharacter.characterSprite;

            Animator animator = winnerSprite.GetComponent<Animator>();
            if (animator != null)
            {
                string overridePath = "Animations/" + winnerCharacter.characterName + "Override";
                AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);
                if (overrideController != null)
                    animator.runtimeAnimatorController = overrideController;
                else
                    Debug.LogWarning("No Animator Override Controller found for " + winnerCharacter.characterName);
            }

            if (returnToMenuButton != null)
            {
                ColorBlock cb = returnToMenuButton.colors;
                cb.highlightedColor = winnerCharacter.primaryColor;
                returnToMenuButton.colors = cb;
                returnToMenuButton.onClick.AddListener(() => {
                    SceneManager.LoadScene("StartScreen");
                });
            }
            if (playAgainButton != null)
            {
                ColorBlock cb = playAgainButton.colors;
                cb.highlightedColor = winnerCharacter.primaryColor;
                playAgainButton.colors = cb;
                playAgainButton.onClick.AddListener(() => {
                    SceneManager.LoadScene("CharacterSelection");
                });
            }
        }
        else
        {
            Debug.LogError("End Screen Panel is not assigned in the inspector!");
            SceneManager.LoadScene("StartScreen");
        }
    }


    private void UpdateHealthDisplays()
    {
        float p1Health = GameManager.Instance.GetPlayerHealth(1);
        float p2Health = GameManager.Instance.GetPlayerHealth(2);

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

    private bool IsAttackAvailable(AttackData attack)
    {
        int cooldown = GameManager.Instance.GetAttackCooldown(attackingPlayer, attack.attackName);
        return cooldown <= 0;
    }

    private void PerformAttack(AttackData attack)
    {
        // Check if attack is on cooldown
        if (!IsAttackAvailable(attack))
        {
            battleStatusText.text = attack.attackName + " is on cooldown!";
            return;
        }

        SetAttackButtonsInteractable(false);

        // Set cooldown for this attack if it has one (this will now apply the maxCooldown, as initial is set in GameManager)
        if (attack.maxCooldown > 0)
        {
            GameManager.Instance.SetAttackCooldown(attackingPlayer, attack.attackName, attack.maxCooldown);
        }

        int targetPlayer = (attackingPlayer == 1) ? 2 : 1;
        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        Animator attackerAnimator = attacker.GetComponent<Animator>();


        if (attack.canSelfKO)
        {
            float selfKORoll = Random.Range(0f, 1f);
            if (selfKORoll < attack.selfKOFailChance)
            {
                battleStatusText.text = "Player " + attackingPlayer + "'s " + attack.attackName + " backfired! They defeated themselves!";
                GameManager.Instance.DamagePlayer(attackingPlayer, GameManager.Instance.GetPlayerHealth(attackingPlayer), false);
                GameManager.Instance.PlaySelfKOSound();
                StartCoroutine(ShowSelfKOAnimation(attackerAnimator, attack));
                return;
            }
        }

        if (Random.Range(0f, 1f) > attack.accuracy)
        {
            battleStatusText.text = "Player " + attackingPlayer + "'s " + attack.attackName + " missed!";

            if (attack.doubleEdgeDamage > 0)
            {
                float selfDamage = GameManager.Instance.DamagePlayer(attackingPlayer, attack.doubleEdgeDamage, false);
                battleStatusText.text += "\nBut Player " + attackingPlayer + " hurt themselves for " + selfDamage + " damage!";
            }

            GameManager.Instance.PlaySFX("General/MissVoice");
            StartCoroutine(ShowMissedAttack(attack.doubleEdgeDamage > 0 ? attacker : null));
            return;
        }

        if (attack.attackType == AttackType.Heal)
        {
            GameManager.Instance.PlaySFX(attack.soundEffectName);
            float healAmount = GameManager.Instance.HealPlayer(attackingPlayer, attack.damage);
            GameManager.Instance.AddHealingDone(attackingPlayer, healAmount);
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + " and recovered " + healAmount + " HP!";
        }
        else
        {
            GameManager.Instance.PlaySFX(attack.soundEffectName);
            float finalDamage = attack.damage;
            bool isCrit = false;

            // Check for critical hit
            if (Random.Range(0f, 1f) < attack.critChance)
            {
                finalDamage *= 2f; // Double damage for crit
                isCrit = true;
                GameManager.Instance.PlayCritSound();
            }

            float actualDamage = GameManager.Instance.DamagePlayer(targetPlayer, finalDamage, true);
            GameManager.Instance.AddDamageDealt(attackingPlayer, actualDamage);
            GameManager.Instance.AddDamageTaken(targetPlayer, actualDamage);

            string critText = isCrit ? " It's a critical hit!" : "";
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + "!" + critText;

            // Apply double edge damage *after* dealing damage to the enemy
            if (attack.doubleEdgeDamage > 0)
            {
                float selfDamage = GameManager.Instance.DamagePlayer(attackingPlayer, attack.doubleEdgeDamage, false);
                battleStatusText.text += "\nBut Player " + attackingPlayer + " hurt themselves for " + selfDamage + " damage!";
            }
        }

        StartCoroutine(ShowAttackAnimation(attackerAnimator, attack, false)); // false = hit
        UpdateHealthDisplays();
    }

    // Add this new method for handling self-KO:
    private IEnumerator ShowSelfKOAnimation(Animator animator, AttackData attack)
    {
        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;

        // Play hit animation and sound on attacker
        GameManager.Instance.PlayHitSound();
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        // Flash the attacker sprite
        SpriteRenderer attackerRenderer = attacker.GetComponent<SpriteRenderer>();
        if (attackerRenderer != null)
        {
            StartCoroutine(FlashSprite(attackerRenderer, attack));
        }

        yield return new WaitForSeconds(0.35f); // Wait after hit

        // Now handle death
        StartCoroutine(HandlePlayerDeath(attackingPlayer));
    }

    // Add this new method for handling missed attacks:
    private IEnumerator ShowMissedAttack(GameObject selfDamageTarget)
    {
        yield return new WaitForSeconds(1.0f); // Wait to let players process the miss

        // If there's self-damage from double-edge, show hit animation on attacker
        if (selfDamageTarget != null)
        {
            Animator attackerAnimator = selfDamageTarget.GetComponent<Animator>();
            if (attackerAnimator != null)
            {
                GameManager.Instance.PlayHitSound();
                attackerAnimator.SetTrigger("Hit");
            }

            // Flash the attacker sprite for self-damage
            SpriteRenderer attackerRenderer = selfDamageTarget.GetComponent<SpriteRenderer>();
            if (attackerRenderer != null)
            {
                Color originalColor = Color.white;
                for (int i = 0; i < 3; i++)
                {
                    attackerRenderer.color = Color.red;
                    yield return new WaitForSeconds(0.1f);
                    attackerRenderer.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield return new WaitForSeconds(0.5f); // Wait after self-damage animation
        }

        UpdateHealthDisplays();
        CheckForGameOver();
    }

    private IEnumerator ShowAttackAnimation(Animator animator, AttackData attack, bool missed = false)
    {
        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        GameObject defender = (attackingPlayer == 1) ? Player2 : Player1;

        if (attack.attackType == AttackType.MoveAndHit)
        {
            yield return StartCoroutine(HandleMoveAndHit(attacker, attack, animator, missed));
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger(attack.animationTrigger);
            }

            yield return new WaitForSeconds(attack.effectDelay);

            // Only show effects if attack didn't miss
            if (!missed)
            {
                if (attack.attackType == AttackType.Projectile || attack.attackType == AttackType.Magic)
                {
                    yield return StartCoroutine(ShowTravelingEffect(attacker, defender, attack));
                }
                else if (attack.attackType == AttackType.Heal)
                {
                    yield return StartCoroutine(ShowDirectEffect(attacker, attack));
                }
                else
                {
                    yield return StartCoroutine(ShowDirectEffect(defender, attack));
                }
            }

            if (animator != null)
            {
                float remainingAnimTime = animator.GetCurrentAnimatorStateInfo(0).length -
                                          animator.GetCurrentAnimatorStateInfo(0).normalizedTime *
                                          animator.GetCurrentAnimatorStateInfo(0).length;
                remainingAnimTime = Mathf.Max(remainingAnimTime, 0) + 0.1f;
                yield return new WaitForSeconds(remainingAnimTime);
            }
        }

        yield return new WaitForSeconds(0.5f);

        CheckForGameOver();
    }

    private IEnumerator HandleMoveAndHit(GameObject attacker, AttackData attack, Animator animator, bool missed = false)
    {
        Vector3 originalPosition = attacker.transform.position;
        Vector3 attackPosition = new Vector3(0f, -2.290813f, originalPosition.z);

        float moveSpeed = 10f;
        while (Vector3.Distance(attacker.transform.position, attackPosition) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, attackPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (animator != null)
        {
            animator.SetTrigger(attack.animationTrigger);
        }

        GameObject defender = (attackingPlayer == 1) ? Player2 : Player1;

        yield return new WaitForSeconds(attack.effectDelay);

        // Only show impact effect if attack didn't miss
        if (!missed)
        {
            PlayImpactEffect(defender, attack);
        }

        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Idle"))
            {
                float animLength = stateInfo.length;
                float normalizedTimeRemaining = 1f - stateInfo.normalizedTime % 1f;
                float timeRemaining = normalizedTimeRemaining * animLength;

                yield return new WaitForSeconds(timeRemaining + 0.4f);
            }
        }

        while (Vector3.Distance(attacker.transform.position, originalPosition) > 0.05f)
        {
            attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, originalPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        attacker.transform.position = originalPosition;
    }

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

    private IEnumerator ShowTravelingEffect(GameObject attacker, GameObject defender, AttackData attack, bool missed = false)
    {
        GameObject effectPrefab = GetEffectPrefabForAttack(attack);

        GameObject attackEffect = Instantiate(effectPrefab);

        attackEffect.transform.position = attack.effectOffset;

        Vector3 targetPosition = attack.targetHitOffset;

        Vector3 direction = targetPosition - attack.effectOffset;

        float speed = 8f;
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

        attackEffect.transform.position = targetPosition;

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
                    float hitAnimationLength = 2f;

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

        // Only play impact effect if attack didn't miss
        if (!missed)
        {
            PlayImpactEffect(defender, attack);
        }
    }

    private IEnumerator ShowDirectEffect(GameObject target, AttackData attack, bool missed = false)
    {

        GameObject effectPrefab = GetEffectPrefabForAttack(attack);

        GameObject attackEffect = Instantiate(effectPrefab,
                                             attack.effectOffset,
                                             Quaternion.identity);

        if (attack.attackType == AttackType.Slash)
        {
            attackEffect.transform.position += new Vector3(attackingPlayer == 1 ? 0.5f : -0.5f, 0, 0);
        }

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

        // Only play impact effect if attack didn't miss
        if (!missed)
        {
            PlayImpactEffect(target, attack);
        }

        yield return new WaitForSeconds(effectDuration);

        Destroy(attackEffect);
    }

    private void PlayImpactEffect(GameObject target, AttackData attack)
    {

        int targetPlayer = target == Player1 ? 1 : 2;

        Animator targetAnimator = target.GetComponent<Animator>();

        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
        if (attack.attackType == AttackType.Heal)
        {
            StartCoroutine(FlashSprite(targetRenderer, attack));
            return;
        }
        if (targetAnimator != null)
        {
            GameManager.Instance.PlayHitSound();
            targetAnimator.SetTrigger("Hit");
        }
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
    private void OnAttackButtonHoverEnter(AttackData attack, RectTransform buttonRect)
    {
        // Cancel any existing hover coroutine
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }

        hoverCoroutine = StartCoroutine(ShowHoverAfterDelay(attack, buttonRect));
    }

    // Add this method to handle hover exit:
    private void OnAttackButtonHoverExit()
    {
        // Cancel hover coroutine if still waiting
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }

        // Destroy hover instance if it exists
        if (currentHoverInstance != null)
        {
            Destroy(currentHoverInstance);
            currentHoverInstance = null;
        }
    }

    private IEnumerator ShowHoverAfterDelay(AttackData attack, RectTransform buttonRect)
    {
        yield return new WaitForSeconds(hoverDelay);

        // Create hover instance
        currentHoverInstance = Instantiate(attackHoverPrefab, attackPanel.transform);

        // Set hover text content
        Text[] hoverTexts = currentHoverInstance.GetComponentsInChildren<Text>();
        foreach (Text text in hoverTexts)
        {
            if (text.name.Contains("Damage") || text.name.Contains("damage"))
            {
                text.text = attack.damage.ToString() + " dmg";
            }
            else if (text.name.Contains("Description") || text.name.Contains("description"))
            {
                string enhancedDescription = attack.description;
                enhancedDescription += "\nCrit: " + (attack.critChance * 100f).ToString("F0") + "%";
                enhancedDescription += "\nAccuracy: " + (attack.accuracy * 100f).ToString("F0") + "%";
                if (attack.doubleEdgeDamage > 0)
                {
                    enhancedDescription += "\nSelf-damage: " + attack.doubleEdgeDamage;
                }
                // *** ADD THIS BLOCK FOR SELF-KO CHANCE ***
                if (attack.canSelfKO)
                {
                    enhancedDescription += "\nSelf-KO Chance: " + (attack.selfKOFailChance * 100f).ToString("F0") + "%";
                }
                // *****************************************
                text.text = enhancedDescription;
            }
        }

        // Position hover 125px below the button
        RectTransform hoverRect = currentHoverInstance.GetComponent<RectTransform>();
        Vector2 buttonPos = buttonRect.anchoredPosition;
        hoverRect.anchoredPosition = new Vector2(buttonPos.x, buttonPos.y + 125f);

        // Disable raycast target on hover popup to prevent interference
        Graphic[] graphics = currentHoverInstance.GetComponentsInChildren<Graphic>();
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        hoverCoroutine = null;
    }

    // Modify your ShowAttackOptions method - replace the button creation section with this:
    private void ShowAttackOptions()
    {
        foreach (Transform child in attackPanel.transform)
        {
            if (child.gameObject != attackHoverPrefab)
                Destroy(child.gameObject);
        }

        List<AttackData> currentAttacks = (attackingPlayer == 1) ? player1Attacks : player2Attacks;

        string characterName = (attackingPlayer == 1) ?
            GameManager.Instance.SelectedCharacterP1.characterName :
            GameManager.Instance.SelectedCharacterP2.characterName;

        battleStatusText.text = characterName + "'s turn to attack!";

        float startY = -75;
        float xPosition = 692;
        float yStep = -100;

        int index = 0;
        foreach (AttackData attack in currentAttacks)
        {
            GameObject buttonObj = Instantiate(attackButtonPrefab, attackPanel.transform);

            Button button = buttonObj.GetComponent<Button>();
            Image image = buttonObj.GetComponent<Image>();
            if (button != null) button.enabled = true;
            if (image != null) image.enabled = true;

            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            int cooldown = GameManager.Instance.GetAttackCooldown(attackingPlayer, attack.attackName);
            if (buttonText != null)
            {
                // Check if attack is on cooldown and modify button text
                if (cooldown > 0)
                {
                    buttonText.text = attack.attackName + " (" + cooldown + ")";
                    button.interactable = false; // Disable button if on cooldown
                    image.color = Color.gray; // Make button appear grayed out
                }
                else
                {
                    buttonText.text = attack.attackName;
                    button.interactable = true;
                    image.color = Color.white;
                }
                buttonText.enabled = true;
            }
            buttonText.gameObject.SetActive(true);

            button.onClick.AddListener(() => {
                PerformAttack(attack);
            });

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(xPosition, startY + (yStep * index));

            // Add EventTrigger for hover functionality (only if not on cooldown)
            if (cooldown <= 0)
            {
                UnityEngine.EventSystems.EventTrigger eventTrigger = buttonObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }

                // Pointer Enter event
                UnityEngine.EventSystems.EventTrigger.Entry pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                pointerEnter.callback.AddListener((data) => { OnAttackButtonHoverEnter(attack, buttonRect); });
                eventTrigger.triggers.Add(pointerEnter);

                // Pointer Exit event
                UnityEngine.EventSystems.EventTrigger.Entry pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                pointerExit.callback.AddListener((data) => { OnAttackButtonHoverExit(); });
                eventTrigger.triggers.Add(pointerExit);
            }

            buttonObj.SetActive(true);
            index++;
        }
    }
}