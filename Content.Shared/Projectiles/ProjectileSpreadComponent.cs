using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Projectiles;

/// <summary>
/// Spawns a spread of the projectiles when fired
/// </summary>
[RegisterComponent]
public sealed partial class ProjectileSpreadComponent : Component
{
    /// <summary>
    /// The entity prototype that will be fired by the rest of the spread.
    /// Will generally be the same entity prototype as the first projectile being fired.
    /// Needed for ammo components that do not specify a fired prototype, unlike cartridges.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    /// <summary>
    /// How much the ammo spreads when shot, in degrees. Does nothing if count is 0.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spread")]
    public Angle Spread = Angle.FromDegrees(5);

    /// <summary>
    /// How many prototypes are spawned when shot.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("count")]
    public int Count = 1;
}
