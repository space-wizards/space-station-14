using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Guardian
{
    /// <summary>
    /// Creates a GuardianComponent attached to the user's GuardianHost.
    /// </summary>
    [RegisterComponent]
    public sealed partial class GuardianCreatorComponent : Component
    {
        /// <summary>
        /// Counts as spent upon exhausting the injection
        /// </summary>
        /// <remarks>
        /// We don't mark as deleted as examine depends on this.
        /// </remarks>
        public bool Used = false;

        /// <summary>
        /// The prototype of the guardian entity which will be created
        /// </summary>
        [DataField("guardianProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
        public string GuardianProto { get; set; } = default!;

        /// <summary>
        /// How long it takes to inject someone.
        /// </summary>
        [DataField("delay")]
        public float InjectionDelay = 5f;
    }
}
