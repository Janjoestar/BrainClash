using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private GameObject Player1;
    [SerializeField] private GameObject Player2;

    [SerializeField] private Text player1HealthText;
    [SerializeField] private Text player2HealthText;
    [SerializeField] private Text battleStatusText;

    [SerializeField] private GameObject attackPanel;
    [SerializeField] private GameObject attackButtonPrefab;
    private List<AttackData> player1Attacks = new List<AttackData>(); // Samurai Attacks
    private List<AttackData> player2Attacks = new List<AttackData>(); // Knight Attacks

    private int attackingPlayer;


    private void OnEnable()
    {
        GameManager.OnGameManagerReady += InitializeBattle;
    }

    private void OnDisable()
    {
        GameManager.OnGameManagerReady -= InitializeBattle;
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

        SetCharacter(Player1, p1Character);
        SetCharacter(Player2, p2Character);

        attackingPlayer = GameManager.Instance.GetLastCorrectPlayer();
        if (attackingPlayer == 0) attackingPlayer = 1;

        UpdateHealthDisplays();

        if (battleStatusText != null)
        {
            battleStatusText.text = "Player " + attackingPlayer + "'s turn to attack!";
        }
        InitializeExampleAttacks();
        ShowAttackOptions();
    }

    private void SetCharacter(GameObject playerObject, Character character)
    {
        if (character == null) return;

        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = character.characterSprite;
        }
    }


    private void UpdateHealthDisplays()
    {
        if (GameManager.Instance != null)
        {
            if (player1HealthText != null)
                player1HealthText.text = "Player 1 HP: " + GameManager.Instance.GetPlayerHealth(1);

            if (player2HealthText != null)
                player2HealthText.text = "Player 2 HP: " + GameManager.Instance.GetPlayerHealth(2);
        }
        else
        {
            // Debug mode
            if (player1HealthText != null)
                player1HealthText.text = "Player 1 HP: 100";

            if (player2HealthText != null)
                player2HealthText.text = "Player 2 HP: 100";
        }
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

            // Add tooltip description (optional)
            Tooltip tooltip = buttonObj.AddComponent<Tooltip>();
            tooltip.tooltipText = attack.description;

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
        // Determine target (opposite of attacker)
        int targetPlayer = attackingPlayer == 1 ? 2 : 1;

        // Apply damage
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DamagePlayer(targetPlayer, attack.damage);
        }

        // Update battle status
        if (battleStatusText != null)
        {
            battleStatusText.text = "Player " + attackingPlayer + " used " + attack.attackName + "!";
        }

        // Update displays
        UpdateHealthDisplays();

        // Show attack animation
        StartCoroutine(ShowAttackAnimation(attackingPlayer, targetPlayer, attack));
    }

    private IEnumerator ShowAttackAnimation(int attacker, int target, AttackData attack)
    {
        // Simple animation - flash the target character with attack color
        GameObject targetCharacter = target == 1 ? Player1 : Player2;
        SpriteRenderer renderer = targetCharacter.GetComponent<SpriteRenderer>();

        if (renderer != null)
        {
            Color originalColor = renderer.color;

            // Flash 3 times
            for (int i = 0; i < 3; i++)
            {
                renderer.color = attack.effectColor;
                yield return new WaitForSeconds(0.1f);
                renderer.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Wait a bit before returning to quiz
        yield return new WaitForSeconds(1f);

        // Return to quiz scene for next question
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToQuizScene();
        }
    }

    private void InitializeExampleAttacks()
    {
        // Samurai Attacks (Player 1)
        player1Attacks.Add(new AttackData
        {
            attackName = "Quick Slash",
            damage = 12,
            description = "A swift sword slash that deals moderate damage.",
            effectColor = new Color(1f, 0f, 0f) // Red
        });

        player1Attacks.Add(new AttackData
        {
            attackName = "Focused Strike",
            damage = 18,
            description = "A focused attack that deals high damage.",
            effectColor = new Color(1f, 0.5f, 0f) // Orange
        });

        player1Attacks.Add(new AttackData
        {
            attackName = "Parry",
            damage = 0,
            description = "Block the next attack and counterattack.",
            effectColor = new Color(0.5f, 0.5f, 1f) // Blue
        });

        player1Attacks.Add(new AttackData
        {
            attackName = "Blade Dance",
            damage = 25,
            description = "A spinning sword attack hitting multiple times.",
            effectColor = new Color(1f, 1f, 0f) // Yellow
        });

        // Knight Attacks (Player 2)
        player2Attacks.Add(new AttackData
        {
            attackName = "Shield Bash",
            damage = 10,
            description = "Bash the enemy with a shield, stunning them.",
            effectColor = new Color(0f, 1f, 1f) // Cyan
        });

        player2Attacks.Add(new AttackData
        {
            attackName = "Heavy Strike",
            damage = 20,
            description = "A slow but powerful attack with a greatsword.",
            effectColor = new Color(0.5f, 0.3f, 0.1f) // Brown
        });

        player2Attacks.Add(new AttackData
        {
            attackName = "Defensive Stance",
            damage = 0,
            description = "Reduce damage taken on the next turn.",
            effectColor = new Color(0.3f, 0.3f, 1f) // Dark Blue
        });

        player2Attacks.Add(new AttackData
        {
            attackName = "Divine Strike",
            damage = 22,
            description = "A holy attack that deals extra damage to evil foes.",
            effectColor = new Color(1f, 1f, 0.5f) // Light Yellow
        });
    }
}