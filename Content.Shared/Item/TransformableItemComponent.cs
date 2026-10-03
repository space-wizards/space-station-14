using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Item;

/// <summary>
/// Marks this item as able to swap place with another item stashed inside it, e.g. a pair of gloves
/// that swap for a hidden fingergun. see <see cref="TransformableItemSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TransformableItemComponent : Component
{
    /// <summary>
    /// The prototype spawned inside <see cref="ContainerId"/> the first time this item is initialized, if the
    /// container is empty. leave unset for the item that starts hidden (its container gets filled
    /// the first time something is swapped into it).
    /// </summary>
    [DataField]
    public EntProtoId? Prototype;

    /// <summary>
    /// The container the paired item is stashed in while this one is actiive
    /// </summary>
    [DataField]
    public string ContainerId = "transform_stash";
}
