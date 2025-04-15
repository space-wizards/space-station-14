using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles;

/// <summary>
/// Spawns a spread of the projectiles when fired
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem))]
public sealed partial class ProjectileSpreadComponent : Component
{
    /// <summary>
    /// The entity prototype that will be fired by the rest of the spread.
    /// Will generally be the same entity prototype as the first projectile being fired.
    /// Needed for ammo components that do not specify a fired prototype, unlike cartridges.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Proto;

    /// <summary>
    /// How much the ammo spreads when shot, in degrees. Does nothing if count is 0.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.FromDegrees(5);

    /// <summary>
    /// How many prototypes are spawned when shot.
    /// </summary>
    [DataField]
    public int Count = 1;
}
