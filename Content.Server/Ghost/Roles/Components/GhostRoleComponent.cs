using Content.Server.Mind.Commands;
using Robust.Server.Player;

namespace Content.Server.Ghost.Roles.Components
{
    [Access(typeof(GhostRoleSystem))]
    public abstract class GhostRoleComponent : Component
    {
        [DataField("name")] public string _roleName = "Unknown";

        [DataField("description")] private string _roleDescription = "Unknown";

        [DataField("rules")] private string _roleRules = "";

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
            get => _roleRules;
            set
            {
                _roleRules = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        [DataField("whitelistRequired")]
        public bool WhitelistRequired = true;

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

        public abstract bool Take(IPlayerSession session);
    }
}
