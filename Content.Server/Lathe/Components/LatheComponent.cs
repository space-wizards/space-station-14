using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Audio;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    public sealed class LatheComponent : SharedLatheComponent
    {
        /// <summary>
        /// The lathe's construction queue
        /// </summary>
        [DataField("queue", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        public List<string> Queue { get; } = new();
        // TODO queue serializer.

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
        /// The sound that plays when the lathe is producing an item, if any
        /// </summary>
        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;

        /// <summmary>
        /// The lathe's UI.
        /// </summary>
        [ViewVariables] public BoundUserInterface? UserInterface;
    }
}
