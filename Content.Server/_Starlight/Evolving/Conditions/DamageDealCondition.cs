using Content.Shared._Starlight.Evolving;

namespace Content.Server._Starlight.Evolving.Conditions;

/// <summary>
/// Condition that checks if the entity has dealt a certain amount of damage.
/// </summary>
public sealed partial class DamageDealCondition : EvolvingCondition
{
    /// <summary>
    /// Target amount of damage which we wait to mark condition as passed.
    /// </summary>
    [DataField]
    public float TargetDamageAmount = 10f;

    /// <summary>
    /// Whether or not the damage must to be dealed to alive entity.
    /// </summary>
    [DataField]
    public bool OnlyAlive = false;

    private float _dealedDamage = 0f;

    public override bool Condition(EvolvingConditionArgs args) => _dealedDamage >= TargetDamageAmount;

    public bool Condition() => _dealedDamage >= TargetDamageAmount;

    public void AddDamage(float dealedDamage) => _dealedDamage += dealedDamage;
}