using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Given priority when considering where to dock.
/// </summary>
[RegisterComponent]
public sealed partial class PriorityDockComponent : Component
{
    /// <summary>
    /// Tag to match on the docking request, if this dock is to be prioritised.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? Tag;
}
