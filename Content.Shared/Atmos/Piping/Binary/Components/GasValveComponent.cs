using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Binary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasValveComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Open = true;

    [DataField("inlet")]
    public string InletName = "inlet";

    [DataField("outlet")]
    public string OutletName = "outlet";

    [DataField]
    public SoundSpecifier ValveSound = new SoundCollectionSpecifier("valveSqueak");
}
