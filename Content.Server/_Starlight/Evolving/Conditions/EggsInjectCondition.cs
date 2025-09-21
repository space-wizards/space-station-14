using Content.Shared._Starlight.Evolving;

namespace Content.Server._Starlight.Evolving.Conditions;

/// <summary>
/// Condition that checks if the entity injected a certain amount of eggs.
/// </summary>
public sealed partial class EggsInjectCondition : EvolvingCondition
{
    /// <summary>
    /// Target amount of eggs which we wait to mark condition as passed.
    /// </summary>
    [DataField]
    public int TargetEggsAmount = 4;

    private int _injectedEggs = 0;

    public override bool Condition(EvolvingConditionArgs args) => _injectedEggs >= TargetEggsAmount;

    public bool Condition() => _injectedEggs >= TargetEggsAmount;

    public void UpdateEggs(int eggsAmount) => _injectedEggs += eggsAmount;
}