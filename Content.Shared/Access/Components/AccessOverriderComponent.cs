using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

/// <summary>
/// Allows this item to be used to configure the access of devices with an <see cref="AccessReaderComponent">.
/// Also known as the access configurator.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedAccessOverriderSystem))]
public sealed partial class AccessOverriderComponent : Component
{
    /// <summary>
    /// ItemSlot identifier for the ID card.
    /// </summary>
    public const string PrivilegedIdCardSlotId = "AccessOverrider-privilegedId";

    /// <summary>
    /// If the Access Overrider UI will show info about the privileged ID
    /// </summary>
    [DataField]
    public bool ShowPrivilegedId = true;

    /// <summary>
    /// Actual item slot that the ID card is put into.
    /// </summary>
    [DataField]
    public ItemSlot PrivilegedIdSlot = new();

    /// <summary>
    /// The uid of the entity the configurator was used on. If null, it was used
    /// in-hand with no target.
    /// </summary>
    /// <remarks>
    /// TODO: use WeakEntityReference
    /// </remarks>
    [DataField, AutoNetworkedField]
    public EntityUid? TargetAccessReaderId;

    /// <summary>
    /// The set of access levels that this access configurator can modify. The
    /// user still needs an ID card with matching access to actually set those
    /// levels, however.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> AccessLevels = [];

    /// <summary>
    /// The duration of the doafter from clicking on an entity.
    /// </summary>
    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(0.5);
}

/// <summary>
/// Raised from the client to the server when a new access selection is made.
/// </summary>
/// <param name="accessList">
/// The set of accesses that the targeted device should be set to.
/// </param>
[Serializable, NetSerializable]
public sealed class SetAccessesMessage(List<ProtoId<AccessLevelPrototype>> accessList) : BoundUserInterfaceMessage
{
    public readonly List<ProtoId<AccessLevelPrototype>> AccessList = accessList;
}

[Serializable, NetSerializable]
public enum AccessOverriderUiKey : byte
{
    Key,
}
