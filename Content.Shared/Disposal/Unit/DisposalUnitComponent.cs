using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Disposal.Components;

/// <summary>
/// Takes in entities and flushes them out to attached disposals tubes after a timer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DisposalUnitComponent : Component
{
    public const string ContainerId = "disposals";

    /// <summary>
    /// Air contained in the disposal unit.
    /// </summary>
    [DataField]
    public GasMixture Air = new(Atmospherics.CellVolume);

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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextPressurized = TimeSpan.Zero;

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
    [DataField, AutoNetworkedField]
    public TimeSpan LastExitAttempt;

    [DataField]
    public bool AutomaticEngage = true;

    [DataField, AutoNetworkedField]
    public TimeSpan AutomaticEngageTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Delay from trying to enter disposals ourselves.
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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? NextFlush;

    [Serializable, NetSerializable]
    public enum Visuals : byte
    {
        VisualState,
        Handle,
        Light
    }

    [Serializable, NetSerializable]
    public enum VisualState : byte
    {
        UnAnchored,
        Anchored,
        OverlayFlushing,
        OverlayCharging
    }

    [Serializable, NetSerializable]
    public enum HandleState : byte
    {
        Normal,
        Engaged
    }

    [Serializable, NetSerializable]
    [Flags]
    public enum LightStates : byte
    {
        Off = 0,
        Charging = 1 << 0,
        Full = 1 << 1,
        Ready = 1 << 2
    }

    [Serializable, NetSerializable]
    public enum UiButton : byte
    {
        Eject,
        Engage,
        Power
    }

    /// <summary>
    ///     Message data sent from client to server when a disposal unit ui button is pressed.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
    {
        public readonly UiButton Button;

        public UiButtonPressedMessage(UiButton button)
        {
            Button = button;
        }
    }

    [Serializable, NetSerializable]
    public enum DisposalUnitUiKey : byte
    {
        Key
    }
}

[Serializable, NetSerializable]
public enum DisposalsPressureState : byte
{
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
