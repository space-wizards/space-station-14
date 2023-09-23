using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Components;

/// <summary>
/// Makes a node graph unify the contents of any gas-containing nodes it has.
/// </summary>
[Access(typeof(AtmosPipeNetSystem), typeof(AtmosPipeNodeComponent))]
[RegisterComponent]
public sealed partial class AtmosPipeNetComponent : Component, IGasMixtureHolder
{
    /// <summary>
    /// The gases that the node graph contains.
    /// Shared between all gas containing nodes.
    /// </summary>
    [DataField("air")]
    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air { get; set; } = new() { Temperature = Atmospherics.T20C };

    /// <summary>
    /// The grid that this pipe network is a part of.
    /// AKA the one responsible for updating the state of this pipenet.
    /// </summary>
    [ViewVariables]
    public EntityUid? GridId = null;
}
