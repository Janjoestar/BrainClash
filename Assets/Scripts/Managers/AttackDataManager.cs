using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// REMOVE these definitions from here. They are now in AttackDataTypes.cs
// public enum AttackType { ... }
// [System.Serializable] public class AttackData { ... }


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
                    new AttackData("Quick Slash", 15, "Fast sword strike.", "Attack1",
                                   AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash4", "", default, 0.50f, 1.0f),
                    new AttackData("Warrior Slash", 30, "Fiery blade attack, risky but powerful.", "Attack1",
                                   AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash4", "", default, 0f, 0.8f, maxCooldown: 2), // Added cooldown
                    new AttackData("Dragon Slash", 40, "Powerful dragon strike, with some recoil.", "Attack2",
                                   AttackType.DirectHit, "DragonSlash", new Vector3(-3.23f, -1.93f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash6", "", default, 0.15f, 0.90f, 10f, maxCooldown: 3), // Added cooldown
                    new AttackData("Sacrificial Blade", 80, "A final, desperate blow that may consume you entirely.", "Special",
                                   AttackType.DirectHit, "JudgementImpact", new Vector3(-2.81f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "Knight/Slash4", "", default, 0.05f, 1.0f, 0f, true, 0.5f, maxCooldown: 5) // Added cooldown
                };
            case "Archer":
                return new List<AttackData>
                {
                    new AttackData("Poison Arrow", 10, "Toxic projectile.", "Attack1",
                                   AttackType.Projectile, "PoisonArrow", new Vector3(1.01f, -3.7f, -4.116615f), 1f, 0.1f, Color.magenta, "Archer/Atk1", "PoisonArrowHitEffect", new Vector3(-2.43f,-3.291f,0.1f), 0.40f, 0.95f),
                    new AttackData("Arrow Shower", 35, "Rain of arrows, some may graze you.", "Attack2",
                                   AttackType.AreaEffect, "ArrowShower", new Vector3(-3.2f, -3.46f, -4.116615f), 2f, 0.1f, Color.red, "Archer/Atk2", "", default, 0.10f, 1.0f, 10f, maxCooldown: 3), // Added cooldown
                    new AttackData("Impale Arrow", 25, "A sharp arrow thrust directly into the target. Very accurate.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1f, 0.1f, Color.white, "Archer/Atk3", "", default, 0.45f, 1.0f),
                    new AttackData("Green Beam", 60, "A focused beam of magical energy.", "Special",
                                   AttackType.Magic, "None", new Vector3(0f, 0f, 0f), 1.595f, 0.1f, Color.green, "Archer/Special", "LargeExplosionHit", default, 0.10f, 0.60f, 10f, maxCooldown: 4) // Added cooldown
                };
            case "Water":
                return new List<AttackData>
                {
                    new AttackData("Aqua Slash", 20, "A swift slash imbued with water. Very accurate.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.cyan, "Water/Atk1", "", default, 0.25f, 1.0f),
                    new AttackData("Heal", 40, "Restore health.", "Attack2",
                                   AttackType.Heal, "None", new Vector3(1.01f, -3.7f, -4.116615f), 1.25f, 0.1f, Color.green, "Heal", "", default, 0.00f, 1.0f, maxCooldown: 2), // Added cooldown
                    new AttackData("Water Dance", 45, "Flowing combo attack. Consistent damage.High chance for critical.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.7f, 0.2f, Color.blue, "Water/Atk3", "", default, 0.30f, 0.70f, maxCooldown: 3), // Added cooldown
                    new AttackData("Water Ball", 100, "Liquid projectile. Good chance for critical.", "Special",
                                   AttackType.Projectile, "None", new Vector3(-3.2f, -3.46f, -4.116615f), 1.25f, 0.1f, Color.blue, "Water/Atk2", "", default, 0.40f, 0.45f, maxCooldown: 4) // Added cooldown
                };
            case "Fire":
                return new List<AttackData>
                {
                    new AttackData("Fire Slash", 25, "Burns enemy, but scorches you significantly.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.red, "FireSlash", "", default, 0.15f, 0.90f, 10f),
                    new AttackData("Spin Slash", 30, "Spinning flame attack. High crit potential.", "Attack2",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.2f, Color.red, "FireSpin", "", default, 0.40f, 0.70f, maxCooldown: 2), // Added cooldown
                    new AttackData("Fire Combo", 50, "Multi-hit flames. Good accuracy. Burns the user.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.9f, 0.38f, Color.red, "FireSpin", "", default, 0.05f, 0.90f, 30, maxCooldown: 3), // Added cooldown
                    new AttackData("Inferno Sacrifice", 100, "Unleashes a massive inferno, but at a terrible cost to yourself.", "Special",
                                   AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 2f, 0.2f, Color.red, "LargeFireExplosion", "", default, 0.0f, 0.8f, 0f, true, 0.60f, maxCooldown: 5) // Added cooldown
                };
            case "Wind":
                return new List<AttackData>
                {
                    new AttackData("Wind Slash", 25, "Cutting air blade. High Accuracy.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, new Color(0.659f, 0.592f, 0.447f), "Wind/Atk1", "", default, 0.20f, 0.95f),
                    new AttackData("Wind Barrage", 30, "Multiple air strikes, with strong backlash.", "Attack2",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1.2f, 0.1f, new Color(0.659f, 0.592f, 0.447f), "Wind/Atk2", "", default, 0.25f, 0.80f, 10f, maxCooldown: 2), // Added cooldown
                    new AttackData("Tornado", 25, "Whirling vortex. Low accuracy, but high crit chance.", "Attack3",
                                   AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.15f, new Color(0.659f, 0.592f, 0.447f), "Wind/WindTornado", "", default, 0.60f, 0.65f),
                    new AttackData("Tempest Collapse", 75, "Summons a devastating tempest.", "Special",
                                   AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.5f, 0.25f, new Color(0.659f, 0.592f, 0.447f), "Wind/Special", "", default, 0.0f, 0.50f, 0f, maxCooldown: 4) // Added cooldown
                };
            case "Necromancer":
                return new List<AttackData>
                {
                    new AttackData("Soul Rise", 20, "Summon spirits. Reliable damage.", "Attack1",
                                   AttackType.DirectHit, "SoulRise", new Vector3(-3.26f, -1.92f, -0.05191165f), 0.8f, 0.1f, Color.red, "SoulRise", "", default, 0.05f, 0.90f),
                    new AttackData("Blood Spike", 30, "Piercing blood magic with a high crit chance.", "Attack1",
                                   AttackType.DirectHit, "BloodSpike", new Vector3(-3.25f, -2.25f, -0.05191165f), 1.2f, 0.1f, Color.red, "BloodSpike", "", default, 0.35f, 0.85f, 10f, maxCooldown: 2), // Added cooldown
                    new AttackData("Red Lightning", 50, "Dark thunder strike, drawing heavily from your own life force.", "Attack1",
                                   AttackType.DirectHit, "RedLightning", new Vector3(-3.33f, -1.7f, -0.05191165f), 1.5f, 0.15f, Color.red, "ThunderBolt", "", default, 0.30f, 0.60f,30f, maxCooldown: 3), // Added cooldown
                    new AttackData("Final Offering", 150, "Sacrifices all life energy to unleash a cataclysmic curse, with a high chance of self-destruction.", "Attack1",
                                   AttackType.AreaEffect, "BloodTornado", new Vector3(-3.27f, -0.97f, -0.05191165f), 1.5f, 0.25f, Color.red, "Hurricane", "", default, 0.0f, 1f, 0f, true, 0.85f, maxCooldown: 5) // Added cooldown
                };
            case "Crystal":
                return new List<AttackData>
                {
                    new AttackData("Crystal Crusher", 20, "Shattering punch. Good accuracy.", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 0.8f, 0.1f, Color.cyan, "Crystal/Atk1", "", default, 0.25f, 0.95f),
                    new AttackData("Crystal Hammer", 25, "A massive crystal hammer falls from the sky. Very high crit chance", "Attack2",
                                   AttackType.DirectHit, "PhantomShatter", new Vector3(-3.19f, -1.47f, -0.03036325f), 1.2f, 0.1f, Color.cyan, "Crystal/Atk2", "", default, 0.40f, 0.75f, maxCooldown: 2),
                    new AttackData("Crystal Eruption", 45, "Gem explosion, shards may cut you significantly.", "Attack3",
                                   AttackType.DirectHit, "None", new Vector3(0f, 0f, 0f), 1.6f, 0.15f, Color.cyan, "Crystal/Atk3", "", default, 0.05f, 0.80f, 20f, maxCooldown: 3),
                    new AttackData("Prismatic Overload", 65, "Channels immense crystal energy.", "Special",
                                   AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.65f, 0.15f, Color.cyan, "Crystal/Special", "", default, 0.0f, 0.55f, 10f, maxCooldown: 4)
                };
            case "Ground":
                return new List<AttackData>
                {
                    new AttackData("Quick Punch", 20, "Fast earth strike. High Accuracy", "Attack1",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockPunch", "", default, 0.15f, 0.95f),
                    new AttackData("Punch Combo", 35, "Multiple earth hits, causing minor tremors to yourself. High crit chance", "Attack2",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.1f, new Color(0.6f, 0.3f, 0.1f), "Ground/DoubleRockPunch", "", default, 0.30f, 0.80f, 10f, maxCooldown: 2),
                    new AttackData("Rock Slide", 50, "Falling boulder. Riskier, but can be worth it.", "Attack3",
                                   AttackType.MoveAndHit, "None", new Vector3(0f, 0f, 0f), 1f, 0.35f, new Color(0.6f, 0.3f, 0.1f), "Ground/RealGroundAttack3", "", default, 0.15f, 0.60f, 20f, maxCooldown: 3),
                    new AttackData("Titanic Reckoning", 100, "Shatters the earth with suicidal force, potentially crushing yourself.", "Special",
                                   AttackType.AreaEffect, "None", new Vector3(0f, 0f, 0f), 1.8f, 0.25f, new Color(0.6f, 0.3f, 0.1f), "Ground/RockSpecial", "", default, 0.0f, 1.0f, 0f, true, 0.5f, maxCooldown: 5)
                };
            default:
                return new List<AttackData>
                {
                    new AttackData("Basic Attack", 10, "Simple strike.", "Attack1",
                                   AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.0f, 0.95f),
                    new AttackData("Special Attack", 15, "Enhanced strike. Balanced.", "Attack2",
                                   AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.05f, 0.90f, maxCooldown: 1),
                    new AttackData("Power Strike", 25, "Strong attack with some recoil.", "Attack1",
                                   AttackType.DirectHit, "Slash", new Vector3(-3.3f, -2.18f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.0f, 0.85f, 10f, maxCooldown: 2),
                    new AttackData("Ultimate Attack", 60, "A devastating, yet risky, ultimate.", "Attack2",
                                   AttackType.DirectHit, "WarriorSlash", new Vector3(-3.25f, -2.33f, -4.116615f), 0.8f, 0.1f, Color.red, "", "", default, 0.15f, 0.75f, 0f, maxCooldown: 3)
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

    public AttackData GetAttackByName(string attackName)
    {
        // Define all possible character names to iterate through
        List<string> characterNames = new List<string>
        {
            "Knight", "Archer", "Water", "Fire", "Wind", "Necromancer", "Crystal", "Ground", "Default"
        };

        foreach (string charName in characterNames)
        {
            List<AttackData> attacks = GetAttacksForCharacter(charName);
            AttackData foundAttack = attacks.FirstOrDefault(attack => attack.attackName == attackName);
            if (foundAttack != null)
            {
                return foundAttack;
            }
        }
        return null; // Return null if no attack with the given name is found
    }
}