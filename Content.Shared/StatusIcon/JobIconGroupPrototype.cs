using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.StatusIcon;

/// <summary>
/// A grouping of <see cref="JobIconPrototype"/>. Used for displaying in a menu.
/// </summary>
[Prototype]
public sealed partial class JobIconGroupPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Set of ids that make up the group.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<JobIconPrototype>> Icons = new();

    /// <summary>
    /// Name of the group used for menu tooltips.
    /// </summary>
    [DataField]
    public LocId GroupName;

    /// <summary>
    /// Sprite used to represent the group.
    /// </summary>
    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new ("/Textures/Interface/Misc/job_icons.rsi"), "Unknown");
}
