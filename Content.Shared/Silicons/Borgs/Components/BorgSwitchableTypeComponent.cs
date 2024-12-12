using Content.Shared.Actions;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// Component for borgs that can switch their "type" after being created.
/// </summary>
/// <remarks>
/// <para>
/// This is used by all NT borgs, on construction and round-start spawn.
/// Borgs are effectively useless until they have made their choice of type.
/// Borg type selections are currently irreversible.
/// </para>
/// <para>
/// Available types are specified in <see cref="BorgTypePrototype"/>s.
/// </para>
/// </remarks>
/// <seealso cref="SharedBorgSwitchableTypeSystem"/>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedBorgSwitchableTypeSystem))]
public sealed partial class BorgSwitchableTypeComponent : Component
{
    /// <summary>
    /// Action entity used by players to select their type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SelectTypeAction;

    /// <summary>
    /// The currently selected borg type, if any.
    /// </summary>
    /// <remarks>
    /// This can be set in a prototype to immediately apply a borg type, and not have switching support.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public ProtoId<BorgTypePrototype>? SelectedBorgType;

    /// <summary>
    /// Radio channels that the borg will always have. These are added on top of the selected type's radio channels.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>[] InherentRadioChannels = [];
}

/// <summary>
/// Action event used to open the selection menu of a <see cref="BorgSwitchableTypeComponent"/>.
/// </summary>
public sealed partial class BorgToggleSelectTypeEvent : InstantActionEvent;

/// <summary>
/// UI message used by a borg to select their type with <see cref="BorgSwitchableTypeComponent"/>.
/// </summary>
/// <param name="prototype">The borg type prototype that the user selected.</param>
[Serializable, NetSerializable]
public sealed class BorgSelectTypeMessage(ProtoId<BorgTypePrototype> prototype) : BoundUserInterfaceMessage
{
    public ProtoId<BorgTypePrototype> Prototype = prototype;
}

/// <summary>
/// UI key used by the selection menu for <see cref="BorgSwitchableTypeComponent"/>.
/// </summary>
[NetSerializable, Serializable]
public enum BorgSwitchableTypeUiKey : byte
{
    SelectBorgType,
}
