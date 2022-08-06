using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Recycling.Components
{
    [RegisterComponent, Access(typeof(RecyclerSystem))]
    public sealed class RecyclableComponent : Component
    {
        /// <summary>
        ///     The prototype that will be spawned on recycle.
        /// </summary>
        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))] public string? Prototype;

        /// <summary>
        ///     The amount of things that will be spawned on recycle.
        /// </summary>
        [DataField("amount")] public int Amount = 1;

        /// <summary>
        ///     Whether this is "safe" to recycle or not.
        ///     If this is false, the recycler's safety must be disabled to recycle it.
        /// </summary>
        [DataField("safe")]
        public bool Safe { get; set; } = true;
    }
}
