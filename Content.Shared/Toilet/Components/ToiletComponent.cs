using Robust.Shared.Audio;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;


namespace Content.Shared.Toilet.Components
{
    /// <summary>
    /// Toilets that can be flushed, seats toggled up and down, items hidden in cistern.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ToiletComponent : Component
    {
        /// <summary>
        /// Toggles seat state.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool ToggleSeat;

        /// <summary>
        /// Toggles Lid state.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool ToggleLid;

        /// <summary>
        /// Sound to play when toggling toilet seat.
        /// </summary>
        [DataField]
        public SoundSpecifier SeatSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");

        /// <summary>
        /// Prying the lid.
        /// </summary>
        [DataField]
        public float PryLidTime = 1f;

        [DataField]
        public ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    }

    [Serializable, NetSerializable]
    public sealed partial class ToiletPryDoAfterEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    public enum ToiletVisuals : byte
    {
        SeatVisualState,
        LidVisualState
    }

    [Serializable, NetSerializable]
    public enum LidVisualState : byte
    {
        LidOpen,
        LidClosed
    }

    [Serializable, NetSerializable]
    public enum SeatVisualState : byte
    {
        SeatUp,
        SeatDown
    }
}

