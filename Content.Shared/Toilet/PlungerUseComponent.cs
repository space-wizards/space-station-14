using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Random;

namespace Content.Shared.Toilet
{
    /// <summary>
    /// Entity can interact with plungers.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class PlungerUseComponent : Component
    {
        /// <summary>
        /// If true entity has been plungered.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public bool Plunged;

        /// <summary>
        /// If true entity can interact with plunger.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public bool NeedsPlunger = false;

        /// <summary>
        /// A weighted random entity prototype containing the different loot that rummaging can provide.
        /// </summary>
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public string PlungerLoot = "PlungerLoot";

        /// <summary>
        /// Sound played on rummage completion.
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/glug.ogg");
    }
}
