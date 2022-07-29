namespace Content.Server.AI.HTN.PrimitiveTasks.Operators.Melee;

/// <summary>
/// Selects a target for melee.
/// </summary>
public sealed class PickMeleeTargetOperator : HTNOperator
{
    [ViewVariables, DataField("key")] public string Key = "CombatTarget";
}
