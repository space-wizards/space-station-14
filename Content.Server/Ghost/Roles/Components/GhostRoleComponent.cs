using Content.Server.Mind.Commands;
using Content.Server.PAI;
using Robust.Server.Player;
using Robust.Shared.Serialization;

namespace Content.Server.Ghost.Roles.Components
{
    [Access(typeof(GhostRoleSystem))]
    public abstract class GhostRoleComponent : Component
    {
        [DataField("name")] private string _roleName = "Unknown";

        [DataField("description")] private string _roleDescription = "Unknown";

        [DataField("rules")] private string _roleRules = "";

        [DataField("lotteryEnabled")] private bool _roleLotteryEnabled;

        /// <summary>
        /// Whether the <see cref="MakeSentientCommand"/> should run on the mob.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("makeSentient")]
        protected bool MakeSentient = true;

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
                var oldRoleName = _roleName;
                _roleName = value;

                EntitySystem.Get<GhostRoleSystem>().OnGhostRoleUpdate(this, oldRoleName);
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
                EntitySystem.Get<GhostRoleSystem>().OnGhostRoleUpdate(this, _roleName);
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public string RoleRules
        {
            get => _roleRules;
            set
            {
                _roleRules = value;
                EntitySystem.Get<GhostRoleSystem>().OnGhostRoleUpdate(this, _roleName);
            }
        }

        [ViewVariables(VVAccess.ReadOnly)]
        [Access(typeof(GhostRoleSystem), typeof(PAISystem), Other = AccessPermissions.Read)] // FIXME Friends
        public bool RoleLotteryEnabled
        {
            get => _roleLotteryEnabled;
            set
            {
                _roleLotteryEnabled = value;
                EntitySystem.Get<GhostRoleSystem>().OnGhostRoleUpdate(this, _roleName);
            }
        }

        [ViewVariables(VVAccess.ReadOnly)]
        public bool Taken { get; set; }

        /// <summary>
        /// If the ghost role currently in the queue for the lottery.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool PendingLottery { get; set; }

        /// <summary>
        /// If the ghost role is disabled because
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool Damaged { get; set; }

        /// <summary>
        /// If the ghost role is disabled due to being taken by a <see cref="GhostRoleGroupComponent"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool GhostRoleGroupTaken { get; set; }

        /// <summary>
        /// If the ghost role is available to be taken.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool Available => !(Taken || PendingLottery || Damaged || GhostRoleGroupTaken);

        /// <summary>
        /// Reregisters the ghost role when the current player ghosts.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("reregister")]
        public bool ReregisterOnGhost { get; set; } = true;

        public abstract bool Take(IPlayerSession session);
    }
}
