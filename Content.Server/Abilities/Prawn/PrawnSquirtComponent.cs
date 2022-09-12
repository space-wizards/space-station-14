using Content.Shared.Actions.ActionTypes;
using Content.Shared.Dataset;
using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities
{
    [RegisterComponent]
    public sealed class PrawnSquirtComponent : Component
    {
        [DataField("actionPrawnSquirt", required: true)]
        public InstantAction ActionPrawnSquirt = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("ThirstPerSquirtUse", required: true)]
        public float ThirstPerSquirtUse = 15f;

        [ViewVariables(VVAccess.ReadWrite), DataField("PuddleBrineId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PuddleBrineId = "PuddleBrine";

        [DataField("squirtsound")]
        public SoundSpecifier SquirtSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
    }
};