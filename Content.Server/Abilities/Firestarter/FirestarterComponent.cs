using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities.Firestarter
{
    /// <summary>
    /// Lets its owner entity ignite flammables around it and also heal some damage.
    /// </summary>
    [RegisterComponent]
    public sealed partial class FirestarterComponent : Component
    {
        /// <summary>
        /// Radius of objects that will be ignited if flammable.
        /// </summary>
        [DataField("ignitionRadius")]
        public float IgnitionRadius = 4f;

        /// <summary>
        /// Healing when using action.
        /// </summary>
        [DataField("healingOnFire")]
        public DamageSpecifier HealingOnFire = new()
        {
            DamageDict = new()
        {
            { "Blunt", -8 },
            { "Slash", -8 },
            { "Piercing", -8 }
        }
        };

        /// <summary>
        /// The action entity.
        /// </summary>
        [DataField("fireStarterAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FireStarterAction = "ActionFireStarter";

        [DataField("fireStarterActionEntity")] public EntityUid? FireStarterActionEntity;


        /// <summary>
        /// Radius of objects that will be ignited if flammable.
        /// </summary>
        [DataField("igniteSound")]
        public SoundSpecifier IgniteSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");
    }
}
