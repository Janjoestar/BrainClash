﻿using System.Collections.Generic;
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
                        AttackType.DirectHit, "Slash", new Vector3(0.0f, 0.5f, 0.0f),
                        0.1f, 0.8f, Color.red, sound: "Knight/Slash4", critChance: 0.50f, accuracy: 1.0f, charName: "Knight", numberOfTargets: 1),
                    new AttackData("Warrior Slash", 30, "Fiery blade attack, risky but powerful.", "Attack1",
                        AttackType.DirectHit, "WarriorSlash", new Vector3(0.1f, 0.6f, -0.1f),
                        0.1f, 0.8f, Color.red, sound: "Knight/Slash4", critChance: 0f, accuracy: 0.8f, charName: "Knight", numberOfTargets: 1),
                    new AttackData("Dragon Slash", 40, "Powerful dragon strike, with some recoil.", "Attack2",
                        AttackType.DirectHit, "DragonSlash", new Vector3(0.0f, 0.7f, 0.0f),
                        0.1f, 0.8f, Color.red, sound: "Knight/Slash6", critChance: 0.15f, accuracy: 0.90f, doubleEdgeDamage: 10f, charName: "Knight", numberOfTargets: 1),
                    new AttackData("Sacrificial Blade", 80, "A final, desperate blow that may consume you entirely.", "Special",
                        AttackType.DirectHit, "JudgementImpact", new Vector3(0.0f, 0.0f, 0.0f),
                        0.1f, 0.8f, Color.red, sound: "Knight/Slash4", critChance: 0.05f, accuracy: 1.0f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.5f, charName: "Knight", numberOfTargets: 1)
                };
            case "Archer":
                return new List<AttackData>
                {
                    new AttackData("Poison Arrow", 10, "Toxic projectile.", "Attack1",
                        AttackType.DirectHit, "PoisonArrow", new Vector3(-1.5f, -1.5f, 0f),
                        0.1f, 1f, Color.magenta, sound: "Archer/Atk1", hitEffectName: "PoisonArrowHitEffect",
                        hitOffset: new Vector3(1.5f, -1.5f, 0f),
                        critChance: 0.40f, accuracy: 0.95f, charName: "Archer", numberOfTargets: 1),
                    new AttackData("Arrow Shower", 35, "Rain of arrows, some may graze you.", "Attack2",
                        AttackType.AreaEffect, "ArrowShower", new Vector3(0f, -1.3f, 0f),
                        0.1f, 2f, Color.red, sound: "Archer/Atk2", critChance: 0.10f, accuracy: 1.0f, doubleEdgeDamage: 10f, charName: "Archer", numberOfTargets: 99),
                    new AttackData("Impale Arrow", 25, "A sharp arrow thrust directly into the target. Very accurate.", "Attack3",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.3f, 0.0f),
                        0.1f, 1f, Color.white, sound: "Archer/Atk3", critChance: 0.45f, accuracy: 1.0f, charName: "Archer", numberOfTargets: 1),
                    new AttackData("Green Beam", 60, "A focused beam of magical energy.", "Special",
                        AttackType.MoveAndHit, "None", new Vector3(1.0f, 0.2f, 0f),
                        0.1f, 1.595f, Color.green, sound: "Archer/Special", hitEffectName: "None",
                        hitOffset: new Vector3(0f, 0f, 0f),
                        critChance: 0.10f, accuracy: 0.90f, doubleEdgeDamage: 10f, charName: "Archer", numberOfTargets: 1)
                };
            case "Water":
                return new List<AttackData>
                {
                    new AttackData("Aqua Slash", 20, "A swift slash imbued with water. Very accurate.", "Attack1",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f),
                        0.1f, 0.8f, Color.cyan, sound: "Water/Atk1", critChance: 0.25f, accuracy: 1.0f, charName: "Water", numberOfTargets: 1),
                    new AttackData("Heal", 40, "Restore health.", "Attack2",
                        AttackType.Heal, "None", new Vector3(0f, 1.0f, 0f),
                        0.1f, 1.25f, Color.green, sound: "Heal", critChance: 0.00f, accuracy: 1.0f, charName: "Water"),
                    new AttackData("Water Dance", 45, "Flowing combo attack. Consistent damage.High chance for critical.", "Attack3",
                        AttackType.MoveAndHit, "None", new Vector3(0.1f, 0.4f, 0f),
                        0.2f, 1.7f, Color.blue, sound: "Water/Atk3", critChance: 0.30f, accuracy: 0.70f, charName: "Water", numberOfTargets: 1),
                    new AttackData("Water Ball", 100, "Liquid projectile. Good chance for critical.", "Special",
                        AttackType.Projectile, "None", new Vector3(1.0f, 0.2f, 0f),
                        0.1f, 1.25f, Color.blue, sound: "Water/Atk2", hitEffectName: "",
                        hitOffset: new Vector3(0f, 0.5f, 0f),
                        critChance: 0.40f, accuracy: 0.45f, charName: "Water", numberOfTargets: 1)
                };
            case "Fire":
                return new List<AttackData>
                {
                    new AttackData("Fire Slash", 25, "Burns enemy, but scorches you significantly.", "Attack1",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.red, sound: "FireSlash", critChance: 0.15f, accuracy: 1f, doubleEdgeDamage: 10f, charName: "Fire", numberOfTargets: 1),
                    new AttackData("Spin Slash", 30, "Spinning flame attack. High crit potential.", "Attack2",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.red, sound: "FireSpin", critChance: 0.40f, accuracy: 0.70f, charName: "Fire", numberOfTargets: 1),
                    new AttackData("Fire Combo", 50, "Multi-hit flames. Good accuracy. Burns the user.", "Attack3",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.38f, 0.9f, Color.red, sound: "FireSpin", critChance: 0.05f, accuracy: 0.90f, doubleEdgeDamage: 30, charName: "Fire", numberOfTargets: 1),
                    new AttackData("Inferno Sacrifice", 100, "Unleashes a massive inferno, but at a terrible cost to yourself.", "Special",
                        AttackType.AreaEffect, "None", new Vector3(0f, 0.5f, 0f), 0.2f, 2f, Color.red, sound: "LargeFireExplosion", critChance: 0.0f, accuracy: 0.8f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.60f, charName: "Fire", numberOfTargets: 99)
                };
            case "Wind":
                return new List<AttackData>
                {
                    new AttackData("Wind Slash", 25, "Cutting air blade. High Accuracy.", "Attack1",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, new Color(0.659f, 0.592f, 0.447f), sound: "Wind/Atk1", critChance: 0.20f, accuracy: 0.95f, charName: "Wind", numberOfTargets: 1),
                    new AttackData("Wind Barrage", 30, "Multiple air strikes, with strong backlash.", "Attack2",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1.2f, new Color(0.659f, 0.592f, 0.447f), sound: "Wind/Atk2", critChance: 0.25f, accuracy: 0.80f, doubleEdgeDamage: 10f, charName: "Wind", numberOfTargets: 1),
                    new AttackData("Tornado", 25, "Whirling vortex. Low accuracy, but high crit chance.", "Attack3",
                        AttackType.DirectHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.15f, 1.5f, new Color(0.659f, 0.592f, 0.447f), sound: "Wind/WindTornado", critChance: 0.60f, accuracy: 0.65f, charName: "Wind", numberOfTargets: 1),
                    new AttackData("Tempest Collapse", 75, "Summons a devastating tempest.", "Special",
                        AttackType.AreaEffect, "None", new Vector3(0.0f, 0.5f, 0f), 0.25f, 1.5f, new Color(0.659f, 0.592f, 0.447f), sound: "Wind/Special", critChance: 0.0f, accuracy: 0.50f, doubleEdgeDamage: 0f, charName: "Wind", numberOfTargets: 99)
                };
            case "Necromancer":
                return new List<AttackData>
                {
                    new AttackData("Soul Rise", 20, "Summon spirits. Reliable damage.", "Attack1",
                        AttackType.DirectHit, "SoulRise", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0.8f, Color.red, sound: "SoulRise", critChance: 0.05f, accuracy: 0.90f, charName: "Necromancer", numberOfTargets: 1),
                    new AttackData("Blood Spike", 30, "Piercing blood magic with a high crit chance.", "Attack1",
                        AttackType.DirectHit, "BloodSpike", new Vector3(0.0f, -0.5f, 0f), 0.1f, 1.2f, Color.red, sound: "BloodSpike", critChance: 0.35f, accuracy: 0.85f, doubleEdgeDamage: 10f, charName: "Necromancer", numberOfTargets: 1),
                    new AttackData("Red Lightning", 50, "Dark thunder strike, drawing heavily from your own life force.", "Attack1",
                        AttackType.DirectHit, "RedLightning", new Vector3(0.0f, 0.5f, 0f), 0.15f, 1.5f, Color.red, sound: "ThunderBolt", critChance: 0.30f, accuracy: 0.60f, doubleEdgeDamage: 30f, charName: "Necromancer", numberOfTargets: 1),
                    new AttackData("Final Offering", 150, "Sacrifices all life energy to unleash a cataclysmic curse, with a high chance of self-destruction.", "Attack1",
                        AttackType.AreaEffect, "BloodTornado", new Vector3(0.0f, 0.5f, 0f), 0.25f, 1.5f, Color.red, sound: "Hurricane", critChance: 0.0f, accuracy: 1f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.85f, charName: "Necromancer", numberOfTargets: 99)
                };
            case "Crystal":
                return new List<AttackData>
                {
                    new AttackData("Crystal Crusher", 20, "Shattering punch. Good accuracy.", "Attack1",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 0f, Color.cyan, sound: "Crystal/Atk1", critChance: 0.25f, accuracy: 0.95f, charName: "Crystal", numberOfTargets: 1),
                    new AttackData("Crystal Hammer", 25, "A massive crystal hammer falls from the sky. Very high crit chance", "Attack2",
                        AttackType.DirectHit, "PhantomShatter", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1.2f, Color.cyan, sound: "Crystal/Atk2", critChance: 0.40f, accuracy: 0.95f, charName: "Crystal", numberOfTargets: 1),
                    new AttackData("Crystal Eruption", 45, "Gem explosion, shards may cut you significantly.", "Attack3",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.15f, 0f, Color.cyan, sound: "Crystal/Atk3", critChance: 0.05f, accuracy: 0.95f, doubleEdgeDamage: 20f, charName: "Crystal", numberOfTargets: 1),
                    new AttackData("Prismatic Overload", 65, "Channels immense crystal energy.", "Special",
                        AttackType.AreaEffect, "None", new Vector3(0.0f, 0.5f, 0f), 0.15f, 0f, Color.cyan, sound: "Crystal/Special", critChance: 0.0f, accuracy: 0.95f, doubleEdgeDamage: 10f, charName: "Crystal", numberOfTargets: 99)
                };
            case "Ground":
                return new List<AttackData>
                {
                    new AttackData("Quick Punch", 20, "Fast earth strike. High Accuracy", "Attack1",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1f, new Color(0.6f, 0.3f, 0.1f), sound: "Ground/RockPunch", critChance: 0.15f, accuracy: 0.95f, charName: "Ground", numberOfTargets: 1),
                    new AttackData("Punch Combo", 35, "Multiple earth hits, causing minor tremors to yourself. High crit chance", "Attack2",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.1f, 1f, new Color(0.6f, 0.3f, 0.1f), sound: "Ground/DoubleRockPunch", critChance: 0.30f, accuracy: 0.80f, doubleEdgeDamage: 10f, charName: "Ground", numberOfTargets: 1),
                    new AttackData("Rock Slide", 50, "Falling boulder. Riskier, but can be worth it.", "Attack3",
                        AttackType.MoveAndHit, "None", new Vector3(0.0f, 0.5f, 0f), 0.35f, 1f, new Color(0.6f, 0.3f, 0.1f), sound: "Ground/RealGroundAttack3", critChance: 0.15f, accuracy: 0.60f, doubleEdgeDamage: 20f, charName: "Ground", numberOfTargets: 1),
                    new AttackData("Titanic Reckoning", 100, "Shatters the earth with suicidal force, potentially crushing yourself.", "Special",
                        AttackType.AreaEffect, "None", new Vector3(0.0f, 0.5f, 0f), 0.25f, 1.8f, new Color(0.6f, 0.3f, 0.1f), sound: "Ground/RockSpecial", critChance: 0.0f, accuracy: 1.0f, doubleEdgeDamage: 0f, canSelfKO: true, selfKOFailChance: 0.5f, charName: "Ground", numberOfTargets: 99)
                };
            default:
                return new List<AttackData>
                {
                    new AttackData("Basic Attack", 10, "Simple strike.", "Attack1",
                        AttackType.DirectHit, "Slash", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "", critChance: 0.0f, accuracy: 0.95f, charName: "Default", numberOfTargets: 1),
                    new AttackData("Special Attack", 15, "Enhanced strike. Balanced.", "Attack2",
                        AttackType.DirectHit, "WarriorSlash", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "", critChance: 0.05f, accuracy: 0.90f, charName: "Default", numberOfTargets: 1),
                    new AttackData("Power Strike", 25, "Strong attack with some recoil.", "Attack1",
                        AttackType.DirectHit, "Slash", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "", critChance: 0.0f, accuracy: 0.85f, doubleEdgeDamage: 10f, charName: "Default", numberOfTargets: 1),
                    new AttackData("Ultimate Attack", 60, "A devastating, yet risky, ultimate.", "Attack2",
                        AttackType.DirectHit, "WarriorSlash", new Vector3(0.0f, 0.5f, 0.0f), 0.1f, 0.8f, Color.red, sound: "", critChance: 0.15f, accuracy: 0.75f, doubleEdgeDamage: 0f, charName: "Default", numberOfTargets: 1)
                };
        }
    }

    // ... (Rest of the class is unchanged)

    public List<AttackData> GetAttacksForEnemy(string enemyName)
    {
        switch (enemyName)
        {
            case "FlyingEye":
                return new List<AttackData> {
                new AttackData("Bite", 10, "A piercing bite that slightly damages the target.", "Attack1",
                    AttackType.MoveAndHit, "None", Vector3.zero, 0.1f, 0.1f, Color.red, sound: "Knight/Slash4", accuracy: 1f, maxCooldown: 0),

                new AttackData("Terrifying Scream", 5, "A scream that instills fear, lowering player's accuracy.", "Attack3",
                    AttackType.DirectHit, "None", Vector3.zero, 0.1f, 0.1f, Color.yellow, sound: "Knight/Slash4", accuracy: 1.0f, maxCooldown: 4,
                    effects: new List<StatusEffect> { new StatusEffect { effectType = StatusEffectType.AccuracyDown, value = 0.15f, duration = 2, isBuff = false } }),

                new AttackData("Dive Bomb", 25, "Dives at the player for heavy damage.", "Attack2",
                    AttackType.MoveAndHit, "None", Vector3.zero, 0.1f, 0.1f, Color.red, sound: "Knight/Slash4", accuracy: 0.8f, maxCooldown: 3)
            };
            case "Slime":
                return new List<AttackData> {
                new AttackData("Slime Swipe", 8, "A weak, sticky swipe.", "Attack1",
                    AttackType.MoveAndHit, "None", new Vector3(0, 0.7f, 0), 0.1f, 0.1f, Color.blue, sound: "Knight/Slash4", accuracy: 1f, maxCooldown: 0),

                new AttackData("Corrosive Spit", 12, "Spits acid that lowers player's damage.", "Attack2",
                    AttackType.MoveAndHit, "None", new Vector3(0, 0.5f, 0), 0.2f, 0.1f, Color.blue, sound: "Archer/Atk1", accuracy: 1f, maxCooldown: 3,
                    hitEffectName: "None",
                    effects: new List<StatusEffect> { new StatusEffect { effectType = StatusEffectType.DamageDown, value = 0.2f, duration = 2, isBuff = false } }),

                new AttackData("Engulf", 0, "Consumes ambient magic to heal itself.", "Attack2",
                    AttackType.Heal, "None", new Vector3(0, 0.5f, 0), 0.2f, 0.1f, Color.green, sound: "Heal", accuracy: 1.0f, maxCooldown: 4)
            };
            default:
                return new List<AttackData> {
                new AttackData("Tackle", 8, "A basic physical attack.", "Attack1", AttackType.MoveAndHit, "Slash", Vector3.zero, 0.1f, 0.1f, Color.white, sound: "Knight/Slash4", accuracy: 0.9f, maxCooldown: 0)
            };
        }
    }

    public GameObject GetEffectPrefabForAttack(AttackData attack)
    {
        if (!string.IsNullOrEmpty(attack.effectPrefabName))
        {
            //This is likely meant to load the sound, not a visual effect, based on the new understanding.
            //The logic for playing sound vs instantiating effect should be handled elsewhere.
            //This manager's job is just to hold the data correctly.
            GameObject customEffect = Resources.Load<GameObject>("Effects/" + attack.effectPrefabName);
            if (customEffect != null)
                return customEffect;
        }

        return null; //Return null if no effect is specified
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