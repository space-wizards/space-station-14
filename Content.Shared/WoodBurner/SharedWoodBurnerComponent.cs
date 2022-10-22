using Content.Shared.Atmos;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.WoodBurner
{
    // Copy paste from Lathe
    // TO-DO --- Change all of this

    [RegisterComponent]
    public class SharedWoodBurnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled = true;

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
        public float OutputGasAmount { get; set; } = Atmospherics.MolesCellStandard * 20f;

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

        #region Visualizer info
        [DataField("idleState", required: true)]
        public string IdleState = default!;

        [DataField("runningState", required: true)]
        public string RunningState = default!;

        [ViewVariables]
        [DataField("ignoreColor")]
        public bool IgnoreColor;
        #endregion


    }
    /*
    public sealed class LatheGetRecipesEvent : EntityEventArgs
    {
        public readonly EntityUid Lathe;

        public List<string> Recipes = new();

        public LatheGetRecipesEvent(EntityUid lathe)
        {
            Lathe = lathe;
        }
    }
    */
}
