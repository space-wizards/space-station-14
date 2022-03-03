using Content.Server.Mind.Commands;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;

namespace Content.Server.Ghost.Roles.Components
{
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

        // We do this so updating RoleName and RoleDescription in VV updates the open EUIs.

        [ViewVariables(VVAccess.ReadWrite)]
        public string RoleName
        {
            get => _roleName;
            set
            {
                _roleName = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string RoleDescription
        {
            get => _roleDescription;
            set
            {
                _roleDescription = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string RoleRules
        {
            get => _roleRules;
            set
            {
                _roleRules = value;
                EntitySystem.Get<GhostRoleSystem>().UpdateAllEui();
            }
        }

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

        protected override void Initialize()
        {
            base.Initialize();
            if (_roleRules == "")
                _roleRules = Loc.GetString("ghost-role-component-default-rules");
            EntitySystem.Get<GhostRoleSystem>().RegisterGhostRole(this);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            EntitySystem.Get<GhostRoleSystem>().UnregisterGhostRole(this);
        }

        public abstract bool Take(IPlayerSession session);
    }
}
