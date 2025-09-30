using Content.Shared._Starlight.Evolving;

namespace Content.Server._Starlight.Evolving.Conditions;

/// <summary>
/// Condition that checks if the entity created a certain amount of spider web.
/// </summary>
public sealed partial class SpiderWebCondition : EvolvingCondition
{
    /// <summary>
    /// Target amount of eggs which we wait to mark condition as passed.
    /// </summary>
    [DataField]
    public int TargetWebAmount = 24;

    private int _createdWebs = 0;

    public override bool Condition(EvolvingConditionArgs args) => _createdWebs >= TargetWebAmount;

    public bool Condition() => _createdWebs >= TargetWebAmount;

    public void UpdateWebs(int websAmount) => _createdWebs += websAmount;
}