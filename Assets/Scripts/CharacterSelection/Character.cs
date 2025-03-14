using UnityEngine;

[System.Serializable]
public class Character
{
    public string characterName;
    public Sprite characterSprite;
    public Vector3 Player1positionOffset = Vector3.zero; // Default: No position offset
    public Vector3 Player2positionOffset = Vector3.zero; // Default: No position offset
}
