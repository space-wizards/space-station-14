using Robust.Shared.GameStates;
using Content.Shared.Atmos;

namespace Content.Shared.Disposal.Unit;

/// <summary>
///     A component added to entities that are currently in disposals.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeingDisposedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Holder;
}

/// <summary>
/// Raised on a being-disposed holder to request the gas mixture its contained entities should breathe.
/// </summary>
[ByRefEvent]
public record struct GetBeingDisposedGasEvent
{
    public GasMixture? Gas;
}
