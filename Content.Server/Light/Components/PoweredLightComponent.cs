using System;
using Content.Server.Light.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Light;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent, Friend(typeof(PoweredLightSystem))]
    public class PoweredLightComponent : Component
    {
        public override string Name => "PoweredLight";

        [DataField("burnHandSound")]
        public SoundSpecifier BurnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        [DataField("turnOnSound")]
        public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/light_tube_on.ogg");

        [DataField("hasLampOnSpawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? HasLampOnSpawn = null;

        [DataField("bulb")]
        public LightBulbType BulbType;

        [ViewVariables]
        [DataField("on")]
        public bool On = true;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [ViewVariables]
        [DataField("ignoreGhostsBoo")]
        public bool IgnoreGhostsBoo;

        [ViewVariables]
        [DataField("ghostBlinkingTime")]
        public TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);

        [ViewVariables]
        [DataField("ghostBlinkingCooldown")]
        public TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);

        [ViewVariables]
        public ContainerSlot LightBulbContainer = default!;
        [ViewVariables]
        public bool CurrentLit;
        [ViewVariables]
        public bool IsBlinking;
        [ViewVariables]
        public TimeSpan LastThunk;
        [ViewVariables]
        public TimeSpan? LastGhostBlink;
    }
}
