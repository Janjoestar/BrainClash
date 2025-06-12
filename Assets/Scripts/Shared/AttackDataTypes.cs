using System;
using UnityEngine;

// Define AttackType enum once
public enum AttackType
{
    Slash,            // Melee slash effects that appear near the target
    Projectile,       // Effects that travel from attacker to defender
    Magic,            // Magical projectiles that travel
    AreaEffect,       // Effects that appear on or around the target (explosions, etc.)
    DirectHit,        // Effects that appear directly on the target (hammer hit, etc.)
    MoveAndHit,       // Character moves to target, attacks, then returns to position
    Heal
}

// Define AttackData class once
[Serializable] // This is why it's not a ScriptableObject
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
    public int cooldown = 0;        // How many turns this attack is on cooldown
    public int maxCooldown = 0;     // Maximum cooldown turns (0 = no cooldown)
    public float damageIncrease = 0f;

    // NEW FIELD for the character name
    public string characterName;

    public AttackData(string name, float dmg, string desc, string animTrigger,
                      AttackType type, string effectName, Vector3 offset, float delay,
                      float flashInterval, Color flashColor, string sound = "",
                      string hitEffectName = "", Vector3 hitOffset = default,
                      float critChance = 0.0f, float accuracy = 0.85f, float doubleEdgeDamage = 0f,
                      bool canSelfKO = false, float selfKOFailChance = 0.0f, int maxCooldown = 0,
                      string charName = "Default") // Added charName parameter
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
        this.maxCooldown = maxCooldown;
        this.cooldown = 0;
        this.characterName = charName; // Assign the new field
    }

    // Backward compatibility constructor - updated to include characterName
    public AttackData(string name, int dmg, string desc, string animTrigger, AttackType type)
        : this(name, (float)dmg, desc, animTrigger, type, "", Vector3.zero, 0.3f, 0.1f, Color.red, charName: "Default") // Added charName here
    {
    }
}