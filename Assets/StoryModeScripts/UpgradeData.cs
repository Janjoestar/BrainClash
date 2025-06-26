using System;
using UnityEngine;

[Serializable]
public enum UpgradeType
{
    Health,
    Damage,
    CritChance,
    Accuracy,
    HealingPower,
    DoubleEdgeReduction,
    SpecialAttack,
    Defensive,
    Utility,
    AttackModification // --- NEW ---
}

[Serializable]
public enum UpgradeRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

// --- NEW ---
[Serializable]
public enum AttackModificationType
{
    None,
    AddTargets,
    SetToFullAOE
}


[CreateAssetMenu(fileName = "New Upgrade", menuName = "Battle System/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Basic Info")]
    public string upgradeName;
    [TextArea(3, 5)]
    public string description;
    public Sprite upgradeIcon;
    public UpgradeType upgradeType;
    public UpgradeRarity rarity;

    [Header("Stat Effects")]
    public float healthIncrease = 0f;
    public float damageMultiplier = 1f;
    public float damageIncrease = 0f;
    public float critChanceIncrease = 0f;
    public float accuracyIncrease = 0f;
    public float healingMultiplier = 1f;
    public float doubleEdgeReduction = 0f;

    [Header("Special Effects")]
    public bool grantsNewAttack = false;
    [Tooltip("If 'Grants New Attack' is true, specify the name of the attack to grant from StoryAttackDataManager.")]
    public string newAttackName = "";
    public bool grantsLifesteal = false;
    public float lifestealPercentage = 0f;
    public bool grantsShield = false;
    public float shieldAmount = 0f;
    public bool grantsRegeneration = false;
    public float regenPerTurn = 0f;

    // --- NEW ---
    [Header("Attack Modification")]
    [Tooltip("Defines how this upgrade modifies an existing attack.")]
    public AttackModificationType attackModificationType = AttackModificationType.None;
    [Tooltip("How many targets to add. Used if modification type is AddTargets.")]
    public int addTargets = 0;
    [Tooltip("A multiplier applied to the attack's damage when this mod is applied. E.g., 0.8 for 80% damage.")]
    public float attackDamageMultiplier = 1.0f;


    [Header("Appearance")]
    public Color rarityColor = Color.white;
    public Color backgroundColor = Color.gray;

    [Header("Stacking")]
    public bool canStack = true;
    public int maxStacks = 5;
    public bool isUnique = false;

    // Public constructor for dynamic instantiation (optional, but good practice)
    public UpgradeData() { }

    private void OnValidate()
    {
        SetRarityColorsInternal();
    }

    public void SetRarityColorsInternal()
    {
        switch (rarity)
        {
            case UpgradeRarity.Common:
                rarityColor = Color.white;
                backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
                break;
            case UpgradeRarity.Rare:
                rarityColor = Color.blue;
                backgroundColor = new Color(0.2f, 0.2f, 1f, 0.3f);
                break;
            case UpgradeRarity.Epic:
                rarityColor = Color.magenta;
                backgroundColor = new Color(1f, 0.2f, 1f, 0.3f);
                break;
            case UpgradeRarity.Legendary:
                rarityColor = Color.yellow;
                backgroundColor = new Color(1f, 1f, 0.2f, 0.3f);
                break;
        }
    }
}