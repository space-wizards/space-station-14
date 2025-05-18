using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared.Clothing.Components
{
    /// <summary>
    /// Handle the hails (audible orders to stop) coming from a security gas mask / swat mask
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class SecurityHailerComponent : Component
    {
        /// <summary>
        /// 
        /// </summary>
        [DataField]
        public SecMaskState CurrentState = SecMaskState.Functional;

        /// <summary>
        /// Range value
        /// </summary>
        [DataField]
        public float Distance = 0;

        /// <summary>
        /// The name displayed as the speaker when hailing orders
        /// </summary>
        [DataField]
        public string? ChatName = "Security Hailer";

        /// <summary>
        /// Delay when the hailer is screwed to change aggression level
        /// </summary>
        [DataField]
        public float ScrewingDoAfterDelay = 1f;

        /// <summary>
        /// How aggresive are the orders coming from the hailer ? Higher means more aggressive / shitsec
        /// </summary>
        public enum AggresionState : byte
        {
            Low = 0,
            Medium = 1,
            High = 2
        }

        [DataField, AutoNetworkedField]
        public AggresionState AggresionLevel = AggresionState.Low;

        public SoundSpecifier LowAggressionSounds = new SoundCollectionSpecifier("SecHailLow");
        public SoundSpecifier MediumAggressionSounds = new SoundCollectionSpecifier("SecHailMedium");
        public SoundSpecifier HighAggressionSounds = new SoundCollectionSpecifier("SecHailHigh");
        public SoundSpecifier EmagAggressionSounds = new SoundCollectionSpecifier("SecHailEmag");
        public SoundSpecifier ScrewedSounds = new SoundCollectionSpecifier("Screwdriver"); //From the soundcollection of tools
        public SoundSpecifier CutSounds = new SoundCollectionSpecifier("Wirecutter"); //From the soundcollection of tools

        /// <summary>
        ///     The action that gets displayed when the gas mask is equipped.
        /// </summary>
        [DataField]
        public EntProtoId Action = "ActionSecHailer";

        /// <summary>
        ///     Reference to the action.
        /// </summary>
        [DataField]
        public EntityUid? ActionEntity;

        /// <summary>
        /// Entity prototype to spawn when used, using the whistle one
        /// </summary>
        [DataField]
        public EntProtoId ExclamationEffect = "WhistleExclamation";
    }

    [Serializable, NetSerializable]
    public enum SecMaskVisuals : byte
    {
        State
    }

    [Serializable, NetSerializable]
    public enum SecMaskState : byte
    {
        Functional,
        WiresCut
    }
}
