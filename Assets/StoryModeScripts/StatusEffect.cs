// StatusEffect.cs

using UnityEngine;

public enum StatusEffectType
{
    // Buffs
    DamageUp,
    DefenseUp,
    HealOverTime,
    // Debuffs
    DamageDown,
    AccuracyDown,
    Stun,
    DamageOverTime
}

[System.Serializable]
public class StatusEffect
{
    public StatusEffectType effectType;
    public float value;       // e.g., 0.2 for 20% down, or 10 for 10 damage
    public int duration;      // in turns
    public bool isBuff;
}