using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(DamageOnHoldingSystem))]
public sealed partial class DamageOnHoldingComponent : Component
{
    [DataField, ViewVariables]
    [AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Damage per interval dealt to entity holding the entity with this component
    /// </summary>
    [DataField, ViewVariables]
    public DamageSpecifier Damage = new();
    // TODO: make it networked

    /// <summary>
    /// Delay between damage events in seconds
    /// </summary>
    [DataField, ViewVariables]
    [AutoNetworkedField]
    public float Interval = 1f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)), ViewVariables(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextDamage = TimeSpan.Zero;
}
