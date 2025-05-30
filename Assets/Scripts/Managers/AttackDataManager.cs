using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    Slash,      // Melee slash effects that appear near the target
    Projectile, // Effects that travel from attacker to defender
    Magic,      // Magical projectiles that travel
    AreaEffect, // Effects that appear on or around the target (explosions, etc.)
    DirectHit,  // Effects that appear directly on the target (hammer hit, etc.)
    MoveAndHit, // Character moves to target, attacks, then returns to position
    Heal
}

[System.Serializable]
public class AttackData
{
    public string attackName;
    public float damage;
    public string description;
    public string animationTrigger;
    public AttackType attackType;
    public string effectPrefabName;
    public Vector3 effectOffset;
    public float effectDelay;
    public string hitEffectPrefabName;
    public Vector3 targetHitOffset;
    public float flashInterval;
    public Color flashColor;
    public string soundEffectName;

    public AttackData(string name, float dmg, string desc, string animTrigger,
                    AttackType type, string effectName, Vector3 offset, float delay,
                    float flashInterval, Color flashColor, string sound = "",
                    string hitEffectName = "", Vector3 hitOffset = default)
    {
        attackName = name;
        damage = dmg;
        description = desc;
        animationTrigger = animTrigger;
        attackType = type;
        effectPrefabName = effectName;
        effectOffset = offset;
        effectDelay = delay;
        this.flashInterval = flashInterval;
        this.flashColor = flashColor;
        soundEffectName = sound;
        hitEffectPrefabName = hitEffectName;
        targetHitOffset = hitOffset == default ? Vector3.zero : hitOffset;
    }

    // Backward compatibility constructor
    public AttackData(string name, int dmg, string desc, string animTrigger, AttackType type)
        : this(name, dmg, desc, animTrigger, type, "", Vector3.zero, 0.3f, 0.1f, Color.red)
    {
    }
}

[System.Serializable]
public class CharacterStats
{
    public string characterName;
    public int maxHealth;
    public int currentHealth;
    public float defenseModifier; // Damage reduction multiplier (0.9f = 10% reduction)
    public float speedModifier;   // Attack speed multiplier (0.8f = 20% faster)

    public CharacterStats(string name, int health, float defense = 1.0f, float speed = 1.0f)
    {
        characterName = name;
        maxHealth = health;
        currentHealth = health;
        defenseModifier = defense;
        speedModifier = speed;
    }
}

public class AttackDataManager : MonoBehaviour
{
    private static AttackDataManager _instance;
    public static AttackDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject attackSystemObj = new GameObject("AttackSystem");
                _instance = attackSystemObj.AddComponent<AttackDataManager>();
                DontDestroyOnLoad(attackSystemObj);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Get character stats including health
    public CharacterStats GetCharacterStats(string characterName)
    {
        switch (characterName)
        {
            case "Knight":
                return new CharacterStats("Knight", 120, 0.85f, 1.1f); // Tank: High HP, good defense, fast attacks
            case "Archer":
                return new CharacterStats("Archer", 85, 1.1f, 0.9f);   // Glass cannon: Low HP, weak defense, fast attacks
            case "Water":
                return new CharacterStats("Water", 100, 0.95f, 1.0f);  // Support: Medium HP, slight defense, healing
            case "Samurai":
                return new CharacterStats("Samurai", 110, 0.9f, 1.0f); // Balanced: Good HP, good defense
            case "Fire":
                return new CharacterStats("Fire", 95, 1.05f, 0.95f);   // Berserker: Medium HP, weak defense, fast
            case "Wind":
                return new CharacterStats("Wind", 80, 1.15f, 0.85f);   // Speed demon: Low HP, weak defense, very fast
            case "Necromancer":
                return new CharacterStats("Necromancer", 75, 1.2f, 0.9f); // Glass cannon: Very low HP, very weak defense
            case "Crystal":
                return new CharacterStats("Crystal", 90, 1.1f, 0.9f);  // Burst: Low HP, weak defense
            case "Ground":
                return new CharacterStats("Ground", 140, 0.8f, 1.2f);  // Heavy tank: Very high HP, great defense, slow
            default:
                return new CharacterStats("Default", 100, 1.0f, 1.0f);
        }
    }

