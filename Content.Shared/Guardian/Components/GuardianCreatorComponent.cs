using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Guardian.Components
{
    /// <summary>
    /// Creates a GuardianComponent attached to the user's GuardianHost.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class GuardianCreatorComponent : Component
    {
        /// <summary>
        /// Counts as spent upon exhausting the injection
        /// </summary>
        /// <remarks>
        /// We don't mark as deleted as examine depends on this.
        /// </remarks>
        [DataField, AutoNetworkedField]
        public bool Used;

        /// <remarks>
        /// For other methods that are not done through injection.
        /// </remarks>
        [DataField, AutoNetworkedField]
        public bool Magical;

        /// <summary>
        /// The prototype of the guardian entity which will be created
        /// </summary>
        [DataField("guardianProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
        public string GuardianProto { get; set; } = null!;

        /// <summary>
        /// How long it takes to inject someone.
        /// </summary>
        [DataField("delay")]
        public float InjectionDelay = 5f;
    }
}
