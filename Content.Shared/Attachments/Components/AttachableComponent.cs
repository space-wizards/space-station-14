using Robust.Shared.Prototypes;

namespace Content.Shared.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachableComponent : Component
{
    /// <summary>
    ///     Components this object will be granted when an item is inserted.
    /// </summary>
    [ViewVariables]
    [DataField]
    public Dictionary<string, ComponentRegistry> Components;

    /// <summary>
    ///     Fields from components that will be inherited from the inserted item.
    /// </summary>
    [ViewVariables]
    [DataField]
    public Dictionary<string, List<string>> Fields;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<(EntityUid, Type)> AddedComps = new ();
}
