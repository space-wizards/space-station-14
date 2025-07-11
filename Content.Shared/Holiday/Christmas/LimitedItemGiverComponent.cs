using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Holiday.Christmas;

/// <summary>
/// This is used for granting items to lucky souls, exactly once.
/// </summary>
[RegisterComponent, AutoGenerateComponentState, Access(typeof(LimitedItemGiverSystem))]
public sealed partial class LimitedItemGiverComponent : Component
{
    /// <summary>
    /// Santa knows who you are behind the screen, only one gift per player per round!
    /// </summary>
    [AutoNetworkedField] // Should we network a HashSet? (Probably no)
    public HashSet<NetUserId> GrantedPlayers = new();

    /// <summary>
    /// Selects what entities can be given out by the giver.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> SpawnEntries = default!;

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
    /// The holiday required for this giver to work, if any.
    /// </summary>
    [DataField]
    public ProtoId<HolidayPrototype>? RequiredHoliday;
}
