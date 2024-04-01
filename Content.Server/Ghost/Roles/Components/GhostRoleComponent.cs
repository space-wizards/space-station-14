using Content.Server.Mind.Commands;
using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ghost.Roles.Components
{
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed partial class GhostRoleComponent : Component
    {
        [DataField("name")] private string _roleName = "Unknown";

        [DataField("description")] private string _roleDescription = "Unknown";

        [DataField("rules")] private string _roleRules = "ghost-role-component-default-rules";

        [DataField("requirements")]
        public HashSet<JobRequirement>? Requirements;

        /// <summary>
        /// Whether the <see cref="MakeSentientCommand"/> should run on the mob.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("makeSentient")]
        public bool MakeSentient = true;

        /// <summary>
        ///     The probability that this ghost role will be available after init.
        ///     Used mostly for takeover roles that want some probability of being takeover, but not 100%.
        /// </summary>
        [DataField("prob")]
        public float Probability = 1f;

        // We do this so updating RoleName and RoleDescription in VV updates the open EUIs.

        [ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public string RoleName
        {
            get => Loc.GetString(_roleName);
            set
            {
                _roleName = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public string RoleDescription
        {
            get => Loc.GetString(_roleDescription);
            set
            {
                _roleDescription = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public string RoleRules
        {
            get => Loc.GetString(_roleRules);
            set
            {
                _roleRules = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

        [DataField("allowSpeech")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool AllowSpeech { get; set; } = true;

        [DataField("allowMovement")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool AllowMovement { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public bool Taken { get; set; }

        [ViewVariables]
        public uint Identifier { get; set; }

        /// <summary>
        /// Reregisters the ghost role when the current player ghosts.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("reregister")]
        public bool ReregisterOnGhost { get; set; } = true;

        /// <summary>
        /// If true, ghost role is raffled, otherwise it is "first come first served".
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("raffle")]
        public bool Raffle { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("raffleDecider", customTypeSerializer: typeof(PrototypeIdSerializer<GhostRoleRaffleDeciderPrototype>))]
        public string RaffleDecider { get; set; } = "default";

        /// <summary>
        /// The initial duration of a raffle in seconds. This is the countdown timer's value when the raffle starts.
        /// Not used if <see cref="Raffle"/> is false.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("raffleInitialDuration")]
        public uint RaffleInitialDuration { get; set; } = 30;

        /// <summary>
        /// When the raffle is joined by a player, the countdown timer is extended by this value in seconds.
        /// Not used if <see cref="Raffle"/> is false.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("raffleJoinExtendsDurationBy")]
        public uint RaffleJoinExtendsDurationBy { get; set; } = 10;

        /// <summary>
        /// The maximum duration in seconds for the ghost role raffle. A raffle cannot run for longer than this
        /// duration, even if extended by joiners. Must be greater than or equal to <see cref="RaffleInitialDuration"/>.
        /// Not used if <see cref="Raffle"/> is false.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("raffleMaxDuration")]
        public uint RaffleMaxDuration { get; set; } = 120;
    }
}
