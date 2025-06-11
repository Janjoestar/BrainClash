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
                animator.runtimeAnimatorController = overrideController;
            else
                Debug.LogWarning("No Animator Override Controller found for " + character.characterName);
        }
    }

    private void FlipSprite(GameObject player, bool faceRight)
    {
        SpriteRenderer sprite = player.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.flipX = faceRight;
    }
}