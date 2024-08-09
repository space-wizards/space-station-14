using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Plunger.Components
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
        [DataField]
        [AutoNetworkedField]
        public bool Plunged;

        /// <summary>
        /// If true entity can interact with plunger.
        /// </summary>
        [DataField]
        [AutoNetworkedField]
        public bool NeedsPlunger = false;

        /// <summary>
        /// A weighted random entity prototype containing the different loot that rummaging can provide.
        /// </summary>
        [DataField]
        [AutoNetworkedField]
        public ProtoId<WeightedRandomEntityPrototype> PlungerLoot = "PlungerLoot";


        /// <summary>
        /// Sound played on rummage completion.
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/glug.ogg");
    }
}
