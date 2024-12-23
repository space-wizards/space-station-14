using Content.Shared.Cloning;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Item.PseudoItem;
/// <summary>
/// For entities that behave like an item under certain conditions,
/// but not under most conditions.
/// </summary>
[RegisterComponent]
public sealed partial class PseudoItemComponent : Component, ITransferredByCloning
{
    [DataField("size", customTypeSerializer: typeof(PrototypeIdSerializer<ItemSizePrototype>))]
    public string Size = "Huge";

    public bool Active = false;

    [DataField]
    public EntityUid? SleepAction;
}
