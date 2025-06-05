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
    public string hitEffectPrefabName; // Prefab for hit effect animation
    public Vector3 targetHitOffset;    // Where to position hit effects on target
    public float flashInterval;        // Time between flashes for defense indicators
    public Color flashColor;
    public string soundEffectName;     // Sound effect file name (without extension)

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

    // Get attacks for a specific character
    // Replace the GetAttacksForCharacter method in your AttackDataManager with this updated version
    // that has proper short descriptions for the hover tooltip:

    public List<AttackData> GetAttacksForCharacter(string characterName)
    {
        switch (characterName)
        {
            case "Knight":
                return new List<AttackData>
            {
                new AttackData("Quick Slash", 12, "Fast sword strike", "Attack1",
                              AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Warrior Slash", 15, "Fiery blade attack", "Attack1",
                              AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Dragon Slash", 20, "Powerful dragon strike", "Attack2",
                              AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Judgement Impact", 20, "Divine punishment", "Special",
                              AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
            };
            case "Archer":
                return new List<AttackData>
            {
                new AttackData("Poison Arrow", 15, "Toxic projectile", "Attack1",
                              AttackType.Projectile, "PoisonArrow", new Vector3(1.01f, -3.7f, -4.116615f), 1f, 0.1f, Color.magenta, "SoundEffect", "PoisonArrowHitEffect", new Vector3(-2.43f,-3.291f,0.1f)),
                new AttackData("Arrow Shower", 20, "Rain of arrows", "Attack2",
                              AttackType.DirectHit, "ArrowShower", new Vector3(-3.2f, -3.46f, -4.116615f), 2f, 0.1f, Color.red),
                new AttackData("Green Beam", 15, "Magical energy beam", "Special",
                              AttackType.DirectHit, "None", new Vector3(-2.16f, -2.62f, -4.116615f), 1.595f, 0.1f, Color.green),
                new AttackData("Green Beam", 15, "Magical energy beam", "Special",
                              AttackType.DirectHit, "None", new Vector3(-2.16f, -2.62f, -4.116615f), 1.595f, 0.1f, Color.green)
            };
            case "Water":
                return new List<AttackData>
            {
                new AttackData("Heal", 20, "Restore health", "Attack2",
                              AttackType.Heal, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, 0.1f, Color.green, "Heal"),
                new AttackData("Water Ball", 25, "Liquid projectile", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, 0.1f, Color.blue),
                new AttackData("Water Dance", 30, "Flowing combo attack", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.7f, 0.2f, Color.blue),
                new AttackData("Water Dance", 30, "Flowing combo attack", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.7f, 0.2f, Color.blue)
            };
            case "Samurai":
                return new List<AttackData>
            {
                new AttackData("Shield Bash", 14, "Defensive strike", "Attack1",
                              AttackType.DirectHit, "ShieldBash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Holy Strike", 19, "Sacred blade attack", "Attack2",
                              AttackType.DirectHit, "HolyStrike", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Crusader's Charge", 24, "Rush attack", "Attack3",
                              AttackType.DirectHit, "CrusaderCharge", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Crusader's Charge", 24, "Rush attack", "Attack3",
                              AttackType.DirectHit, "CrusaderCharge", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red)
            };
            case "Fire":
                return new List<AttackData>
            {
                new AttackData("Fire Slash", 14, "Burns enemy", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.red, "FireSlash"),
                new AttackData("Spin Slash", 20, "Spinning flame attack", "Attack2",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.2f, Color.red, "FireSpin"),
                new AttackData("Fire Combo", 25, "Multi-hit flames", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.9f, 0.38f, Color.red, "FireSpin"),
                new AttackData("Fire Slam", 40, "Explosive impact", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 2f, 0.2f, Color.red, "FireCharge")
            };
            case "Wind":
                return new List<AttackData>
            {
                new AttackData("Wind Slash", 20, "Cutting air blade", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, new Color(0.659f, 0.592f, 0.447f)),
                new AttackData("Wind Barrage", 20, "Multiple air strikes", "Attack2",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.2f, 0.1f, new Color(0.659f, 0.592f, 0.447f)),
                new AttackData("Tornado", 35, "Whirling vortex", "Attack3",
                              AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.15f, new Color(0.659f, 0.592f, 0.447f), "WindTornado"),
                new AttackData("Teleporting Slash", 45, "Instant strike", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.25f, new Color(0.659f, 0.592f, 0.447f))
            };
            case "Necromancer":
                return new List<AttackData>
            {
                new AttackData("Soul Rise", 20, "Summon spirits", "Attack1",
                              AttackType.DirectHit, "SoulRise", new Vector3(-3.26f, -1.92f, -0.05191165f), 0.8f, 0.1f, Color.red, "SoulRise"),
                new AttackData("Blood Spike", 20, "Piercing blood magic", "Attack1",
                              AttackType.DirectHit, "BloodSpike", new Vector3(-3.25f, -2.25f, -0.05191165f), 1.2f, 0.1f, Color.red, "BloodSpike"),
                new AttackData("Red Lightning", 35, "Dark thunder strike", "Attack1",
                              AttackType.DirectHit, "RedLightning", new Vector3(-3.33f, -1.7f, -0.05191165f), 1.5f, 0.15f, Color.red, "ThunderBolt"),
                new AttackData("Blood Tornado", 45, "Crimson whirlwind", "Attack1",
                              AttackType.DirectHit, "BloodTornado", new Vector3(-3.27f, -0.97f, -0.05191165f), 1.5f, 0.25f, Color.red, "Hurricane")
            };
            case "Crystal":
                return new List<AttackData>
            {
                new AttackData("Crystal Crusher", 20, "Shattering punch", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.cyan),
                new AttackData("Phantom Shatter", 20, "Spectral crystal", "Attack1",
                              AttackType.DirectHit, "PhantomShatter", new Vector3(-3.19f, -1.47f, -0.03036325f), 1.2f, 0.1f, Color.cyan),
                new AttackData("Crystal Eruption", 35, "Gem explosion", "Attack3",
                              AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.6f, 0.15f, Color.cyan),
                new AttackData("Crystalline Surge", 45, "Pure crystal wave", "Special",
                              AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.65f, 0.15f, Color.cyan)
            };
            case "Ground":
                return new List<AttackData>
            {
                new AttackData("Quick Punch", 15, "Fast earth strike", "Attack1",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockPunch"),
                new AttackData("Punch Combo", 20, "Multiple earth hits", "Attack2",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/DoubleRockPunch"),
                new AttackData("Rock Slide", 30, "Falling boulder", "Attack3",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.35f, new Color(0.6f, 0.3f, 0.1f), "Ground/RealGroundAttack3"),
                new AttackData("Rock Smash", 50, "Devastating impact", "Special",
                              AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.8f, 0.25f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockSpecial")
            };
            default:
                return new List<AttackData>
            {
                new AttackData("Basic Attack", 10, "Simple strike", "Attack1",
                              AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Special Attack", 15, "Enhanced strike", "Attack2",
                              AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Basic Attack", 10, "Simple strike", "Attack1",
                              AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red),
                new AttackData("Special Attack", 15, "Enhanced strike", "Attack2",
                              AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red)
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
        // Return a color based on attack type
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
}