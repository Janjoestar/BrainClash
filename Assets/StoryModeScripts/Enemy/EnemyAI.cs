using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

public class EnemyAI : MonoBehaviour
{
    [Tooltip("Must match a case in StoryAttackDataManager's GetAttacksForEnemy method.")]
    public string enemyTypeName;

    private List<AttackData> allAttacks;
    private Dictionary<string, int> attackCooldowns = new Dictionary<string, int>();

    void Start()
    {
        if (StoryAttackDataManager.Instance != null)
        {
            allAttacks = StoryAttackDataManager.Instance.GetAttacksForEnemy(enemyTypeName);
            foreach (var attack in allAttacks)
            {
                attackCooldowns[attack.attackName] = 0; // All attacks start ready
            }
        }
        else
        {
            Debug.LogError($"Could not find StoryAttackDataManager instance for {enemyTypeName}.");
            allAttacks = new List<AttackData>();
        }
    }

    /// Reduces all active cooldowns by 1 turn.
    public void DecrementCooldowns()
    {
        var keys = new List<string>(attackCooldowns.Keys);
        foreach (var attackName in keys)
        {
            if (attackCooldowns[attackName] > 0)
            {
                attackCooldowns[attackName]--;
            }
        }
    }

    /// Puts an attack on cooldown after use.
    public void SetCooldownOnAttack(string attackName)
    {
        AttackData usedAttack = allAttacks.FirstOrDefault(a => a.attackName == attackName);
        if (usedAttack != null && usedAttack.maxCooldown > 0)
        {
            attackCooldowns[attackName] = usedAttack.maxCooldown;
        }
    }

    /// <summary>
    /// Coroutine to get a smart attack choice, which populates a shared result object.
    /// </summary>
    public IEnumerator GetSmartAttack(StoryBattleManager battleManager, GroqAI_Handler groq, AttackChoiceResult result)
    {
        string prompt = BuildPrompt(battleManager);

        string aiResponseContent = null;
        yield return groq.StartCoroutine(groq.GetAiChoice(prompt, response => {
            aiResponseContent = response;
        }));

        AttackData finalChoice = null;
        if (!string.IsNullOrEmpty(aiResponseContent))
        {
            Debug.Log($"[Groq Response for {name}]: {aiResponseContent}"); // Log the full response for debugging
            try
            {
                string key = "\"attackName\":";
                int keyIndex = aiResponseContent.IndexOf(key);
                if (keyIndex != -1)
                {
                    int startIndex = aiResponseContent.IndexOf("\"", keyIndex + key.Length) + 1;
                    int endIndex = aiResponseContent.IndexOf("\"", startIndex);
                    string attackName = aiResponseContent.Substring(startIndex, endIndex - startIndex);

                    finalChoice = allAttacks.FirstOrDefault(a => a.attackName == attackName && attackCooldowns.ContainsKey(a.attackName) && attackCooldowns[a.attackName] <= 0);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing AI response: {e.Message}");
                finalChoice = null;
            }
        }

        // Fallback: If AI fails or picks an invalid move, use simple logic.
        if (finalChoice == null)
        {
            Debug.LogWarning($"AI choice failed for {name}. Falling back to highest damage attack.");
            var availableAttacks = allAttacks.Where(a => attackCooldowns.ContainsKey(a.attackName) && attackCooldowns[a.attackName] <= 0).ToList();
            if (availableAttacks.Count > 0)
            {
                // A better fallback: pick the highest damage attack available.
                finalChoice = availableAttacks.OrderByDescending(a => a.damage).FirstOrDefault();
            }
        }

        result.ChosenAttack = finalChoice; // Set the result in the shared object
    }

    private string BuildPrompt(StoryBattleManager bm)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("### Player Status:");
        sb.AppendLine($"* **HP:** {Mathf.Round(bm.currentPlayerHealth)} / {bm.playerMaxHealth}");
        sb.AppendLine($"* **Shield:** {bm.ShieldAmount}");
        var upgrades = bm.upgradeManager.GetPlayerUpgrades();
        if (upgrades.Count > 0)
        {
            sb.AppendLine("* **Active Upgrades (Boons):**");
            foreach (var upgrade in upgrades) sb.AppendLine($"    * {upgrade.Key.upgradeName} (x{upgrade.Value})");
        }
        sb.AppendLine("* **Player Abilities:**");
        foreach (var attack in bm.GetCurrentPlayerAttacks()) sb.AppendLine($"    * {attack.attackName} (Dmg: {attack.damage})");

        sb.AppendLine("\n### Battlefield Status:");
        for (int i = 0; i < bm.Enemies.Count; i++)
        {
            if (bm.Enemies[i] != null && bm.Enemies[i].activeInHierarchy)
            {
                string selfIdentifier = (bm.Enemies[i] == this.gameObject) ? " (This is me)" : "";
                sb.AppendLine($"* **{bm.Enemies[i].name.Replace("(Clone)", "")} HP:** {Mathf.Round(bm.currentEnemyHealths[i])}{selfIdentifier}");
            }
        }

        sb.AppendLine("\n### My Available Attacks (Choose ONE):");
        var availableAttacks = allAttacks.Where(a => attackCooldowns.ContainsKey(a.attackName) && attackCooldowns[a.attackName] <= 0).ToList();
        if (availableAttacks.Count == 0)
        {
            sb.AppendLine("* No attacks available this turn.");
        }
        else
        {
            foreach (var attack in availableAttacks)
            {
                sb.AppendLine($"* **Name:** {attack.attackName}, **Damage:** {attack.damage}, **Accuracy:** {attack.accuracy * 100}%, **Cooldown:** {attack.maxCooldown}, **Description:** {attack.description}");
            }
        }

        sb.AppendLine("\n**Objective:** Based on all this information, choose the single best attack from my available attacks to defeat the player.");
        return sb.ToString();
    }
}

// Helper class to safely store the result from the asynchronous AI operation.
public class AttackChoiceResult
{
    public AttackData ChosenAttack { get; set; }
}