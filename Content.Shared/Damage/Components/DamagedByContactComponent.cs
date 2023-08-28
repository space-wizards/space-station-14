using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[NetworkedComponent, RegisterComponent]
public sealed partial class DamagedByContactComponent : Component
{
    [DataField("nextSecond", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    [ViewVariables]
    public DamageSpecifier? Damage;
}
