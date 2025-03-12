using UnityEngine;

[System.Serializable]
public class Character
{
    public string characterName;
    public Sprite characterSprite;
    public float scaleFactor = 1.0f;  // Default: 1.0 (no scaling)
    public Vector3 Player1positionOffset = Vector3.zero; // Default: No position offset
    public Vector3 Player2positionOffset = Vector3.zero; // Default: No position offset
}
