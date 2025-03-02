using Robust.Shared.Prototypes;

namespace Content.Shared.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachmentHolderComponent : Component
{
    /// <summary>
    ///     Components this object will be granted.
    /// </summary>
    [ViewVariables]
    [DataField]
    public Dictionary<string, ComponentRegistry>? Components;

    /// <summary>
    ///     Prototype which we will pull components from if <see cref="Components"/>> is null.>.
    /// </summary>
    [ViewVariables]
    [DataField]
    public Dictionary<string, EntProtoId>? Prototypes;

    /// <summary>
    ///     Fields from components that will be inherited from the inserted item.
    /// </summary>
    [ViewVariables]
    [DataField]
    public Dictionary<string, List<string>>? Fields;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<(EntityUid, Type)> AddedComps = new ();
}
