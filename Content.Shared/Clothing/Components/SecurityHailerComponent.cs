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

        public enum SpecialUseCase : byte
        {
            None = 0,
            HOS = 1,
            ERT = 2
        }

        /// <summary>
        /// Special use cases where some voicelines shouldn't play or we want to play some other voicelines, ex: HOS or ERT
        /// </summary>
        [DataField]
        public SpecialUseCase SpecialCircumtance = SpecialUseCase.None;

        /// <summary>
        /// What ftl line to replace in special circumstances
        /// </summary>
        public Dictionary<SpecialUseCase, (string[], string[])> ReplaceVoicelinesSpecial = new() //List of Tuples
        {
            { SpecialUseCase.HOS, (["hail-high-8"], ["hail-high-HOS"]) }//"Take it to the HOS voice" line, make no sense if HOS
        };

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
        public SoundSpecifier ERTAggressionSounds = new SoundCollectionSpecifier("SecHailERT");
        public SoundSpecifier HOSReplaceSounds = new SoundCollectionSpecifier("SecHailHOS");
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
