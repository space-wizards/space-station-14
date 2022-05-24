using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing.Components;

[RegisterComponent]
[Friend(typeof(SharedChameleonClothingSystem))]
public sealed class ChameleonClothingComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("slot", required: true)]
    public SlotFlags Slot;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("default", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SelectedId = default!;
}

[Serializable, NetSerializable]
public sealed class ChameleonBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly SlotFlags Slot;
    public readonly string SelectedId;

    public ChameleonBoundUserInterfaceState(SlotFlags slot, string selectedId)
    {
        Slot = slot;
        SelectedId = selectedId;
    }
}

[Serializable, NetSerializable]
public sealed class ChameleonPrototypeSelectedMessage: BoundUserInterfaceMessage
{
    public readonly string SelectedId;

    public ChameleonPrototypeSelectedMessage(string selectedId)
    {
        SelectedId = selectedId;
    }
}

[Serializable, NetSerializable]
public enum ChameleonVisuals : byte
{
    ClothingId
}

[Serializable, NetSerializable]
public enum ChameleonUiKey : byte
{
    Key
}
