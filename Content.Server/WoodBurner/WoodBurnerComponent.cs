using Content.Server.Atmos;
using Content.Shared.Atmos;
//using Content.Shared.WoodBurner;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Audio;

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
        /// How hot is gas that will be released 
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outputGasTemperature")]
        public float OutputGasTemperature { get; set; } = 1000f;

        /// <summary>
        /// How much gas will be released 
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outputGasAmount")]
        public float OutputGasAmount { get; set; } = Atmospherics.MolesCellStandard * 0.0005f;

        /// <summary>
        /// How long the inserting animation will play
        /// </summary>
        [DataField("insertionTime")]
        public float InsertionTime = 0.79f; // 0.01 off for animation timing

        /// <summary>
        /// The sound that plays when the lathe is producing an item, if any
        /// </summary>
        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;

        /// <summary>
        /// Wood burner will convert wood into charcoal if timer reach 0
        /// </summary>
        [DataField("processingTimer")]
        public float ProcessingTimer = 5;

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
