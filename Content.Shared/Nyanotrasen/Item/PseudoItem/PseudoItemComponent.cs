using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nyanotrasen.Item.PseudoItem;

    /// <summary>
    /// For entities that behave like an item under certain conditions,
    /// but not under most conditions.
    /// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class PseudoItemComponent : Component
{
    [DataField("size")]
    public ProtoId<ItemSizePrototype> Size = "Huge";

    /// <summary>
    /// An optional override for the shape of the item within the grid storage.
    /// If null, a default shape will be used based on <see cref="Size"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2i>? Shape;

    [DataField, AutoNetworkedField]
    public Vector2i StoredOffset;

    public bool Active = false;
}
