using Robust.Shared.Audio;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Toilet
{
    /// <summary>
    /// Toilets that can be flushed, seats toggled up and down, items hidden in cistern.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ToiletComponent : Component
    {
        /// <summary>
        /// Sound for toilet seat interaction.
        /// </summary>
        [DataField, AutoNetworkedField]
        public SoundSpecifier? Sound;

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
        /// Prying the lid.
        /// </summary>
        [DataField]
        public float PryLidTime = 1f;

        [DataField]
        public ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

        /// <summary>
        /// The state for when the toilet seat is up.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public string? SeatUp;

        /// <summary>
        /// The state for when the toilet seat is down.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public string? SeatDown;

        /// <summary>
        /// The state for when the lid is open.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public string? LidOpen;

        /// <summary>
        /// The state for when the lid is closed.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public string? LidClosed;
    }

    [Serializable, NetSerializable]
    public sealed partial class ToiletPryDoAfterEvent : SimpleDoAfterEvent
    {
    }

    public enum ToiletVisualLayers
    {
        Door,
        Lid
    }
}