    // Balanced attack data for all characters
    public List<AttackData> GetAttacksForCharacter(string characterName)
    {
        switch (characterName)
        {
            case "Knight":
                return new List<AttackData>
                {
                    new AttackData("Swift Strike", 15, "Quick reliable sword hit", "Attack1",
                                  AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                    new AttackData("Power Slash", 18, "Stronger blade attack", "Attack1",
                                  AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.9f, 0.1f, Color.red),
                    new AttackData("Shield Slam", 20, "Defensive counter-attack", "Attack2",
                                  AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 1.0f, 0.1f, Color.blue),
                    new AttackData("Divine Judgment", 26, "Blessed finishing move", "Special",
                                  AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 1.2f, 0.1f, Color.yellow)
                };

            case "Archer":
                return new List<AttackData>
                {
                    new AttackData("Quick Shot", 16, "Fast arrow attack", "Attack1",
                                  AttackType.Projectile, "PoisonArrow", new Vector3(1.01f, -3.7f, -4.116615f), 0.8f, 0.1f, Color.green, "SoundEffect", "PoisonArrowHitEffect", new Vector3(-2.43f,-3.291f,0.1f)),
                    new AttackData("Piercing Shot", 19, "Armor-piercing arrow", "Attack2",
                                  AttackType.Projectile, "ArrowShower", new Vector3(-3.2f, -3.46f, -4.116615f), 1.0f, 0.1f, Color.red),
                    new AttackData("Multi-Shot", 22, "Spread arrow volley", "Attack2",
                                  AttackType.AreaEffect, "ArrowShower", new Vector3(-3.2f, -3.46f, -4.116615f), 1.3f, 0.15f, Color.red),
                    new AttackData("Explosive Shot", 28, "Devastating blast arrow", "Special",
                                  AttackType.AreaEffect, "None", new Vector3(-2.16f, -2.62f, -4.116615f), 1.5f, 0.2f, Color.red)
                };

            case "Water":
                return new List<AttackData>
                {
                    new AttackData("Healing Wave", 25, "Restore ally health", "Attack2",
                                  AttackType.Heal, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.2f, 0.1f, Color.cyan, "Heal"),
                    new AttackData("Water Bolt", 18, "Pressurized water attack", "Attack1",
                                  AttackType.Projectile, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.0f, 0.1f, Color.blue),
                    new AttackData("Tidal Strike", 22, "Flowing melee combo", "Special",
                                  AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.4f, 0.15f, Color.blue),
                    new AttackData("Tsunami", 26, "Overwhelming water force", "Attack3",
                                  AttackType.AreaEffect, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.8f, 0.2f, Color.blue)
                };

            case "Samurai":
                return new List<AttackData>
                {
                    new AttackData("Katana Strike", 16, "Precise blade cut", "Attack1",
                                  AttackType.DirectHit, "ShieldBash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.9f, 0.1f, Color.red),
                    new AttackData("Honor Slash", 20, "Disciplined sword technique", "Attack2",
                                  AttackType.DirectHit, "HolyStrike", new Vector3(-3.25f, -2.33f, -4.116615f), 1.0f, 0.1f, Color.yellow),
                    new AttackData("Bushido Combo", 24, "Traditional sword dance", "Attack3",
                                  AttackType.Slash, "CrusaderCharge", new Vector3(-2.81f, -2.18f, -4.116615f), 1.2f, 0.1f, Color.red),
                    new AttackData("Seppuku Strike", 30, "Ultimate sacrifice attack", "Special",
                                  AttackType.DirectHit, "CrusaderCharge", new Vector3(-2.81f, -2.18f, -4.116615f), 1.5f, 0.15f, Color.red)
                };

            case "Fire":
                return new List<AttackData>
                {
                    new AttackData("Flame Punch", 17, "Burning fist attack", "Attack1",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.9f, 0.1f, Color.red, "FireSlash"),
                    new AttackData("Fire Spin", 21, "Whirling flame strike", "Attack2",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.1f, 0.15f, Color.red, "FireSpin"),
                    new AttackData("Blazing Combo", 25, "Multi-hit fire assault", "Attack3",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.3f, 0.2f, Color.red, "FireSpin"),
                    new AttackData("Inferno Blast", 32, "Explosive fire eruption", "Special",
                                  AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.8f, 0.25f, Color.red, "FireCharge")
                };

            case "Wind":
                return new List<AttackData>
                {
                    new AttackData("Wind Blade", 19, "Cutting air strike", "Attack1",
                                  AttackType.Projectile, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, new Color(0.659f, 0.592f, 0.447f)),
                    new AttackData("Gust Barrage", 22, "Rapid air attacks", "Attack2",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.0f, 0.1f, new Color(0.659f, 0.592f, 0.447f)),
                    new AttackData("Cyclone", 26, "Swirling wind vortex", "Attack3",
                                  AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.4f, 0.15f, new Color(0.659f, 0.592f, 0.447f), "WindTornado"),
                    new AttackData("Storm Strike", 32, "Lightning-fast teleport attack", "Special",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.6f, 0.2f, new Color(0.659f, 0.592f, 0.447f))
                };

            case "Necromancer":
                return new List<AttackData>
                {
                    new AttackData("Soul Drain", 20, "Life-stealing magic", "Attack1",
                                  AttackType.Magic, "SoulRise", new Vector3(-3.26f, -1.92f, -0.05191165f), 1.0f, 0.1f, Color.magenta, "SoulRise"),
                    new AttackData("Dark Spike", 23, "Shadow piercing magic", "Attack1",
                                  AttackType.Magic, "BloodSpike", new Vector3(-3.25f, -2.25f, -0.05191165f), 1.2f, 0.1f, Color.red, "BloodSpike"),
                    new AttackData("Curse Bolt", 27, "Hex projectile", "Attack1",
                                  AttackType.Magic, "RedLightning", new Vector3(-3.33f, -1.7f, -0.05191165f), 1.4f, 0.15f, Color.red, "ThunderBolt"),
                    new AttackData("Death Vortex", 32, "Soul-consuming whirlwind", "Attack1",
                                  AttackType.AreaEffect, "BloodTornado", new Vector3(-3.27f, -0.97f, -0.05191165f), 1.8f, 0.2f, Color.black, "Hurricane")
                };

            case "Crystal":
                return new List<AttackData>
                {
                    new AttackData("Crystal Shard", 18, "Sharp gem projectile", "Attack1",
                                  AttackType.Projectile, "None", new Vector3(0f, 0f, 0f), 0.9f, 0.1f, Color.cyan),
                    new AttackData("Prism Strike", 22, "Refracted light attack", "Attack1",
                                  AttackType.Magic, "PhantomShatter", new Vector3(-3.19f, -1.47f, -0.03036325f), 1.1f, 0.1f, Color.cyan),
                    new AttackData("Gem Burst", 26, "Explosive crystal formation", "Attack3",
                                  AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.4f, 0.15f, Color.cyan),
                    new AttackData("Diamond Storm", 32, "Devastating crystal rain", "Special",
                                  AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.7f, 0.2f, Color.white)
                };

            case "Ground":
                return new List<AttackData>
                {
                    new AttackData("Rock Punch", 16, "Solid earth strike", "Attack1",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockPunch"),
                    new AttackData("Boulder Slam", 20, "Heavy stone impact", "Attack2",
                                  AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.3f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/DoubleRockPunch"),
                    new AttackData("Earth Quake", 28, "Ground-shaking tremor", "Attack3",
                                  AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.6f, 0.2f, new Color(0.6f, 0.3f, 0.1f), "Ground/RealGroundAttack3"),
                    new AttackData("Mountain Crusher", 34, "Catastrophic earth force", "Special",
                                  AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 2.0f, 0.25f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockSpecial")
                };

            default:
                return new List<AttackData>
                {
                    new AttackData("Basic Attack", 15, "Simple strike", "Attack1",
                                  AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                    new AttackData("Power Attack", 18, "Enhanced strike", "Attack2",
                                  AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 1.0f, 0.1f, Color.red),
                    new AttackData("Combo Attack", 22, "Chain strike", "Attack3",
                                  AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 1.2f, 0.1f, Color.red),
                    new AttackData("Ultimate Attack", 28, "Finishing move", "Special",
                                  AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 1.5f, 0.15f, Color.yellow)
                };
        }
    }

    // Get effect prefab based on attack type
    public GameObject GetEffectPrefabForAttack(AttackData attack)
    {
        if (!string.IsNullOrEmpty(attack.effectPrefabName))
        {
            GameObject customEffect = Resources.Load<GameObject>("Effects/" + attack.effectPrefabName);
            if (customEffect != null)
                return customEffect;
        }

        GameObject defaultEffect = Resources.Load<GameObject>("Effects/" + "BloodSpike");
        return defaultEffect;
    }

    public Color GetColorForAttackType(AttackType type)
    {
        switch (type)
        {
            case AttackType.Slash:
                return new Color(0.8f, 0.2f, 0.2f); // Red
            case AttackType.Projectile:
                return new Color(0.2f, 0.6f, 0.8f); // Blue
            case AttackType.Magic:
                return new Color(0.8f, 0.2f, 0.8f); // Purple
            case AttackType.AreaEffect:
                return new Color(0.8f, 0.8f, 0.2f); // Yellow
            case AttackType.DirectHit:
                return new Color(0.8f, 0.5f, 0.2f); // Orange
            case AttackType.MoveAndHit:
                return new Color(0.2f, 0.8f, 0.2f); // Green
            case AttackType.Heal:
                return new Color(0.2f, 0.8f, 0.6f); // Teal
            default:
                return Color.white;
        }
    }

    // Helper method to calculate total character power
    public float GetCharacterPowerRating(string characterName)
    {
        var attacks = GetAttacksForCharacter(characterName);
        var stats = GetCharacterStats(characterName);

        float avgDamage = 0f;
        foreach (var attack in attacks)
        {
            if (attack.attackType != AttackType.Heal)
                avgDamage += attack.damage;
        }
        avgDamage /= attacks.Count;

        float healthRating = stats.maxHealth / 100f;
        float defenseRating = 2f - stats.defenseModifier; // Lower defense modifier = higher rating
        float speedRating = 2f - stats.speedModifier; // Lower speed modifier = higher rating

        return (avgDamage * 0.4f) + (healthRating * 0.3f) + (defenseRating * 0.15f) + (speedRating * 0.15f);
    }
}