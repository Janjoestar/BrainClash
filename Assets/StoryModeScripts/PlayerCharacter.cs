using UnityEngine;

public class PlayerCharacter
{
    private GameObject player;
    private Transform playerPosition;

    public Vector3 OriginalPlayerPosition { get; private set; }

    public PlayerCharacter(GameObject player, Transform playerPosition)
    {
        this.player = player;
        this.playerPosition = playerPosition;
    }

    public void SetupPlayer()
    {
        if (playerPosition != null)
        {
            player.transform.position = playerPosition.position;
            OriginalPlayerPosition = player.transform.position;
        }
        FlipSprite(player, true);
    }

    public void SetCharacter(Character character)
    {
        if (character == null) return;

        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sprite = character.characterSprite;

        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            string overridePath = "Animations/" + character.characterName + "Override";
            AnimatorOverrideController overrideController = Resources.Load<AnimatorOverrideController>(overridePath);
            if (overrideController != null)
            {
                // This is the *permanent* controller for the player character in this battle
                animator.runtimeAnimatorController = overrideController;
                animator.Rebind(); // Added: Crucial for immediate application
                animator.Update(0f); // Added: Ensure state machine is initialized
                Debug.Log($"[PlayerCharacter] Player's base animator set to: {character.characterName}Override");
            }
            else
            {
                Debug.LogWarning($"[PlayerCharacter] No Animator Override Controller found for {character.characterName} at {overridePath}. Player Animator might be missing or incorrect.");
                // If no override controller is found for the selected character,
                // ensure there's at least a default controller on the Player GameObject in the scene.
            }
        }
    }

    private void FlipSprite(GameObject player, bool faceRight)
    {
        SpriteRenderer sprite = player.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.flipX = faceRight;
    }
}