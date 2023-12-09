using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.CombatMode.Pacification;

/// <summary>
/// Status effect that disables combat mode and restricts aggressive actions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPacificationSystem))]
public sealed partial class PacifiedComponent : Component
{
    /// <summary>
    ///     A blacklist specifying entities that the player will refuse to throw.
    /// </summary>
    [DataField("throwBlacklist"), ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist ThrowBlacklist = new()
    {
            Components = new[]
            {
                // For sure no throwing stars or darts:
                "EmbeddableProjectile",
                // Nothing that can hurt if it hits someone:
                "DamageOtherOnHit",
                "DamageOnLand",
                // Nothing that could make someone slip:
                "Slippery",
                // Containers full of liquid TBD. Some liquids are inert and we should not prohibit tossing
                // water balloons, but should prohibit fluorosulfuric acid beakers.
            }
            //,
            // Tags = new ()
            // {
            //     // No grenades!
            //     "Grenade"
            // }
    };

    /// <summary>
    ///     A blacklist specifying liquids that the player will refuse to spill.
    /// </summary>
    // [DataField("spillBlacklist"), ViewVariables(VVAccess.ReadWrite)]
    // public EntityWhitelist SpillBlacklist = new()
    // {
    //
    // };



}
