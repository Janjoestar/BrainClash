using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    public static EffectSpawner Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [System.Serializable]
    public class CharacterEffectPoints
    {
        public string characterName;
        public Transform projectileSpawnPoint;
        public Transform directEffectPoint;
        public Transform targetHitPoint;
    }

    public List<CharacterEffectPoints> characterEffectPoints = new List<CharacterEffectPoints>();

    private Dictionary<string, CharacterEffectPoints> effectPointsMap = new Dictionary<string, CharacterEffectPoints>();

    private void Start()
    {
        foreach (CharacterEffectPoints points in characterEffectPoints)
        {
            effectPointsMap[points.characterName] = points;
        }
    }

    public CharacterEffectPoints GetEffectPointsForCharacter(string characterName)
    {
        if (effectPointsMap.ContainsKey(characterName))
            return effectPointsMap[characterName];
        return null;
    }

    public IEnumerator SpawnEffect(GameObject character, AttackData attack, bool isSelectionScreen = false)
    {
        string characterName = "";
        characterName = GetCharacterName(character);

        bool isPlayerOne = IsPlayerOne(character);
        Debug.Log($"Spawning effect for {characterName} (Player {(isPlayerOne ? "1" : "2")}) with attack: {attack.attackName}");

        if (attack.effectDelay > 0)
        {
            yield return new WaitForSeconds(attack.effectDelay);
        }

        switch (attack.attackType)
        {
            case AttackType.Projectile:
            case AttackType.Magic:
                yield return SpawnTravelingEffect(character, attack, isSelectionScreen, isPlayerOne);
                break;
            case AttackType.Heal:
                yield return SpawnDirectEffect(character, attack, isSelectionScreen, isPlayerOne);
                break;
            case AttackType.MoveAndHit:
                yield return SpawnMoveAndHitEffect(character, attack, isSelectionScreen, isPlayerOne);
                break;
            default:
                yield return SpawnDirectEffect(character, attack, isSelectionScreen, isPlayerOne);
                break;
        }
    }

    private bool IsPlayerOne(GameObject character)
    {
        CharacterSelectionManager selManager = FindObjectOfType<CharacterSelectionManager>();
        if (selManager != null)
        {
            return selManager.artworkSpriteP1.gameObject == character;
        }
        return false;
    }

    private IEnumerator SpawnDirectEffect(GameObject character, AttackData attack, bool isSelectionScreen, bool isPlayerOne)
    {

        GameObject effectPrefab = AttackDataManager.Instance.GetEffectPrefabForAttack(attack);
        if (effectPrefab == null)
            yield break;

        Vector3 spawnPosition;

        if (isSelectionScreen)
        {
            string characterName = GetCharacterName(character);
            bool isNecromancer = characterName.Contains("Necromancer");
            bool isBloodSpike = attack.attackName.Contains("Blood Spike");
            bool isHurricane = attack.attackName.Contains("Blood Tornado");
            CharacterEffectPoints points = GetEffectPointsForCharacter(characterName);

            if (points != null && points.directEffectPoint != null)
            {
                spawnPosition = points.directEffectPoint.position;

                // Add character-specific offsets or adjustments
                if (isNecromancer)
                {
                    if(isBloodSpike)
                        spawnPosition = new Vector3(4.22f, 0.54f, 0);
                    else if (isHurricane)
                        spawnPosition = new Vector3(4.22f, 1.8f, 0);

                }
            }
            else
                spawnPosition = character.transform.position + new Vector3(0, 0.5f, 0);

            if (!isPlayerOne)
            {
                spawnPosition.x = -spawnPosition.x;
            }
        }
        else
        {
            spawnPosition = attack.effectOffset;
        }

        GameObject attackEffect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);

        float effectDuration = 0.5f;
        Animator effectAnimator = attackEffect.GetComponent<Animator>();
        if (effectAnimator != null)
        {
            effectDuration = effectAnimator.GetCurrentAnimatorStateInfo(0).length;
        }
        ParticleSystem particleSystem = attackEffect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            effectDuration = particleSystem.main.duration;
        }

        yield return new WaitForSeconds(effectDuration);

        Destroy(attackEffect);
    }

    private IEnumerator SpawnTravelingEffect(GameObject character, AttackData attack, bool isSelectionScreen, bool isPlayerOne)
    {
        GameObject effectPrefab = AttackDataManager.Instance.GetEffectPrefabForAttack(attack);
        if (effectPrefab == null)
            yield break;

        Vector3 startPosition;
        Vector3 targetPosition;

        string characterName = GetCharacterName(character);

        if (isSelectionScreen)
        {
            CharacterEffectPoints points = GetEffectPointsForCharacter(characterName);

            if (points != null)
            {
                startPosition = points.projectileSpawnPoint != null ?
                    points.projectileSpawnPoint.position :
                    character.transform.position + new Vector3(0.5f, 0, 0);

                targetPosition = points.targetHitPoint != null ?
                    points.targetHitPoint.position :
                    startPosition + new Vector3(2, 0, 0);
            }
            else
            {
                // Default positions if no specific points defined
                startPosition = character.transform.position + new Vector3(0.5f, 0, 0);
                targetPosition = startPosition + new Vector3(2, 0, 0);
            }
        }
        else
        {
            // Use the attack's effect offset in battle
            startPosition = attack.effectOffset;
            targetPosition = attack.targetHitOffset;
        }

        if (!isPlayerOne)
        {
            Vector3 temp = startPosition;
            startPosition = targetPosition;
            targetPosition = temp;
        }

        GameObject attackEffect = Instantiate(effectPrefab, startPosition, Quaternion.identity);

        if (isPlayerOne)
        {
            Vector3 scale = attackEffect.transform.localScale;
            scale.x *= -1;
            attackEffect.transform.localScale = scale;
        }

        float speed = 15f;
        float distanceCovered = 0;
        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        Vector3 direction = (targetPosition - startPosition).normalized;

        while (distanceCovered < totalDistance)
        {
            float step = speed * Time.deltaTime;
            attackEffect.transform.position += direction * step;
            distanceCovered += step;
            yield return null;
        }

        attackEffect.transform.position = targetPosition;

        if (!string.IsNullOrEmpty(attack.hitEffectPrefabName))
        {
            Destroy(attackEffect);
            string pathToHitEffect = "Effects/" + attack.hitEffectPrefabName;
            GameObject hitEffectPrefab = Resources.Load<GameObject>(pathToHitEffect);
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, targetPosition, Quaternion.identity);
                if (isPlayerOne)
                {
                    Vector3 scale = hitEffect.transform.localScale;
                    scale.x *= -1;
                    hitEffect.transform.localScale = scale;
                }
                Animator hitAnimator = hitEffect.GetComponent<Animator>();
                if (hitAnimator != null)
                {
                    float hitAnimationLength = 2f;
                    AnimatorClipInfo[] clipInfo = hitAnimator.GetCurrentAnimatorClipInfo(0);
                    if (clipInfo.Length > 0)
                    {
                        hitAnimationLength = clipInfo[0].clip.length;
                    }
                    yield return new WaitForSeconds(hitAnimationLength);
                    Destroy(hitEffect);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                    Destroy(hitEffect);
                }
            }
        }
        else
        {
            Destroy(attackEffect);
        }
    }

    private IEnumerator SpawnMoveAndHitEffect(GameObject character, AttackData attack, bool isSelectionScreen, bool isPlayerOne)
    {
        if (isSelectionScreen)
        {
            yield return SpawnDirectEffect(character, attack, true, isPlayerOne);
        }
        else
        {
            yield return null;
        }
    }

    // Helper method to get character name
    private string GetCharacterName(GameObject character)
    {
        CharacterSelectionManager selManager = FindObjectOfType<CharacterSelectionManager>();
        if (selManager != null)
        {
            if (selManager.artworkSpriteP1.gameObject == character)
                return selManager.nameTextP1.text;
            else if (selManager.artworkSpriteP2.gameObject == character)
                return selManager.nameTextP2.text;
        }

        Character charComponent = character.GetComponent<Character>();
        if (charComponent != null)
            return charComponent.characterName;

        return "";
    }
}