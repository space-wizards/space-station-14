using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.EntityEffects.Components;

/// <summary>
///  Periodically imposes entity effects on an entity
///  Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectApplyStatusEffectComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects { get; set; } = default!;

    /// <summary>
    /// How often the status effect is applied
    /// </summary>
    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(1);

    [DataField]
    public FixedPoint2 Quantity = FixedPoint2.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextApplyTime = TimeSpan.Zero;
}
