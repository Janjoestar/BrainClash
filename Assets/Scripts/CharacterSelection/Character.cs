// In Character.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Character
{
    public string characterName;
    public Sprite characterSprite;
    public int maxHealth;
    public List<AttackData> characterAttacks = new List<AttackData>();
    public Color characterColor;
    public Color secondaryColor;
    public Color primaryColor;
}