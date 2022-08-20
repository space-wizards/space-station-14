using Content.Server.Mind.Commands;
using Content.Server.PAI;
using Robust.Server.Player;
using Robust.Shared.Serialization;

namespace Content.Server.Ghost.Roles.Components
{
    [Access(typeof(GhostRoleSystem))]
    public abstract class GhostRoleComponent : Component
    {
        // ReSharper disable InconsistentNaming - Internal state for GhostRoleSystem use only.
        [DataField("name"), Access(typeof(GhostRoleSystem), Other = AccessPermissions.None)]
        public string _InternalRoleName = "Unknown";

        [DataField("description"), Access(typeof(GhostRoleSystem), Other = AccessPermissions.None)]
        public string _InternalRoleDescription = "Unknown";

        [DataField("rules"), Access(typeof(GhostRoleSystem), Other = AccessPermissions.None)]
        public string _InternalRoleRules = "";

        [DataField("lotteryEnabled"), Access(typeof(GhostRoleSystem), Other = AccessPermissions.None)]
        public bool _InternalRoleLotteryEnabled = true;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Whether the <see cref="MakeSentientCommand"/> should run on the mob.
        /// </summary>
        [DataField("makeSentient"), ViewVariables(VVAccess.ReadWrite)]
        protected bool MakeSentient = true;

        /// <summary>
        ///     The probability that this ghost role will be available after init.
        ///     Used mostly for takeover roles that want some probability of being takeover, but not 100%.
        /// </summary>
        [DataField("prob")] public float Probability = 1f;

        public string RoleIdentifier => _InternalRoleName;

            // We do this so updating RoleName and RoleDescription in VV updates the open EUIs.

        [ViewVariables(VVAccess.ReadWrite), Access(typeof(GhostRoleSystem))]
        public string RoleName
        {
            get => Loc.GetString(_InternalRoleName);
            set => EntitySystem.Get<GhostRoleSystem>().SetRoleName(this, value);
        }

        [ViewVariables(VVAccess.ReadWrite), Access(typeof(GhostRoleSystem))]
        public string RoleDescription
        {
            get => Loc.GetString(_InternalRoleDescription);
            set => EntitySystem.Get<GhostRoleSystem>().SetRoleDescription(this, value);
        }

        [ViewVariables(VVAccess.ReadWrite), Access(typeof(GhostRoleSystem))]
        public string RoleRules
        {
            get => _InternalRoleRules;
            set => EntitySystem.Get<GhostRoleSystem>().SetRoleRules(this, value);
        }

        [ViewVariables(VVAccess.ReadWrite), Access(typeof(GhostRoleSystem))]
        public bool RoleLotteryEnabled
        {
            get => _InternalRoleLotteryEnabled;
            set => EntitySystem.Get<GhostRoleSystem>().SetRoleLotteryEnabled(this, value);
        }

        /// <summary>
        /// If the ghost role has been taken by a player.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), Access(typeof(GhostRoleSystem))]
        public bool Taken { get; set; }

        /// <summary>
        /// If the ghost role is disabled because the entity is in critical or dead.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), Access(typeof(GhostRoleSystem))]
        public bool Damaged { get; set; }

        /// <summary>
        /// If the ghost role is disabled due to being reserved by a <see cref="GhostRoleGroupComponent"/>
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), Access(typeof(GhostRoleSystem))]
        public bool RoleGroupReserved { get; set; }

        /// <summary>
        /// If the ghost role is available to be taken.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), Access(typeof(GhostRoleSystem))]
        public bool Available => !(Taken || Damaged || RoleGroupReserved || Deleted);

        /// <summary>
        /// Reregisters the ghost role when the current player ghosts.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("reregister")]
        public bool ReregisterOnGhost { get; set; } = true;

        public abstract bool Take(IPlayerSession session);

        /// <summary>
        /// Returns the number of remaining available takeovers.
        /// </summary>
        public abstract int AvailableTakeovers { get; }
    }
}
