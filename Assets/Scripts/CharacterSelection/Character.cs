// In Character.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Character
{
    public string characterName;
    public Sprite characterSprite;
    public List<AttackData> characterAttacks = new List<AttackData>();
    public int maxHealth;
    public Color characterColor;
}