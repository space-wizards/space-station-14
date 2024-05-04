using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent, Access(typeof(ClusterGrenadeSystem))]
    public sealed partial class ClusterGrenadeComponent : Component
    {
        public Container GrenadesContainer = default!;

        /// <summary>
        ///     What we fill our prototype with if we want to pre-spawn with grenades.
        /// </summary>
        [DataField]
        public EntProtoId? FillPrototype;

        /// <summary>
        ///     If we have a pre-fill how many more can we spawn.
        /// </summary>
        public int UnspawnedCount;

        /// <summary>
        ///     Maximum grenades in the container.
        /// </summary>
        [DataField]
        public int Capacity = 3;

        /// <summary>
        ///     Maximum delay in seconds between individual grenade triggers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float GrenadeTriggerIntervalMax = 0f;

        /// <summary>
        ///     Minimum delay in seconds between individual grenade triggers
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float GrenadeTriggerIntervalMin = 0f;

        /// <summary>
        ///     Minimum delay in seconds before any grenades start to be triggered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float BaseTriggerDelay = 1.0f;

        /// <summary>
        ///     If the contents of the cluster can trigger, enable to activate their timers
        /// </summary>
        [DataField]
        public bool ActivateContentTimers = false;

        /// <summary>
        ///     Does the cluster grenade shoot or throw
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public GrenadeType GrenadeType = GrenadeType.Throw;

        /// <summary>
        ///     The speed at which grenades get thrown
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Velocity = 5;

        /// <summary>
        ///     Should the spread be random
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool RandomSpread = false;

        /// <summary>
        ///     Should the angle be random
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool RandomAngle = false;

        /// <summary>
        ///     Static distance grenades will be thrown to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Distance = 1f;

        /// <summary>
        ///     Max distance grenades should randomly be thrown to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MaxSpreadDistance = 2.5f;

        /// <summary>
        ///     Minimal distance grenades should randomly be thrown to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float MinSpreadDistance = 0f;
    }
}

public enum GrenadeType : byte
{
    Throw,
    Shoot
}
