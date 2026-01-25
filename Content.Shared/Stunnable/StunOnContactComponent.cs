using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunOnContactComponent : Component
{
    /// <summary>
    /// The fixture the entity must collide with to be stunned
    /// </summary>
    [DataField]
    public string FixtureId = "fix";

    /// <summary>
    /// The duration of the stun.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Should the stun applied refresh?
    /// </summary>
    [DataField]
    public bool Refresh = true;

    /// <summary>
    /// Should the stunned entity try to stand up when knockdown ends?
    /// </summary>
    [DataField]
    public bool AutoStand = true;

    [DataField]
    public EntityWhitelist Blacklist = new();
}
