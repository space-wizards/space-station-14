using Content.Shared.FixedPoint;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule that ends when a player gets a
/// </summary>
[RegisterComponent, Access(typeof(DeathMatchRuleSystem))]
public sealed partial class DeathMatchRuleComponent : Component
{
    /// <summary>
    /// The number of points a player has to get to win.
    /// </summary>
    [DataField("killCap"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 KillCap = 31;

    /// <summary>
    /// How long until the round restarts
    /// </summary>
    [DataField("restartDelay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// The person who won.
    /// We store this here in case of some assist shenanigans.
    /// </summary>
    [DataField("victor")]
    public NetUserId? Victor;
}
