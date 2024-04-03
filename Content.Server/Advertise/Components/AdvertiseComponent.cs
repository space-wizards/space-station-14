using Content.Server.Advertise.EntitySystems;
using Content.Shared.Advertise;
using Robust.Shared.Prototypes;

namespace Content.Server.Advertise.Components;

/// <summary>
/// Makes this entity periodically advertise by speaking a randomly selected
/// message from a specified MessagePack into local chat.
/// </summary>
[RegisterComponent, Access(typeof(AdvertiseSystem))]
public sealed partial class AdvertiseComponent : Component
{
    /// <summary>
    /// Minimum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal to 1.
    /// </summary>
    [DataField]
    public int MinimumWait { get; private set; } = 8 * 60;

    /// <summary>
    /// Maximum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal
    /// to <see cref="MinimumWait"/>
    /// </summary>
    [DataField]
    public int MaximumWait { get; private set; } = 10 * 60;

    /// <summary>
    /// The identifier for the advertisements pack prototype.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MessagePackPrototype> Pack { get; private set; }

    /// <summary>
    /// The next time an advertisement will be said.
    /// </summary>
    [DataField]
    public TimeSpan NextAdvertisementTime { get; set; } = TimeSpan.Zero;

}
