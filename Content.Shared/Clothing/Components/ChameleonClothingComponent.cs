using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     Allow players to change clothing sprite to any other clothing prototype.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedChameleonClothingSystem))]
public sealed partial class ChameleonClothingComponent : Component
{
    /// <summary>
    ///     Filter possible chameleon options by their slot flag.
    /// </summary>
    [DataField(required: true)]
    public SlotFlags Slot;

    /// <summary>
    ///     EntityPrototype id that chameleon item is trying to mimic.
    /// </summary>
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

    /// <summary>
    ///     Will component owner be affected by EMP pulses?
    /// </summary>
    [DataField]
    public bool AffectedByEmp = true;

    /// <summary>
    ///     Intensity of clothes change on EMP.
    ///     Can be interpreted as "How many times clothes will change every second?".
    ///     Useless without <see cref="AffectedByEmp"/> set to true.
    /// </summary>
    [DataField]
    public int EmpChangeIntensity = 7;

    /// <summary>
    ///     Should the EMP-change happen continuously, or only once?
    ///     (False = once, True = continuously)
    ///     Useless without <see cref="AffectedByEmp"/>
    /// </summary>
    [DataField]
    public bool EmpContinuous = true;

    /// <summary>
    ///     When should next EMP-caused appearance change happen?
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmpChange = TimeSpan.Zero;
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
