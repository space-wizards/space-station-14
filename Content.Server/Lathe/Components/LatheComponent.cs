using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Content.Shared.Materials;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    public sealed class LatheComponent : SharedLatheComponent
    {
        /// <summary>
        /// Whitelist for specifying the kind of materials that can be insert into the lathe
        /// </summary>
        [ViewVariables]
        [DataField("whitelist")] 
        public EntityWhitelist? LatheWhitelist;

        /// <summary>
        /// Whitelist generated on runtime for what items are specifically used for the lathe's recipes.
        /// </summary>
        [ViewVariables]
        [DataField("materialWhiteList", customTypeSerializer: typeof(PrototypeIdListSerializer<MaterialPrototype>))]
        public List<string> MaterialWhiteList = new();

        /// <summary>
        /// The lathe's construction queue
        /// </summary>
        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new();
        /// <summary>
        /// The recipe the lathe is currently producing
        /// </summary>
        [ViewVariables]
        public LatheRecipePrototype? ProducingRecipe;
        /// <summary>
        /// How long the inserting animation will play
        /// </summary>
        [ViewVariables]
        public float InsertionTime = 0.79f; // 0.01 off for animation timing
        /// <summary>
        /// Update accumulator for the insertion time
        /// </suummary>
        [DataField("insertionAccumulator")]
        public float InsertionAccumulator = 0f;
        /// <summary>
        /// Production accumulator for the production time.
        /// </summary>
        [ViewVariables]
        [DataField("producingAccumulator")]
        public float ProducingAccumulator = 0f;

        /// <summary>
        /// The sound that plays when the lathe is producing an item, if any
        /// </summary>
        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;
        
        /// <summary>
        /// The sound that plays when inserting an item into the lathe, if any
        /// </summary>
        [DataField("insertingSound")]
        public SoundSpecifier? InsertingSound;

        /// <summmary>
        /// The lathe's UI.
        /// </summary>
        [ViewVariables] public BoundUserInterface? UserInterface;
    }
}
