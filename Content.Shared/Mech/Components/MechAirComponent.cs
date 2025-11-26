using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechAirComponent : Component
{
    // TODO: this doesn't support a tank implant for mechs or anything like that.
    [DataField, AutoNetworkedField]
    public GasMixture Air = new(GasMixVolume);

    public const float GasMixVolume = 70f;
}
