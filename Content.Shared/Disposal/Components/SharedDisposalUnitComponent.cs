using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Disposal.Components;

[NetworkedComponent]
public abstract partial class SharedDisposalUnitComponent : Component
{
    public const string ContainerId = "disposals";

    /// <summary>
    /// Sounds played upon the unit flushing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundFlush")]
    public SoundSpecifier? FlushSound = new SoundPathSpecifier("/Audio/Machines/disposalflush.ogg");

    /// <summary>
    /// Sound played when an object is inserted into the disposal unit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundInsert")]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Effects/trashbag1.ogg");

    /// <summary>
    /// Sound played when an item is thrown and misses the disposal unit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundMiss")]
    public SoundSpecifier? MissSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");


    /// <summary>
    /// State for this disposals unit.
    /// </summary>
    [DataField("state")]
    public DisposalsPressureState State;

    // TODO: Just make this use vaulting.
    /// <summary>
    /// We'll track whatever just left disposals so we know what collision we need to ignore until they stop intersecting our BB.
    /// </summary>
    [ViewVariables, DataField("recentlyEjected")]
    public List<EntityUid> RecentlyEjected = new();

    /// <summary>
    /// Next time the disposal unit will be pressurized.
    /// </summary>
    [DataField("nextPressurized", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextPressurized = TimeSpan.Zero;

    /// <summary>
    /// How long it takes to flush a disposals unit manually.
    /// </summary>
    [DataField("flushTime")]
    public TimeSpan ManualFlushTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long it takes from the start of a flush animation to return the sprite to normal.
    /// </summary>
    [DataField("flushDelay")]
    public TimeSpan FlushDelay = TimeSpan.FromSeconds(3);

    [DataField("mobsCanEnter")]
    public bool MobsCanEnter = true;

    /// <summary>
    /// Removes the pressure requirement for flushing.
    /// </summary>
    [DataField("disablePressure"), ViewVariables(VVAccess.ReadWrite)]
    public bool DisablePressure;

    /// <summary>
    ///     Last time that an entity tried to exit this disposal unit.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastExitAttempt;

    [DataField("autoEngageEnabled")]
    public bool AutomaticEngage = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("autoEngageTime")]
    public TimeSpan AutomaticEngageTime = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Delay from trying to enter disposals ourselves.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("entryDelay")]
    public float EntryDelay = 0.5f;

    /// <summary>
    ///     Delay from trying to shove someone else into disposals.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float DraggedEntryDelay = 2.0f;

    /// <summary>
    ///     Container of entities inside this disposal unit.
    /// </summary>
    [ViewVariables] public Container Container = default!;

    // TODO: Network power shit instead fam.
    [ViewVariables, DataField("powered")]
    public bool Powered;

    /// <summary>
    /// Was the disposals unit engaged for a manual flush.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("engaged")]
    public bool Engaged;

    /// <summary>
    /// Next time this unit will flush. Is the lesser of <see cref="FlushDelay"/> and <see cref="AutomaticEngageTime"/>
    /// </summary>
    [ViewVariables, DataField("nextFlush", customTypeSerializer:typeof(TimeOffsetSerializer))]
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
        Flushing,
        Charging
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

    [Serializable, NetSerializable]
    public sealed class DisposalUnitBoundUserInterfaceState : BoundUserInterfaceState, IEquatable<DisposalUnitBoundUserInterfaceState>
    {
        public readonly string UnitName;
        public readonly string UnitState;
        public readonly TimeSpan FullPressureTime;
        public readonly bool Powered;
        public readonly bool Engaged;

        public DisposalUnitBoundUserInterfaceState(string unitName, string unitState, TimeSpan fullPressureTime, bool powered,
            bool engaged)
        {
            UnitName = unitName;
            UnitState = unitState;
            FullPressureTime = fullPressureTime;
            Powered = powered;
            Engaged = engaged;
        }

        public bool Equals(DisposalUnitBoundUserInterfaceState? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UnitName == other.UnitName &&
                   UnitState == other.UnitState &&
                   Powered == other.Powered &&
                   Engaged == other.Engaged &&
                   FullPressureTime.Equals(other.FullPressureTime);
        }
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
