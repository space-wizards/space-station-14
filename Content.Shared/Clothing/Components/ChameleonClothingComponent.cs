using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
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
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField(required: true)]
    public SlotFlags Slot;

    /// <summary>
    ///     EntityPrototype id that chameleon item is trying to mimic.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Default;

    /// <summary>
    ///     Current user that wears chameleon clothing.
    /// </summary>
    [ViewVariables]
    public EntityUid? User;

    /// <summary>
    ///     Filter possible chameleon options by a tag in addition to WhitelistChameleon.
    /// </summary>
    [DataField]
    public string? RequireTag;
}

[Serializable, NetSerializable]
public sealed class ChameleonBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly SlotFlags Slot;
    public readonly string? SelectedId;
    public readonly string? RequiredTag;

    public ChameleonBoundUserInterfaceState(SlotFlags slot, string? selectedId, string? requiredTag)
    {
        Slot = slot;
        SelectedId = selectedId;
        RequiredTag = requiredTag;
    }
}

[Serializable, NetSerializable]
public sealed class ChameleonPrototypeSelectedMessage : BoundUserInterfaceMessage
{
    public readonly string SelectedId;

    public ChameleonPrototypeSelectedMessage(string selectedId)
    {
        SelectedId = selectedId;
    }
}

[Serializable, NetSerializable]
public enum ChameleonUiKey : byte
{
    Key
}
