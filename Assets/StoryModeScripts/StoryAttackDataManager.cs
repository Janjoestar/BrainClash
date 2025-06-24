using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class StoryAttackDataManager : MonoBehaviour
{
    private static StoryAttackDataManager _instance;
    public static StoryAttackDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject attackSystemObj = new GameObject("StoryAttackSystem");
                _instance = attackSystemObj.AddComponent<StoryAttackDataManager>();
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

    public AttackData GetStartingAttackForCharacter(string characterName)
    {
        List<AttackData> allAttacks = GetAttacksForCharacter(characterName);
        if (allAttacks.Count > 0)
        {
            return allAttacks[0]; // The first attack in the list is the starting attack
        }
        return null;
    }

    public List<AttackData> GetUnlockableAttacksForCharacter(string characterName)
    {
        List<AttackData> allAttacks = GetAttacksForCharacter(characterName);
        if (allAttacks.Count > 1)
        {
            return allAttacks.Skip(1).ToList(); // All attacks except the first one are unlockable
        }
        return new List<AttackData>();
    }

    public List<AttackData> GetAttacksForCharacter(string characterName)
    {
        switch (characterName)
        {
            case "Knight":
                return new List<AttackData>
                {
                    new AttackData("Quick Slash", 15, "Fast sword strike.", "Attack1",
                                    AttackType.DirectHit, "Knight/Slash4", new Vector3(0.0f, 0.5f, 0.0f),
                                    0.1f, 0.8f, Color.red, sound: "Slash", critChance: 0.50f, accuracy: 1.0f, charName: "Knight"),
                    new AttackData("Warrior Slash", 30, "Fiery blade attack, risky but powerful.", "Attack1",
                                    AttackType.DirectHit, "Knight/Slash4", new Vector3(0.1f, 0.6f, -0.1f),
                                    0.1f, 0.8f, Color.red, sound: "WarriorSlash", critChance: 0f, accuracy: 0.8f, charName: "Knight"),
                    new AttackData("Dragon Slash", 40, "Powerful dragon strike, with some recoil.", "Attack2",
                                    AttackType.DirectHit, "Knight/Slash6", new Vector3(0.0f, 0.7f, 0.0f),
                                    0.1f, 0.8f, Color.red, sound: "DragonSlash", critChance: 0.15f, accuracy: 0.90f, doubleEdgeDamage: 10f, charName: "Knight"),
                    new AttackData("Sacrificial Blade", 80, "A final, desperate blow that may consume you entirely.", "Special",
                                    AttackType.DirectHit, "Knight/Slash4", new Vector3(0.0f, 0.0f, 0.0f),
                                    0.1f, 0.8f, Color.red, sound: "JudgementImpact", critChance: 0.05f, accuracy: 1.0f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.5f, charName: "Knight")
                };
            case "Archer":
                return new List<AttackData>
                {
                    new AttackData("Poison Arrow", 10, "Toxic projectile.", "Attack1",
                                    AttackType.Projectile, "Archer/Atk1", new Vector3(1.0f, 0.2f, 0f), // Offset from PLAYER for projectile spawn
                                    0.1f, 1f, Color.magenta, sound: "PoisonArrow", hitEffectName: "PoisonArrowHitEffect",
                                    hitOffset: new Vector3(0f, 0.5f, 0f), // CORRECTED: Changed targetHitOffset to hitOffset
                                    critChance: 0.40f, accuracy: 0.95f, charName: "Archer"),
                    new AttackData("Arrow Shower", 35, "Rain of arrows, some may graze you.", "Attack2",
                                    AttackType.AreaEffect, "Archer/Atk2", new Vector3(0f, 1.0f, 0f), // Effect spawns 1 unit above target
                                    0.1f, 2f, Color.red, sound: "ArrowShower", critChance: 0.10f, accuracy: 1.0f, doubleEdgeDamage: 10f, charName: "Archer"),
                    new AttackData("Impale Arrow", 25, "A sharp arrow thrust directly into the target. Very accurate.", "Attack3",
                                    AttackType.MoveAndHit, "Archer/Atk3", new Vector3(0.0f, 0.3f, 0.0f), // Effect at target when player is close
                                    0.1f, 1f, Color.white, sound: "None", critChance: 0.45f, accuracy: 1.0f, charName: "Archer"),
                    new AttackData("Green Beam", 60, "A focused beam of magical energy.", "Special",
                                    AttackType.Magic, "Archer/Special", new Vector3(1.0f, 0.2f, 0f), // Beam origin from player
                                    0.1f, 1.595f, Color.green, sound: "None", hitEffectName: "LargeExplosionHit",
                                    hitOffset: new Vector3(0f, 0f, 0f), // CORRECTED: Changed targetHitOffset to hitOffset
                                    critChance: 0.10f, accuracy: 0.60f, doubleEdgeDamage: 10f, charName: "Archer")
                };
            case "Water":
                return new List<AttackData>
                {
                    new AttackData("Aqua Slash", 20, "A swift slash imbued with water. Very accurate.", "Attack1",
                                    AttackType.MoveAndHit, "Water/Atk1", new Vector3(0.0f, 0.5f, 0f),
                                    0.1f, 0.8f, Color.cyan, sound: "None", critChance: 0.25f, accuracy: 1.0f, charName: "Water"),
                    new AttackData("Heal", 40, "Restore health.", "Attack2",
                                    AttackType.Heal, "Heal", new Vector3(0f, 1.0f, 0f),
                                    0.1f, 1.25f, Color.green, sound: "None", critChance: 0.00f, accuracy: 1.0f, charName: "Water"),
                    new AttackData("Water Dance", 45, "Flowing combo attack. Consistent damage.High chance for critical.", "Attack3",
                                    AttackType.MoveAndHit, "Water/Atk3", new Vector3(0.1f, 0.4f, 0f),
                                    0.2f, 1.7f, Color.blue, sound: "None", critChance: 0.30f, accuracy: 0.70f, charName: "Water"),
                    new AttackData("Water Ball", 100, "Liquid projectile. Good chance for critical.", "Special",
                                    AttackType.Projectile, "Water/Atk2", new Vector3(1.0f, 0.2f, 0f), // Projectile spawn offset from player
                                    0.1f, 1.25f, Color.blue, sound: "None", hitEffectName: "WaterBallHitEffect", // Assuming a hit effect
                                    hitOffset: new Vector3(0f, 0.5f, 0f), // CORRECTED: Changed targetHitOffset to hitOffset
                                    critChance: 0.40f, accuracy: 0.45f, charName: "Water")
                };
            case "Fire":
                return new List<AttackData>
                {
                    new AttackData("Fire Slash", 25, "Burns enemy, but scorches you significantly.", "Attack1",
                                   AttackType.MoveAndHit, "FireSlash", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.red, sound: "None", critChance: 0.15f, accuracy: 1f, doubleEdgeDamage: 10f, charName: "Fire"),
                    new AttackData("Spin Slash", 30, "Spinning flame attack. High crit potential.", "Attack2",
                                   AttackType.MoveAndHit, "FireSpin", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.red, sound: "None", critChance: 0.40f, accuracy: 0.70f, charName: "Fire"),
                    new AttackData("Fire Combo", 50, "Multi-hit flames. Good accuracy. Burns the user.", "Attack3",
                                   AttackType.MoveAndHit, "FireSpin", new Vector3(0.0f, 0.5f, 0f), 0.38f, 0.9f, Color.red, sound: "None", critChance: 0.05f, accuracy: 0.90f, doubleEdgeDamage: 30, charName: "Fire"),
                    new AttackData("Inferno Sacrifice", 100, "Unleashes a massive inferno, but at a terrible cost to yourself.", "Special",
                                   AttackType.AreaEffect, "LargeFireExplosion", new Vector3(0f, 0.5f, 0f), 0.2f, 2f, Color.red, sound: "None", critChance: 0.0f, accuracy: 0.8f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.60f, charName: "Fire")
                };
            case "Wind":
                return new List<AttackData>
                {
                    new AttackData("Wind Slash", 25, "Cutting air blade. High Accuracy.", "Attack1",
                                   AttackType.MoveAndHit, "Wind/Atk1", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, new Color(0.659f, 0.592f, 0.447f), sound: "None", critChance: 0.20f, accuracy: 0.95f, charName: "Wind"),
                    new AttackData("Wind Barrage", 30, "Multiple air strikes, with strong backlash.", "Attack2",
                                   AttackType.MoveAndHit, "Wind/Atk2", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1.2f, new Color(0.659f, 0.592f, 0.447f), sound: "None", critChance: 0.25f, accuracy: 0.80f, doubleEdgeDamage: 10f, charName: "Wind"),
                    new AttackData("Tornado", 25, "Whirling vortex. Low accuracy, but high crit chance.", "Attack3",
                                   AttackType.DirectHit, "Wind/WindTornado", new Vector3(0.0f, 0.5f, 0f), 0.15f, 1.5f, new Color(0.659f, 0.592f, 0.447f), sound: "None", critChance: 0.60f, accuracy: 0.65f, charName: "Wind"),
                    new AttackData("Tempest Collapse", 75, "Summons a devastating tempest.", "Special",
                                   AttackType.AreaEffect, "Wind/Special", new Vector3(0.0f, 0.5f, 0f), 0.25f, 1.5f, new Color(0.659f, 0.592f, 0.447f), sound: "None", critChance: 0.0f, accuracy: 0.50f, doubleEdgeDamage: 10f, charName: "Wind")
                };
            case "Necromancer":
                return new List<AttackData>
                {
                    new AttackData("Soul Rise", 20, "Summon spirits. Reliable damage.", "Attack1",
                                   AttackType.DirectHit, "SoulRise", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.red, sound: "SoulRise", critChance: 0.05f, accuracy: 0.90f, charName: "Necromancer"),
                    new AttackData("Blood Spike", 30, "Piercing blood magic with a high crit chance.", "Attack1",
                                   AttackType.DirectHit, "BloodSpike", new Vector3(0.0f, -0.5f, 0f), 0.1f, 1.2f, Color.red, sound: "BloodSpike", critChance: 0.35f, accuracy: 0.85f, doubleEdgeDamage: 10f, charName: "Necromancer"),
                    new AttackData("Red Lightning", 50, "Dark thunder strike, drawing heavily from your own life force.", "Attack1",
                                   AttackType.DirectHit, "ThunderBolt", new Vector3(0.0f, 0.5f, 0f), 0.15f, 1.5f, Color.red, sound: "RedLightning", critChance: 0.30f, accuracy: 0.60f, doubleEdgeDamage: 30f, charName: "Necromancer"),
                    new AttackData("Final Offering", 150, "Sacrifices all life energy to unleash a cataclysmic curse, with a high chance of self-destruction.", "Attack1",
                                   AttackType.AreaEffect, "Hurricane", new Vector3(0.0f, 0.5f, 0f), 0.25f, 1.5f, Color.red, sound: "BloodTornado", critChance: 0.0f, accuracy: 1f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.85f, charName: "Necromancer")
                };
            case "Crystal":
                return new List<AttackData>
                {
                    new AttackData("Crystal Crusher", 20, "Shattering punch. Good accuracy.", "Attack1",
                                   AttackType.AreaEffect, "Crystal/Atk1", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.cyan, sound: "None", critChance: 0.25f, accuracy: 0.95f, charName: "Crystal"),
                    new AttackData("Crystal Hammer", 25, "A massive crystal hammer falls from the sky. Very high crit chance", "Attack2",
                                   AttackType.DirectHit, "Crystal/Atk2", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1.2f, Color.cyan, sound: "PhantomShatter", critChance: 0.40f, accuracy: 0.75f, charName: "Crystal"),
                    new AttackData("Crystal Eruption", 45, "Gem explosion, shards may cut you significantly.", "Attack3",
                                   AttackType.DirectHit, "Crystal/Atk3", new Vector3(0.0f, 0.5f, 0f), 0.15f, 1.6f, Color.cyan, sound: "None", critChance: 0.05f, accuracy: 0.80f, doubleEdgeDamage: 20f, charName: "Crystal"),
                    new AttackData("Prismatic Overload", 65, "Channels immense crystal energy.", "Special",
                                   AttackType.AreaEffect, "Crystal/Special", new Vector3(0.0f, 0.5f, 0f), 0.15f, 1.65f, Color.cyan, sound: "None", critChance: 0.0f, accuracy: 0.55f, doubleEdgeDamage: 10f, charName: "Crystal")
                };
            case "Ground":
                return new List<AttackData>
                {
                    new AttackData("Quick Punch", 20, "Fast earth strike. High Accuracy", "Attack1",
                                   AttackType.MoveAndHit, "Ground/RockPunch", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1f, new Color(0.6f, 0.3f, 0.1f), sound: "None", critChance: 0.15f, accuracy: 0.95f, charName: "Ground"),
                    new AttackData("Punch Combo", 35, "Multiple earth hits, causing minor tremors to yourself. High crit chance", "Attack2",
                                   AttackType.MoveAndHit, "Ground/DoubleRockPunch", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1f, new Color(0.6f, 0.3f, 0.1f), sound: "None", critChance: 0.30f, accuracy: 0.80f, doubleEdgeDamage: 10f, charName: "Ground"),
                    new AttackData("Rock Slide", 50, "Falling boulder. Riskier, but can be worth it.", "Attack3",
                                   AttackType.MoveAndHit, "Ground/RealGroundAttack3", new Vector3(0.0f, 0.5f, 0f), 0.35f, 1f, new Color(0.6f, 0.3f, 0.1f), sound: "None", critChance: 0.15f, accuracy: 0.60f, doubleEdgeDamage: 20f, charName: "Ground"),
                    new AttackData("Titanic Reckoning", 100, "Shatters the earth with suicidal force, potentially crushing yourself.", "Special",
                                   AttackType.AreaEffect, "Ground/RockSpecial", new Vector3(0.0f, 0.5f, 0f), 0.25f, 1.8f, new Color(0.6f, 0.3f, 0.1f), sound: "None", critChance: 0.0f, accuracy: 1.0f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.5f, charName: "Ground")
                };
            default:
                return new List<AttackData>
                {
                    new AttackData("Basic Attack", 10, "Simple strike.", "Attack1",
                                    AttackType.DirectHit, "", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "Slash", critChance: 0.0f, accuracy: 0.95f, charName: "Default"),
                    new AttackData("Special Attack", 15, "Enhanced strike. Balanced.", "Attack2",
                                    AttackType.DirectHit, "", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "WarriorSlash", critChance: 0.05f, accuracy: 0.90f, charName: "Default"),
                    new AttackData("Power Strike", 25, "Strong attack with some recoil.", "Attack1",
                                    AttackType.DirectHit, "", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "Slash", critChance: 0.0f, accuracy: 0.85f, doubleEdgeDamage: 10f, charName: "Default"),
                    new AttackData("Ultimate Attack", 60, "A devastating, yet risky, ultimate.", "Attack2",
                                    AttackType.DirectHit, "", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "WarriorSlash", critChance: 0.15f, accuracy: 0.75f, doubleEdgeDamage: 0f, charName: "Default")
                };
        }
    }

    // In StoryAttackDataManager.cs -> GetAttacksForEnemy method

    public List<AttackData> GetAttacksForEnemy(string enemyName)
    {
        switch (enemyName)
        {
            case "FlyingEye":
                return new List<AttackData> {
                new AttackData("Bite", 10, "A piercing bite that slightly damages the target.", "Attack1",
                    AttackType.MoveAndHit, "None", Vector3.zero, 0.1f, 0.1f, Color.red, sound: "Knight/Slash4", accuracy: 1f, maxCooldown: 0),

                new AttackData("Terrifying Scream", 5, "A scream that instills fear, lowering player's accuracy.", "Attack3",
                    AttackType.DirectHit, "Effects/ScreamEffect", Vector3.zero, 0.1f, 0.1f, Color.yellow, sound: "Knight/Slash4", accuracy: 1.0f, maxCooldown: 4,
                    effects: new List<StatusEffect> { new StatusEffect { effectType = StatusEffectType.AccuracyDown, value = 0.15f, duration = 2, isBuff = false } }),

                new AttackData("Dive Bomb", 25, "Dives at the player for heavy damage.", "Attack2",
                    AttackType.MoveAndHit, "None", Vector3.zero, 0.1f, 0.1f, Color.red, sound: "Knight/Slash4", accuracy: 0.8f, maxCooldown: 3)
            };
            case "Slime":
                return new List<AttackData> {
                new AttackData("Slime Swipe", 8, "A weak, sticky swipe.", "Attack1",
                    AttackType.MoveAndHit, "None", new Vector3(0, 0.7f, 0), 0.1f, 0.1f, Color.blue, sound: "Knight/Slash4", accuracy: 1f, maxCooldown: 0),

                new AttackData("Corrosive Spit", 12, "Spits acid that lowers player's damage.", "Attack2",
                    AttackType.MoveAndHit, "Effects/SlimeProjectile", new Vector3(0, 0.5f, 0), 0.2f, 0.1f, Color.blue, sound: "Knight/Slash4", accuracy: 1f, maxCooldown: 3,
                    hitEffectName: "Effects/AcidHit",
                    effects: new List<StatusEffect> { new StatusEffect { effectType = StatusEffectType.DamageDown, value = 0.2f, duration = 2, isBuff = false } }),

                new AttackData("Engulf", 0, "Consumes ambient magic to heal itself.", "Attack2",
                    AttackType.Heal, "None", new Vector3(0, 0.5f, 0), 0.2f, 0.1f, Color.green, sound: "Heal", accuracy: 1.0f, maxCooldown: 4)
            };
            default:
                return new List<AttackData> {
                new AttackData("Tackle", 8, "A basic physical attack.", "Attack1", AttackType.MoveAndHit, "Effects/Slash4", Vector3.zero, 0.1f, 0.1f, Color.white, sound: "Slash", accuracy: 0.9f, maxCooldown: 0)
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
        return null;
    }
}