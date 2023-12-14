using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
    [DataField("wrapper", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Wrapper;

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField("sound", required: true)]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Whether or not the gift should be limited only to actual items.
    /// </summary>
    [DataField("insaneMode", required: true), ViewVariables(VVAccess.ReadWrite)]
    public bool InsaneMode;

    /// <summary>
    /// What entities are allowed to examine this gift to see its contents.
    /// </summary>
    [DataField("contentsViewers", required: true)]
    public EntityWhitelist ContentsViewers = default!;

    /// <summary>
    /// The currently selected entity to give out. Used so contents viewers can see inside.
    /// </summary>
    [DataField("selectedEntity"), ViewVariables(VVAccess.ReadWrite)]
    public string? SelectedEntity;
}
