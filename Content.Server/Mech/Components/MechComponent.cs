using System.Threading;
using Content.Server.Atmos;
using Content.Shared.Mech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Mech.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedMechComponent))]
public sealed class MechComponent : SharedMechComponent
{
    /// <summary>
    /// How long it takes to enter the mech.
    /// </summary>
    [DataField("entryDelay")]
    public float EntryDelay = 3;

    /// <summary>
    /// How long it takes to pull *another person*
    /// outside of the mech. You can exit instantly yourself.
    /// </summary>
    [DataField("exitDelay")]
    public float ExitDelay = 3;

    /// <summary>
    /// How long it takes to pull out the battery.
    /// </summary>
    [DataField("batteryRemovalDelay")]
    public float BatteryRemovalDelay = 2;

    public CancellationTokenSource? EntryTokenSource;

    /// <summary>
    /// Whether or not the mech is airtight.
    /// </summary>
    /// <remarks>
    /// This needs to be redone
    /// when mech internals are added
    /// </remarks>
    [DataField("airtight"), ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight;

    /// <summary>
    /// The equipment that the mech initially has when it spawns.
    /// Good for things like nukie mechs that start with guns.
    /// </summary>
    [DataField("startingEquipment", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingEquipment = new();

    /// <summary>
    /// The battery the mech initially has when it spawns
    /// Good for admemes and nukie mechs.
    /// </summary>
    [DataField("startingBattery", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? StartingBattery;

    //TODO: this doesn't support a tank implant for mechs or anything like that
    [ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Air = new (GasMixVolume);
    public const float GasMixVolume = 70f;
}

/// <summary>
/// Event raised when a person successfully enters a mech
/// </summary>
public sealed class MechEntryFinishedEvent : EntityEventArgs
{
    public EntityUid User;

    public MechEntryFinishedEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Event raised when a person fails to enter a mech
/// </summary>
public sealed class MechEntryCanclledEvent : EntityEventArgs
{

}

/// <summary>
/// Event raised when a person successfully removes someone from a mech
/// </summary>
public sealed class MechExitFinishedEvent : EntityEventArgs
{

}

/// <summary>
/// Event raised when a person fails to remove someone from a mech
/// </summary>
public sealed class MechExitCanclledEvent : EntityEventArgs
{

}

/// <summary>
/// Event raised when the battery is successfully removed from the mech
/// </summary>
public sealed class MechRemoveBatteryFinishedEvent : EntityEventArgs
{

}

/// <summary>
/// Event raised when the battery fails to be removed from the mech
/// </summary>
public sealed class MechRemoveBatteryCancelledEvent : EntityEventArgs
{

}
