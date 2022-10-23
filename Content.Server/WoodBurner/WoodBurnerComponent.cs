using Content.Server.Atmos;
using Content.Shared.Atmos;
//using Content.Shared.WoodBurner;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Server.WoodBurner
{
    [RegisterComponent]
    public sealed class WoodBurnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled = false;

        [DataField("inlet")]
        public string InletName = "pipe";

        /// <summary>
        /// How hot is gas that will be released.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outputGasTemperature")]
        public float OutputGasTemperature { get; set; } = 1000f;

        /// <summary>
        /// How much gas will be released.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outputGasAmount")]
        public float OutputGasAmount { get; set; } = Atmospherics.MolesCellStandard * 0.0005f;

        /// <summary>
        /// How long the inserting animation will play.
        /// </summary>
        [DataField("insertionTime")]
        public float InsertionTime = 0.79f; // 0.01 off for animation timing.

        /// <summary>
        /// The sound that plays when the lathe is producing an item, if any.
        /// </summary>
        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;

        /// <summary>
        /// Wood burner will convert wood into charcoal if timer reach 0.
        /// </summary>
        [DataField("processingTimer")]
        public float ProcessingTimer = 0;

        [DataField("processingTimerMax")]
        public float ProcessingTimerMax = 15;

        [DataField("baseProcessingTimerMax")]
        public float BaseProcessingTimerMax = 15;

        [DataField("processingSpeedMultiplier")]
        public float ProcessingSpeedMultiplier = 1;

        /// <summary>
        /// The machine part that increses the processing speed.
        /// </summary>
        [DataField("machinePartProcessingTimer", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartProcessingSpeed = "Laser";

        /// <summary>
        /// Lower number = faster processing.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseProcessingSpeedMultiplier = 0.4f;

        /// <summary>
        /// A modifier that changes how much of a material is needed to print a recipe
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaterialUseMultiplier = 1;

        /// <summary>
        /// The machine part that reduces how much material it takes to print a recipe.
        /// </summary>
        [DataField("machinePartMaterialUse", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMaterialUse = "MatterBin";

        /// <summary>
        /// The value that is used to calculate the modifier <see cref="MaterialUseMultiplier"/>
        /// </summary>
        [DataField("partRatingMaterialUseMultiplier")]
        public float PartRatingMaterialUseMultiplier = 0.75f;

        /*
        #region Visualizer info
        [DataField("idleState", required: true)]
        public string IdleState = default!;

        [DataField("runningState", required: true)]
        public string RunningState = default!;
        #endregion
        */
    }
}
