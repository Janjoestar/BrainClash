using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        int attackingPlayer = GameManager.Instance.GetLastCorrectPlayer();

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
        player1HealthText.text = "Player 1 HP: " + GameManager.Instance.GetPlayerHealth(1);
        player2HealthText.text = "Player 2 HP: " + GameManager.Instance.GetPlayerHealth(2);
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

    private void PerformAttack(AttackData attack)
    {
        // Current code incorrectly targets the opposite of attackingPlayer
        // int targetPlayer = (attackingPlayer == 1) ? 2 : 1;

        // Fixed version - the non-attacking player should take damage
        int targetPlayer = (attackingPlayer == 1) ? 2 : 1;
        GameManager.Instance.DamagePlayer(targetPlayer, attack.damage);
        battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + "!";
        UpdateHealthDisplays();

        GameObject attacker = (attackingPlayer == 1) ? Player1 : Player2;
        Animator animator = attacker.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger(attack.animationTrigger);

        StartCoroutine(ShowAttackAnimation(animator));
    }

        public void ApplyCharacterAnimation(GameObject playerObject, string characterName)
    {
        CharacterAnimation.ApplyCharacterAnimation(playerObject, characterName);
    }

    private IEnumerator ShowAttackAnimation(Animator animator)
    {
        if (animator != null)
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("QuizScene");
    }

    private void InitializeExampleAttacks()
    {
        player1Attacks = new List<AttackData>
        {
            new AttackData("Quick Slash", 12, "A swift sword slash.", "Attack1"),
            new AttackData("Focused Strike", 18, "A powerful attack.", "Attack1")
        };

        player2Attacks = new List<AttackData>
        {
            new AttackData("Shield Bash", 10, "Stun with a shield.", "Attack1"),
            new AttackData("Heavy Strike", 20, "A slow but strong attack.", "Attack1")
        };
    }
}