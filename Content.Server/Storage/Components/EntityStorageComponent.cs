using Content.Server.Atmos;
using Content.Shared.Storage.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Storage.Components;

[RegisterComponent, ComponentReference(typeof(SharedEntityStorageComponent))]
public sealed class EntityStorageComponent : SharedEntityStorageComponent, IGasMixtureHolder
{
    /// <summary>
    ///     Gas currently contained in this entity storage.
    ///     None while open. Grabs gas from the atmosphere when closed, and exposes any entities inside to it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air { get; set; } = new (GasMixVolume);
}
