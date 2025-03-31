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

public class AttackData
{
    public string attackName;
    public int damage;
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

    public AttackData(string name, int dmg, string desc, string animTrigger,
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