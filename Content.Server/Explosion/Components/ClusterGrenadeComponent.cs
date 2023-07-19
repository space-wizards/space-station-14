using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent, Access(typeof(ClusterGrenadeSystem))]
    public sealed class ClusterGrenadeComponent : Component
    {
        public Container GrenadesContainer = default!;

        /// <summary>
        ///     What we fill our prototype with if we want to pre-spawn with grenades.
        /// </summary>
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;

        /// <summary>
        ///     If we have a pre-fill how many more can we spawn.
        /// </summary>
        public int UnspawnedCount;

        /// <summary>
        ///     Maximum grenades in the container.
        /// </summary>
        [DataField("maxGrenadesCount")]
        public int MaxGrenades = 3;

        /// <summary>
        ///     How long until our grenades are shot out and armed.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("delay")]
        public float Delay = 1;

        // <summary>
        ///     Maximum delay in milliseconds between grenade triggers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("bombletDelayMax")]
        public int BombletDelayMax = 900;

        // <summary>
        ///     Minimum delay in milliseconds between grenade triggers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("bombletDelayMin")]
        public int BombletDelayMin = 550;

        // <summary>
        ///     Minimum delay in milliseconds before bomblets start to be triggered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("minimumDelay")]
        public int MinimumDelay = 200;

        // <summary>
        ///     Decides if bomblets trigger after getting launched
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("triggerBomblets")]
        public bool TriggerBomblets = true;

        // <summary>
        ///     Does the grenade shoot or throw
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("grenadeType")]
        public string GrenadeType = "throw";

        // <summary>
        ///     The speed at which bomblets get thrown
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("bombletVelocity")]
        public float BombletVelocity = 5;

        // <summary>
        ///     Should the spread be random
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("randomSpread")]
        public bool RandomSpread = false;

        /// <summary>
        ///     Max distance grenades can be thrown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("distance")]
        public float ThrowDistance = 1;

        /// <summary>
        ///     This is the end.
        /// </summary>
        public bool CountDown;

        [ViewVariables(VVAccess.ReadWrite), DataField("releaseSound")]
        public SoundSpecifier? ReleaseSound = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");
    }
}
