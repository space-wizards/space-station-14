using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access;

/// <summary>
///     Contains a list of access tags that are part of this group.
///     Used by <see cref="AccessComponent"/> to avoid boilerplate.
/// </summary>
[Prototype]
public sealed partial class AccessGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The player-visible name of the access level group
    /// </summary>
    [DataField]
    public string? Name { get; set; }

    /// <summary>
    /// The access levels associated with this group
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<AccessLevelPrototype>> Tags = default!;

    public string GetAccessGroupName()
    {
        if (Name is { } name)
            return Loc.GetString(name);

        return ID;
    }
}
