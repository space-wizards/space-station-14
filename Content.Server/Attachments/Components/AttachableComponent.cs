using Robust.Shared.Prototypes;

namespace Content.Server.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachableComponent : Component
{
    [ViewVariables]
    [DataField("components")]
    public Dictionary<string, Dictionary<string, List<string>>> Components;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<(EntityUid, Type)> AddedComps = new ();
}
