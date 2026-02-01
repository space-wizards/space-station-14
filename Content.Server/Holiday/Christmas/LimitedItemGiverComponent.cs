using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Holiday;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday.Christmas;

/// <summary>
/// This is used for granting items to lucky souls, exactly once.
/// </summary>
[RegisterComponent, Access(typeof(LimitedItemGiverSystem))]
public sealed partial class LimitedItemGiverComponent : Component
{
    /// <summary>
    /// Santa knows who you are behind the screen, only one gift per player per round!
    /// </summary>
    public readonly HashSet<NetUserId> GrantedPlayers = new();

    /// <summary>
    /// Selects what entities can be given out by the giver.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// The (localized) message shown upon receiving something.
    /// </summary>
    [DataField]
    public LocId? ReceivedPopup;

    /// <summary>
    /// The (localized) message shown upon being denied.
    /// </summary>
    [DataField]
    public LocId? DeniedPopup;

    /// <summary>
    /// The (localized) message shown when trying to get a second item.
    /// </summary>
    [DataField]
    public LocId? GreedPopup;
}
