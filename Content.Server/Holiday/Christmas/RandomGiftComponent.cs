using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday.Christmas;

/// <summary>
/// This is used for gifts with COMPLETELY random things.
/// </summary>
[RegisterComponent, Access(typeof(RandomGiftSystem))]
public sealed partial class RandomGiftComponent : Component
{
    /// <summary>
    /// The wrapper entity to spawn when unwrapping the gift.
    /// </summary>
    [DataField]
    public EntProtoId? Wrapper;

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// If false the gift will be limited only to <see cref="ItemComponent"/>.
    /// If true the gift can contain any entity with <see cref="PhysicsComponent"/> (except grids).
    /// </summary>
    [DataField]
    public bool InsaneMode;

    /// <summary>
    /// What entities are allowed to examine this gift to see its contents.
    /// </summary>
    [DataField]
    public EntityWhitelist? ContentsViewers;

    /// <summary>
    /// The currently selected entity to give out. Used so content viewers can see inside.
    /// </summary>
    [DataField]
    public EntProtoId? SelectedEntity;

    /// <summary>
    /// Text when content views examine it.
    /// </summary>
    [DataField]
    public LocId GiftContains = "gift-packin-contains";
}
