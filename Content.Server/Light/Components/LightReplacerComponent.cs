using System;
using System.Collections.Generic;
using Content.Shared.Light;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Device that allows user to quikly change bulbs in <see cref="PoweredLightComponent"/>
    ///     Can be reloaded by new light tubes or light bulbs
    /// </summary>
    [RegisterComponent]
    public class LightReplacerComponent : Component
    {
        public override string Name => "LightReplacer";

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
        public class LightReplacerEntity
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
