using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage.Fulton;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FultonedComponent : Component
{
    /// <summary>
    /// Effect entity to delete upon removing the component. Only matters clientside.
    /// </summary>
    public EntityUid Effect;

    [ViewVariables(VVAccess.ReadWrite), DataField("beacon")]
    public EntityUid Beacon;

    /// <summary>
    /// When the fulton is travelling to the beacon.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextFulton", customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextFulton;
}
