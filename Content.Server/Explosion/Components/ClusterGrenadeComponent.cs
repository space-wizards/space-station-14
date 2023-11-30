using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent, Access(typeof(ClusterGrenadeSystem))]
    public sealed partial class ClusterGrenadeComponent : Component
    {
        public Container GrenadesContainer = default!;

        /// <summary>
        ///     What we fill our prototype with if we want to pre-spawn with grenades.
        /// </summary>
        [DataField("fillPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
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

        // <summary>
        ///     Maximum delay in milliseconds between grenade triggers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bombletDelayMax")]
        public float BombletDelayMax = 0f;

        // <summary>
        ///     Minimum delay in milliseconds between grenade triggers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bombletDelayMin")]
        public float BombletDelayMin = 0f;

        // <summary>
        ///     Minimum delay in milliseconds before bomblets start to be triggered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("minimumDelay")]
        public float MinimumDelay = 1.0f;

        // <summary>
        ///     Decides if bomblets trigger after getting launched
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("triggerBomblets")]
        public bool TriggerBomblets = true;

        // <summary>
        ///     Does the grenade shoot or throw
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("grenadeType")]
        public string GrenadeType = "throw";

        // <summary>
        ///     The speed at which bomblets get thrown
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("velocity")]
        public float Velocity = 5;

        // <summary>
        ///     Should the spread be random
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("randomSpread")]
        public bool RandomSpread = false;

        // <summary>
        ///     Should the angle be random
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("randomAngle")]
        public bool RandomAngle = false;

        /// <summary>
        ///     Static distance grenades will be thrown to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("distance")]
        public float Distance = 1f;
        /// <summary>
        ///     Max distance grenades should randomly be thrown to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxSpreadDistance")]
        public float MaxSpreadDistance = 2.5f;

        /// <summary>
        ///     Minimal distance grenades should randomly be thrown to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("minSpreadDistance")]
        public float MinSpreadDistance = 0f;

        /// <summary>
        ///     This is the end.
        /// </summary>
        public bool CountDown;
    }
}
