using Content.Server.Atmos;
using Content.Shared.Mech.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Mech;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedMechComponent))]
public sealed class MechComponent : SharedMechComponent
{
    [DataField("airtight"), ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air = new (GasMixVolume);
    public const float GasMixVolume = 70f;
}
