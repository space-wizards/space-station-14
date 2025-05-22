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
        /// The person wearing the mask
        /// </summary>
        public EntityUid User = EntityUid.Invalid;

        /// <summary>
        /// State of the sec mask to check if it can hail
        /// </summary>
        [DataField]
        public SecMaskState CurrentState = SecMaskState.Functional;

        /// <summary>
        /// Range value for the exclamation effect on humanoids
        /// </summary>
        [DataField]
        public float Distance = 0f;

        /// <summary>
        /// The name displayed as the speaker when hailing orders
        /// </summary>
        [DataField]
        public string? ChatName = "Security hailer";

        /// <summary>
        /// Delay when the hailer is screwed to change aggression level
        /// </summary>
        [DataField]
        public float ScrewingDoAfterDelay = 3f;

        /// <summary>
        /// Delay when the hailer has its wires cut
        /// </summary>
        [DataField]
        public float CuttingDoAfterDelay = 5f;

        /// <summary>
        /// How long until you can use the verb again to change aggression level
        /// </summary>
        [DataField]
        public TimeSpan VerbCooldown = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Time where the verb will be ready to be used again
        /// </summary>
        public TimeSpan TimeVerbReady = TimeSpan.Zero;

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
            { SpecialUseCase.HOS, (["hail-high-5"], ["hail-high-HOS"]) }//"Take it to the HOS voice" line, make no sense if HOS
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
        public SoundSpecifier SettingBeep = new SoundCollectionSpecifier("CargoToggleLimit"); //Beep when hailer is used with verb. In machines.yml
        public SoundSpecifier SettingError = new SoundCollectionSpecifier("CargoError"); //Beep when hailer is used with verb  and it FAILS !! In machines.yml

        /// <summary>
        /// The action that gets displayed when the gas mask is equipped.
        /// </summary>
        [DataField]
        public EntProtoId Action = "ActionSecHailer";

        /// <summary>
        /// Reference to the action.
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
