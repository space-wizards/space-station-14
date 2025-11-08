using System;
using System.Collections.Generic;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.RespawnMachine
{
    [RegisterComponent]
    public sealed partial class RespawnMachineComponent : Component
    {
        /// <summary>
        /// The job prototype id that this machine will respawn.
        /// Only players whose job matches this will be queued/respawned by this machine.
        /// </summary>
        [DataField("job")]
        public ProtoId<JobPrototype>? Job;

        /// <summary>
        /// Delay before respawning a player queued at this machine. If zero, respawn is immediate.
        /// </summary>
        [DataField("respawnDelay")]
        public TimeSpan RespawnDelay = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether this machine is currently enabled and will accept respawns.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        /// Map of players queued for respawn and the time at which they should be respawned.
        /// </summary>
        [ViewVariables]
        public Dictionary<NetUserId, TimeSpan> Queue { get; } = new();
    }
}
