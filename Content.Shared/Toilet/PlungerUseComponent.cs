using Robust.Shared.Audio;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Random;

namespace Content.Shared.Toilet
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class PlungerUseComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public bool Plunged;

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
