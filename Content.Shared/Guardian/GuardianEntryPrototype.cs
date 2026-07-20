using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Guardian;

/// <summary>
/// Class used to describe data pertaining to one guardian
/// </summary>
[DataDefinition]
[Prototype]
public sealed partial class GuardianEntryPrototype : IPrototype
{
    /// <summary>
    /// The entry's icon in the radial guardian selection
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon;

    /// <summary>
    /// The entry's description on mouse hovering.
    /// </summary>
    [DataField]
    public LocId Description;

    /// <summary>
    /// The entry's title on mouse hovering.
    /// </summary>
    [DataField]
    public LocId Title;

    /// <summary>
    /// The components granted to the guardian host.
    /// </summary>
    [DataField]
    public EntProtoId? Components;

    /// <summary>
    /// The guardian itself
    /// </summary>
    [DataField]
    public EntProtoId Guardian;

    [ViewVariables]
    [IdDataField]
    public string ID => "guardianEntry";
}
