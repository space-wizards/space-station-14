using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.Disposal.Components;

/// <summary>
/// Takes in entities and flushes them out to attached disposals tubes after a timer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class DisposalUnitComponent : Component
{
    public const string ContainerId = "disposals";

    /// <summary>
    /// Air contained in the disposal unit.
    /// </summary>
    [DataField]
    public GasMixture Air = new(Atmospherics.CellVolume);

    /// <summary>
    /// Name of the flushing animation state.
    /// </summary>
    [DataField]
    public string FlushingState = "disposal-flush";

    /// <summary>
    /// The animation used when unit flushes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public object FlushingAnimation = default!;

    /// <summary>
    /// Sounds played upon the unit flushing.
    /// </summary>
    [DataField("soundFlush"), AutoNetworkedField]
    public SoundSpecifier? FlushSound = new SoundPathSpecifier("/Audio/Machines/disposalflush.ogg");

    /// <summary>
    /// Blacklists (prevents) entities listed from being placed inside.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Whitelists (allows) entities listed from being placed inside.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Sound played when an object is inserted into the disposal unit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundInsert")]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Effects/trashbag1.ogg");

    /// <summary>
    /// State for this disposals unit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DisposalsPressureState State;

    /// <summary>
    /// Next time the disposal unit will be pressurized.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPressurized = TimeSpan.Zero;

    /// <summary>
    /// The percentage of pressure gained per second by the unit.
    /// </summary>
    [DataField]
    public float PressurePerSecond = 0.05f;

    /// <summary>
    /// How long it takes to flush a disposals unit manually.
    /// </summary>
    [DataField("flushTime")]
    public TimeSpan ManualFlushTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long it takes from the start of a flush animation to return the sprite to normal.
    /// </summary>
    [DataField]
    public TimeSpan FlushDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Removes the pressure requirement for flushing.
    /// </summary>
    [DataField]
    public bool DisablePressure;

    /// <summary>
    /// Last time that an entity tried to exit this disposal unit.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan LastExitAttempt;

    /// <summary>
    /// Delay in seconds before entities are allowed another
    /// attempt to exit the disposal unit again.
    /// </summary>
    [DataField]
    public TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Sets whether the unit should automatically flush
    /// after something is inserted into it.
    /// </summary>
    [DataField]
    public bool AutomaticEngage = true;

    /// <summary>
    /// Delay for automatic flushing of the disposal unit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutomaticEngageTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Delay for entities trying to enter disposals themselves.
    /// </summary>
    [DataField]
    public float EntryDelay = 0.5f;

    /// <summary>
    /// Delay from trying to shove someone else into disposals.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float DraggedEntryDelay = 2.0f;

    /// <summary>
    /// Container of entities inside this disposal unit.
    /// </summary>
    [ViewVariables] public Container Container = default!;

    /// <summary>
    /// Was the disposals unit engaged for a manual flush.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Engaged;

    /// <summary>
    /// Next time this unit will flush. Is the lesser of <see cref="FlushDelay"/> and <see cref="AutomaticEngageTime"/>
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextFlush;

    /// <summary>
    /// The max number of entities that can be inserted in the unit before it needs to be flushed.
    /// </summary>
    [DataField]
    public int MaxCapacity = 10;
}

/// <summary>
/// Event raised on entities that are entering or exiting disposals.
/// </summary>
[ByRefEvent]
public record DisposalSystemTransitionEvent;

/// <summary>
/// Do after raised when attempting to insert an entity into a disposal unit.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DisposalDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Message sent from the server to the client to update the UI of disposal units.
/// </summary>
[Serializable, NetSerializable]
public sealed class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState;

/// <summary>
/// Disposal unit pressure states.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalsPressureState : byte
{
    /// <summary>
    /// The disposal unit is fully pressurized and ready to flush.
    /// </summary>
    Ready,

    /// <summary>
    /// Has been flushed recently within FlushDelay.
    /// </summary>
    Flushed,

    /// <summary>
    /// FlushDelay has elapsed and now we're transitioning back to Ready.
    /// </summary>
    Pressurizing
}

/// <summary>
/// Visual keys for disposal units.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalUnitVisuals : byte
{
    IsReady,
    IsEngaged,
    IsFlushing,
    IsCharging,
    IsFull,
}

/// <summary>
/// Sprite layers for disposal units.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalUnitVisualLayers : byte
{
    Base,
    OverlayEngaged,
    OverlayCharging,
    OverlayFull,
}

/// <summary>
/// UI button types for disposal units.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalUnitUiButton : byte
{
    Eject,
    Engage,
    Power,
}

/// <summary>
/// Message data sent from client to server when a disposal unit ui button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class DisposalUnitUiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly DisposalUnitUiButton Button;

    public DisposalUnitUiButtonPressedMessage(DisposalUnitUiButton button)
    {
        Button = button;
    }
}

/// <summary>
/// Key for the disposal unit UI.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalUnitUiKey : byte
{
    Key
}
