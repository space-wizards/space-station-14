using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Lathe
{
    [RegisterComponent, NetworkedComponent]
    public sealed class LatheComponent : Component
    {
        /// <summary>
        /// All of the recipes that the lathe has by default
        /// </summary>
        [DataField("staticRecipes", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        public readonly List<string> StaticRecipes = new();

        /// <summary>
        /// All of the recipes that the lathe is capable of researching
        /// </summary>
        [DataField("dynamicRecipes", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        public readonly List<string>? DynamicRecipes;

        /// <summary>
        /// The lathe's construction queue
        /// </summary>
        [DataField("queue")]
        public List<LatheRecipePrototype> Queue = new();

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

        /// <summary>
        /// The recipe the lathe is currently producing
        /// </summary>
        [ViewVariables]
        public LatheRecipePrototype? CurrentRecipe;


    }

    public sealed class LatheGetRecipesEvent : EntityEventArgs
    {
        public readonly EntityUid Lathe;

        public List<string> Recipes = new();

        public LatheGetRecipesEvent(EntityUid lathe)
        {
            Lathe = lathe;
        }
    }
}
