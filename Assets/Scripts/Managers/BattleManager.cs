
using System.Collections;
using System.Collections.Generic;
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

    private Dictionary<float, float> damageDealt = new Dictionary<float, float>() { { 1, 0 }, { 2, 0 } };
    private Dictionary<float, float> damageTaken = new Dictionary<float, float>() { { 1, 0 }, { 2, 0 } };
    private Dictionary<float, float> healingDone = new Dictionary<float, float>() { { 1, 0 }, { 2, 0 } };

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

            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        ShowEndScreen(defeatedPlayer);
    }

    private void ShowEndScreen(int defeatedPlayer)
    {
        if (endScreenPanel != null)
        {
            battlePanel.SetActive(false);
            endScreenPanel.SetActive(true);

            int winnerPlayer = defeatedPlayer == 1 ? 2 : 1;
            Character winnerCharacter = winnerPlayer == 1 ?
                GameManager.Instance.SelectedCharacterP1 :
                GameManager.Instance.SelectedCharacterP2;

            winnerText.text = winnerCharacter.characterName + " WINS!";
            emblemBottom.GetComponent<SpriteRenderer>().color = winnerCharacter.characterColor;
            emblemMiddle.GetComponent<SpriteRenderer>().color = winnerCharacter.primaryColor;
            emblemBorder.GetComponent<SpriteRenderer>().color = winnerCharacter.secondaryColor;
            winnerSprite.GetComponent<SpriteRenderer>().sprite = winnerCharacter.characterSprite;

            damageDealtText.text = "Damage Dealt: " + damageDealt[winnerPlayer];
            damageTakenText.text = "Damage Taken: " + damageTaken[winnerPlayer];
            healingDoneText.text = "Healing Done: " + healingDone[winnerPlayer];

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

    private void PerformAttack(AttackData attack)
    {
        SetAttackButtonsInteractable(false);

        int targetPlayer = (attackingPlayer == 1) ? 2 : 1;

        PlaySound(attack.soundEffectName);

        if (attack.attackType == AttackType.Heal)
        {
            float healAmount = GameManager.Instance.HealPlayer(attackingPlayer, attack.damage);
            healingDone[attackingPlayer] += healAmount;
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + " and recovered " + healAmount + " HP!";
        }
        else
        {
            float actualDamage = GameManager.Instance.DamagePlayer(targetPlayer, attack.damage, true);
            damageDealt[attackingPlayer] += actualDamage;
            damageTaken[targetPlayer] += actualDamage;
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + "!";
        }

        UpdateHealthDisplays();

        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        Animator attackerAnimator = attacker.GetComponent<Animator>();

        StartCoroutine(ShowAttackAnimation(attackerAnimator, attack));
    }

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
            return;

        AudioClip clip = Resources.Load<AudioClip>("SFX/" + soundName);
        if (clip != null)
        {
            GameManager.Instance.PlaySFX(soundName);

        }
        else
        {
            Debug.LogWarning("Sound effect not found: " + soundName);
        }
    }

    private IEnumerator ShowAttackAnimation(Animator animator, AttackData attack)
    {
        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        GameObject defender = (attackingPlayer == 1) ? Player2 : Player1;

        if (attack.attackType == AttackType.MoveAndHit)
        {
            yield return StartCoroutine(HandleMoveAndHit(attacker, attack, animator));
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger(attack.animationTrigger);
            }

            yield return new WaitForSeconds(attack.effectDelay);

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

    private IEnumerator HandleMoveAndHit(GameObject attacker, AttackData attack, Animator animator)
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

        PlayImpactEffect(defender, attack);

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

    private IEnumerator ShowTravelingEffect(GameObject attacker, GameObject defender, AttackData attack)
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

        PlayImpactEffect(defender, attack);
    }

    private IEnumerator ShowDirectEffect(GameObject target, AttackData attack)
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

        PlayImpactEffect(target, attack);

        yield return new WaitForSeconds(effectDuration);

        Destroy(attackEffect);
    }

    private void PlayImpactEffect(GameObject target, AttackData attack)
    {
        int targetPlayer = target == Player1 ? 1 : 2;

        Animator targetAnimator = target.GetComponent<Animator>();
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger("Hit");
        }

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

    private void ShowAttackOptions()
    {

        foreach (Transform child in attackPanel.transform)
        {
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
            if (buttonText != null)
            {
                buttonText.text = attack.attackName + " (" + attack.damage + " dmg)";
                buttonText.enabled = true;
            }
            buttonText.gameObject.SetActive(true);

            button.onClick.AddListener(() => {
                PerformAttack(attack);
            });

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(xPosition, startY + (yStep * index));

            button.interactable = true;
            buttonObj.SetActive(true);

            index++;
        }
    }
}