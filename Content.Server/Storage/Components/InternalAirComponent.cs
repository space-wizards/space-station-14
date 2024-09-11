using Content.Server.Atmos;
using Content.Shared.Atmos;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed partial class InternalAirComponent : Component, IGasMixtureHolder
{
    /// <summary>
    ///     The gas currently contained in this entity.
    ///     Used by containers, vehicles and so on to expose contained entities to the gas
    /// </summary>
    [DataField("air"), ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air { get; set; } = new (100);
}
