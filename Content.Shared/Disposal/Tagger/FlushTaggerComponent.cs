using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Tagger;

/// <summary>
/// Causes a disposals unit to tag its entities during a flush.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlushTaggerComponent : Component
{
    /// <summary>
    /// The disposals tag getting added.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string DisposalTag;
}
