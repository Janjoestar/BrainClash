using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    Slash,          // Melee slash effects that appear near the target
    Projectile,     // Effects that travel from attacker to defender
    Magic,          // Magical projectiles that travel
    AreaEffect,     // Effects that appear on or around the target (explosions, etc.)
    DirectHit,      // Effects that appear directly on the target (hammer hit, etc.)
    MoveAndHit,     // Character moves to target, attacks, then returns to position
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
    public float doubleEdgeDamage = 0f;
    public bool canSelfKO = false;
    public float selfKOFailChance = 0.0f;

    public float critChance = 0.0f;
    public float accuracy = 1f;

    public AttackData(string name, float dmg, string desc, string animTrigger,
                      AttackType type, string effectName, Vector3 offset, float delay,
                      float flashInterval, Color flashColor, string sound = "",
                      string hitEffectName = "", Vector3 hitOffset = default,
                      float critChance = 0.0f, float accuracy = 0.85f, float doubleEdgeDamage = 0f,
                      bool canSelfKO = false, float selfKOFailChance = 0.0f)
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
        this.critChance = critChance;
        this.accuracy = accuracy;
        this.doubleEdgeDamage = doubleEdgeDamage;
        this.canSelfKO = canSelfKO;
        this.selfKOFailChance = selfKOFailChance;
    }

    // Backward compatibility constructor
    public AttackData(string name, int dmg, string desc, string animTrigger, AttackType type)
        : this(name, (float)dmg, desc, animTrigger, type, "", Vector3.zero, 0.3f, 0.1f, Color.red)
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

    public List<AttackData> GetAttacksForCharacter(string characterName)
    {
        switch (characterName)
        {
            case "Knight":
                return new List<AttackData>
                {
                    new AttackData("Quick Slash", 8, "Fast sword strike.", "Attack1",
                                   AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash4", "", default, 0.0f, 0.98f),
                    new AttackData("Warrior Slash", 15, "Fiery blade attack, risky but powerful.", "Attack1",
                                   AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash4", "", default, 0.30f, 0.75f),
                    new AttackData("Dragon Slash", 22, "Powerful dragon strike, with some recoil.", "Attack2",
                                   AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash6", "", default, 0.10f, 0.90f, 15f),
                    new AttackData("Sacrificial Blade", 80, "A final, desperate blow that may consume you entirely.", "Special",
                                   AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash4", "", default, 0.10f, 0.75f, 0f, true, 0.5f)
                };
            case "Archer":
                return new List<AttackData>
                {
                    new AttackData("Poison Arrow", 10, "Toxic projectile.", "Attack1",
                                   AttackType.Projectile, "PoisonArrow", new Vector3(1.01f, -3.7f, -4.116615f), 1f, 0.1f, Color.magenta, "Archer/Atk1", "PoisonArrowHitEffect", new Vector3(-2.43f,-3.291f,0.1f), 0.0f, 0.95f),
                    new AttackData("Arrow Shower", 20, "Rain of arrows, some may graze you.", "Attack2",
                                   AttackType.AreaEffect, "ArrowShower", new Vector3(-3.2f, -3.46f, -4.116615f), 2f, 0.1f, Color.red, "Archer/Atk2", "", default, 0.10f, 0.85f, 18f),
                    new AttackData("Impale Arrow", 16, "A sharp arrow thrust directly into the target. Very accurate.", "Attack3",
                                   AttackType.DirectHit, "None", new Vector3(-2.16f, -2.62f, -4.116615f), 1.595f, 0.1f, Color.white, "Archer/Atk3", "", default, 0.05f, 0.98f),
                    new AttackData("Green Beam", 60, "A focused beam of magical energy.", "Special",
                                   AttackType.Magic, "None", new Vector3(0f, 0f, 0f), 1.595f, 0.1f, Color.green, "Archer/Special", "LargeExplosionHit", default, 0.15f, 0.80f, 0f, false, 0.0f)
                };
            case "Water":
                return new List<AttackData>
                {
                    new AttackData("Aqua Slash", 15, "A swift slash imbued with water.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.cyan, "Water/Atk1", "", default, 0.05f, 0.95f),
                    new AttackData("Heal", 25, "Restore health.", "Attack2",
                                   AttackType.Heal, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, 0.1f, Color.green, "Heal", "", default, 0.00f, 1.0f),
                    new AttackData("Water Dance", 30, "Flowing combo attack. Consistent damage.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.7f, 0.2f, Color.blue, "Water/Atk3", "", default, 0.1f, 0.55f),
                    new AttackData("Water Ball", 20, "Liquid projectile. Good chance for critical.", "Special",
                                   AttackType.Projectile, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.25f, 0.1f, Color.blue, "Water/Atk2", "", default, 0.40f, 0.70f)
                };
            case "Fire":
                return new List<AttackData>
                {
                    new AttackData("Fire Slash", 14, "Burns enemy, but scorches you significantly.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.red, "FireSlash", "", default, 0.15f, 0.90f, 25f),
                    new AttackData("Spin Slash", 20, "Spinning flame attack. High crit potential.", "Attack2",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.2f, Color.red, "FireSpin", "", default, 0.35f, 0.70f),
                    new AttackData("Fire Combo", 28, "Multi-hit flames. Good accuracy.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.9f, 0.38f, Color.red, "FireSpin", "", default, 0.05f, 0.95f),
                    new AttackData("Inferno Sacrifice", 70, "Unleashes a massive inferno, but at a terrible cost to yourself.", "Special",
                                   AttackType.AreaEffect, "FireCharge", new Vector3(0f, 0f, 0f), 2f, 0.2f, Color.red, "LargeFireExplosion", "", default, 0.0f, 0.65f, 0f, true, 0.65f)
                };
            case "Wind":
                return new List<AttackData>
                {
                    new AttackData("Wind Slash", 12, "Cutting air blade.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, new Color(0.659f, 0.592f, 0.447f), "Wind/Atk1", "", default, 0.0f, 0.98f),
                    new AttackData("Wind Barrage", 20, "Multiple air strikes, with strong backlash.", "Attack2",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.2f, 0.1f, new Color(0.659f, 0.592f, 0.447f), "Wind/Atk2", "", default, 0.10f, 0.90f, 20f),
                    new AttackData("Tornado", 38, "Whirling vortex. Low accuracy, but high crit chance.", "Attack3",
                                   AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.15f, new Color(0.659f, 0.592f, 0.447f), "Wind/WindTornado", "", default, 0.25f, 0.65f),
                    new AttackData("Tempest Collapse", 65, "Summons a devastating tempest.", "Special",
                                   AttackType.AreaEffect, "WindTornado", new Vector3(0f, 0f, 0f), 1.5f, 0.25f, new Color(0.659f, 0.592f, 0.447f), "Wind/Special", "", default, 0.0f, 0.70f, 0f)
                };
            case "Necromancer":
                return new List<AttackData>
                {
                    new AttackData("Soul Rise", 20, "Summon spirits. Reliable damage.", "Attack1",
                                   AttackType.DirectHit, "SoulRise", new Vector3(-3.26f, -1.92f, -0.05191165f), 0.8f, 0.1f, Color.red, "SoulRise", "", default, 0.05f, 0.90f),
                    new AttackData("Blood Spike", 18, "Piercing blood magic, drawing heavily from your own life force.", "Attack1",
                                   AttackType.DirectHit, "BloodSpike", new Vector3(-3.25f, -2.25f, -0.05191165f), 1.2f, 0.1f, Color.red, "BloodSpike", "", default, 0.25f, 0.90f, 30f),
                    new AttackData("Red Lightning", 38, "Dark thunder strike. High crit potential.", "Attack1",
                                   AttackType.DirectHit, "RedLightning", new Vector3(-3.33f, -1.7f, -0.05191165f), 1.5f, 0.15f, Color.red, "ThunderBolt", "", default, 0.30f, 0.60f),
                    new AttackData("Final Offering", 90, "Sacrifices all life energy to unleash a cataclysmic curse, with a high chance of self-destruction.", "Attack1",
                                   AttackType.AreaEffect, "BloodTornado", new Vector3(-3.27f, -0.97f, -0.05191165f), 1.5f, 0.25f, Color.red, "Hurricane", "", default, 0.0f, 0.50f, 0f, true, 0.85f)
                };
            case "Crystal":
                return new List<AttackData>
                {
                    new AttackData("Crystal Crusher", 20, "Shattering punch. Good accuracy.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.cyan, "Crystal/Atk1", "", default, 0.05f, 0.95f),
                    new AttackData("Crystal Hammer", 10, "A massive crystal hammer falls from the sky.", "Attack2",
                                   AttackType.DirectHit, "PhantomShatter", new Vector3(-3.19f, -1.47f, -0.03036325f), 1.2f, 0.1f, Color.cyan, "Crystal/Atk2", "", default, 0.40f, 0.75f),
                    new AttackData("Crystal Eruption", 35, "Gem explosion, shards may cut you significantly.", "Attack3",
                                   AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.6f, 0.15f, Color.cyan, "Crystal/Atk3", "", default, 0.05f, 0.80f, 22f),
                    new AttackData("Prismatic Overload", 75, "Channels immense crystal energy.", "Special",
                                   AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.65f, 0.15f, Color.cyan, "Crystal/Special", "", default, 0.0f, 0.60f, 0f)
                };
            case "Ground":
                return new List<AttackData>
                {
                    new AttackData("Quick Punch", 10, "Fast earth strike.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockPunch", "", default, 0.0f, 0.98f),
                    new AttackData("Punch Combo", 20, "Multiple earth hits, causing minor tremors to yourself.", "Attack2",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/DoubleRockPunch", "", default, 0.10f, 0.90f, 12f),
                    new AttackData("Rock Slide", 30, "Falling boulder. Riskier, but can crit big.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.35f, new Color(0.6f, 0.3f, 0.1f), "Ground/RealGroundAttack3", "", default, 0.20f, 0.70f),
                    new AttackData("Titanic Reckoning", 95, "Shatters the earth with suicidal force, potentially crushing yourself.", "Special",
                                   AttackType.AreaEffect, "Ground/RockSpecial", new Vector3(0f, 0f, 0f), 1.8f, 0.25f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockSpecial", "", default, 0.0f, 0.55f, 0f, true, 0.9f)
                };
            default:
                return new List<AttackData>
                {
                    new AttackData("Basic Attack", 10, "Simple strike.", "Attack1",
                                   AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.0f, 0.95f),
                    new AttackData("Special Attack", 15, "Enhanced strike. Balanced.", "Attack2",
                                   AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.05f, 0.90f),
                    new AttackData("Power Strike", 25, "Strong attack with some recoil.", "Attack1",
                                   AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.0f, 0.85f, 10f),
                    new AttackData("Ultimate Attack", 60, "A devastating, yet risky, ultimate.", "Attack2",
                                   AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.15f, 0.75f, 0f)
                };
        }
    }

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
                return new Color(0.8f, 0.2f, 0.2f);
            case AttackType.Projectile:
                return new Color(0.2f, 0.6f, 0.8f);
            case AttackType.Magic:
                return new Color(0.8f, 0.2f, 0.8f);
            case AttackType.AreaEffect:
                return new Color(0.8f, 0.8f, 0.2f);
            case AttackType.DirectHit:
                return new Color(0.8f, 0.5f, 0.2f);
            case AttackType.MoveAndHit:
                return new Color(0.2f, 0.8f, 0.2f);
            case AttackType.Heal:
                return new Color(0.2f, 0.8f, 0.6f);
            default:
                return Color.white;
        }
    }
}