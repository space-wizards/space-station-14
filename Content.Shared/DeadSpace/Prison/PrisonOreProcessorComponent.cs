using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Prison;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrisonOreProcessorComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> OreValues = new();

    [DataField]
    public int PointsPerSecond = 10;

    [DataField]
    public int CrateMinimumUnits = 10;
}

/// <summary>
/// Raised on a prison ore processor after an ore box is dropped onto it.
/// </summary>
[ByRefEvent]
public readonly record struct PrisonOreBoxDepositEvent(EntityUid Box, EntityUid User);
