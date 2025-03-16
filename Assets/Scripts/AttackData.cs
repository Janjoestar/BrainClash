public class AttackData
{
    public string attackName;
    public int damage;
    public string description;
    public string animationTrigger;

    public AttackData(string name, int dmg, string desc, string animTrigger)
    {
        attackName = name;
        damage = dmg;
        description = desc;
        animationTrigger = animTrigger;
    }
}