using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule that ends when a player gets a certain number of kills.
/// </summary>
[RegisterComponent, Access(typeof(DeathMatchRuleSystem))]
public sealed partial class DeathMatchRuleComponent : Component
{
    /// <summary>
    /// The number of points a player has to get to win.
    /// </summary>
    [DataField]
    public FixedPoint2 KillCap = 31;

    /// <summary>
    /// How long until the round restarts
    /// </summary>
    [DataField]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// The person who won.
    /// We store this here in case of some assist shenanigans.
    /// </summary>
    [DataField]
    public NetUserId? Victor;

    /// <summary>
    /// An entity spawned after a player is killed.
    /// </summary>
    [DataField]
    public EntityTableSelector RewardSpawns = default!;

    /// <summary>
    /// The gear all players spawn with.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> Gear = "DeathMatchGear";
}
