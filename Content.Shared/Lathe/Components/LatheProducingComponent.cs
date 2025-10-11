using Robust.Shared.GameStates;

namespace Content.Shared.Lathe.Components;

/// <summary>
/// For EntityQuery to keep track of which lathes are producing
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LatheProducingComponent : Component
{
    /// <summary>
    /// The time at which production began
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StartTime;

    /// <summary>
    /// How long it takes to produce the recipe.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ProductionLength;
}

