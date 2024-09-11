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
    public GasMixture Air { get; set; } = new ();

    /// <summary>
    ///     This is how much gas fits inside this entity.
    /// </summary>
    public float Volume = 200;
}
