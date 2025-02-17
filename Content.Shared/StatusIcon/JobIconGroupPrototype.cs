using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.StatusIcon;

/// <summary>
///     A grouping of <see cref="JobIconPrototype"/>. Used for displaying in a menu.
/// </summary>
[Prototype]
public sealed partial class JobIconGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Set of ids that make up the group.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<JobIconPrototype>> Icons = default!;

    /// <summary>
    /// Name of the group used for menu tooltips.
    /// TODO
    /// </summary>

    /// <summary>
    /// Sprite used to represent the group.
    /// TODO
    /// </summary>
}
