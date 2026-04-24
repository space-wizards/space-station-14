using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PassiveOrganDamageComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField(required: true)]
    public FixedPoint2 Damage;

    [DataField(required: true)]
    public FixedPoint2 DamageCap;
}
