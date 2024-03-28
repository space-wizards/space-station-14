using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     Allow players to change clothing sprite to any other clothing prototype.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedChameleonClothingSystem))]
public sealed partial class ChameleonClothingComponent : Component
{
    /// <summary>
    ///     Filter possible chameleon options by their slot flag.
    /// </summary>
    [ViewVariables]
    [DataField(required: true)]
    public SlotFlags Slot;

    /// <summary>
    ///     EntityPrototype id that chameleon item is trying to mimic.
    /// </summary>
    [ViewVariables]
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Default;

    [ViewVariables]
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     Current user that wears chameleon clothing.
    /// </summary>
    [ViewVariables]
    public EntityUid? User;
}

[Serializable, NetSerializable]
public sealed class ChameleonBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly SlotFlags Slot;
    public readonly EntProtoId? SelectedId;
    public readonly EntityWhitelist? Whitelist;

    public ChameleonBoundUserInterfaceState(SlotFlags slot, EntProtoId? selectedId, EntityWhitelist? whitelist)
    {
        Slot = slot;
        SelectedId = selectedId;
        Whitelist = whitelist;
    }
}

[Serializable, NetSerializable]
public sealed class ChameleonPrototypeSelectedMessage : BoundUserInterfaceMessage
{
    public readonly EntProtoId SelectedId;

    public ChameleonPrototypeSelectedMessage(EntProtoId selectedId)
    {
        SelectedId = selectedId;
    }
}

[Serializable, NetSerializable]
public enum ChameleonUiKey : byte
{
    Key
}
