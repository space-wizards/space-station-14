using Content.Shared.Research.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Lathe.Components
{
    /// <summary>
    /// For EntityQuery to keep track of which lathes are producing
    /// <summary>
    [RegisterComponent]
    public sealed class LatheProducingComponent : Component
    {
        /// <summary>
        /// The recipe the lathe is currently producing
        /// </summary>
        [DataField("recipe", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<LatheRecipePrototype>))]
        public string? Recipe;

        /// <summary>
        /// Remaining production time, in seconds.
        /// </summary>
        [DataField("timeRemaining", required: true)]
        public float TimeRemaining;
    }
}
