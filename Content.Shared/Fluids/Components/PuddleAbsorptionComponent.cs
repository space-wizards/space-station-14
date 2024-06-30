using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Passively absorbs puddles that are being stepped on by the entity
/// with this component
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedPuddleSystem))]
public sealed partial class PuddleAbsorptionComponent : Component
{
    /// <summary>
    /// The next time we absorb from puddles for this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;

    /// <summary>
    /// How much of the solution being stepped on
    /// to absorb per tick
    /// </summary>
    [DataField]
    public float AmountPerTick = 0.5f;
}
