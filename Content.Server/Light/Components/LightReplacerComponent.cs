using Content.Shared.Light;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Device that allows user to quikly change bulbs in <see cref="PoweredLightComponent"/>
    ///     Can be reloaded by new light tubes or light bulbs
    /// </summary>
    [RegisterComponent]
    public sealed class LightReplacerComponent : Component
    {
        [DataField("sound")]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/click.ogg");

        /// <summary>
        /// Bulbs that were inside light replacer when it spawned
        /// </summary>
        [DataField("contents")]
        public List<LightReplacerEntity> Contents = new();

        /// <summary>
        /// Bulbs that were inserted inside light replacer
        /// </summary>
        [ViewVariables]
        public IContainer InsertedBulbs = default!;

        [Serializable]
        [DataDefinition]
        public sealed class LightReplacerEntity
        {
            [DataField("name", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
            public string PrototypeName = default!;

            [DataField("amount")]
            public int Amount;

            [DataField("type")]
            public LightBulbType Type;
        }
    }
}
