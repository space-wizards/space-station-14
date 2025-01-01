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
    /// The duration of the stun. Edited 5 -> 3.5 for impstation
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3.5); 

    [DataField]
    public EntityWhitelist Blacklist = new();
}
