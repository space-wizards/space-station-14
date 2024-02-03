using Content.Server.Research.Systems;
using Content.Shared.Radio;
using Content.Shared.Research;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Research.Components;

[RegisterComponent, Access(typeof(RoboticsConsoleSystem))]
public sealed partial class RoboticsConsoleComponent : Component
{
    /// <summary>
    /// Address and data of each cyborg.
    /// </summary>
    [DataField]
    public Dictionary<string, CyborgControlData> Cyborgs = new();

    /// <summary>
    /// After not responding for this length of time borgs are removed from the console.
    /// </summary>
    [DataField]
    public TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Radio channel to send messages on.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Science";

    /// <summary>
    /// Radio message sent when destroying a borg.
    /// </summary>
    [DataField]
    public LocId DestroyMessage = "robotics-console-cyborg-destroyed";

    /// <summary>
    /// Cooldown on destroying borgs to prevent complete abuse.
    /// </summary>
    [DataField]
    public TimeSpan DestroyCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When a borg can next be destroyed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextDestroy = TimeSpan.Zero;
}
