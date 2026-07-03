using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Represents a pipe network.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PipeNetComponent : Component, IGasMixtureHolder
{
    [DataField]
    public GasMixture Air { get; set; } = new() {Temperature = Atmospherics.T20C};

    [ViewVariables]
    public EntityUid? Grid;
}
