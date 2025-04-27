using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// Groups antags together for playtime requirements, similar to department prototype.
/// This can be expanded to be more similar to department prototypes if more uses can be found for it.
/// </summary>
[Prototype]
public sealed partial class AntagGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The name LocId of the antag group that will be displayed in the playtime requirement popup.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// A color representing this antag group to use for text.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    /// <summary>
    /// Antags to be included in the antag group.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<AntagPrototype>> Roles = new();
}
