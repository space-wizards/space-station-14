using Content.Shared.Atmos.Miasma;
using Robust.Shared.GameStates;

namespace Content.Server.Atmos.Miasma;

[NetworkedComponent, RegisterComponent]
public sealed class FliesComponent : SharedFliesComponent
{
    /// Need something to hold the ambient sound, at least until that system becomes more robust
    public EntityUid VirtFlies;
}
