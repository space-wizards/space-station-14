using Content.Server.Advertise.EntitySystems;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.Advertise.Components;

/// <summary>
/// Makes this entity periodically advertise by speaking a randomly selected
/// message from a specified dataset into local chat.
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
    /// If true, the delay before the first advertisement (at MapInit) will ignore <see cref="MinimumWait"/>
    /// and instead be rolled between 0 and <see cref="MaximumWait"/>. This only applies to the initial delay;
    /// <see cref="MinimumWait"/> will be respected after that.
    /// </summary>
    [DataField]
    public bool Prewarm = true;

    /// <summary>
    /// The identifier for the advertisements dataset prototype.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Pack { get; private set; }

    /// <summary>
    /// The next time an advertisement will be said.
    /// </summary>
    [DataField]
    public TimeSpan NextAdvertisementTime { get; set; } = TimeSpan.Zero;

}
