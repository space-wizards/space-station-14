using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lathe.Components;

/// <summary>
/// For EntityQuery to keep track of which lathes are producing
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LatheProducingComponent : Component
{
    /// <summary>
    /// The time at which production began
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    /// <summary>
    /// How long it takes to produce the recipe.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ProductionLength;
}

