using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;

namespace Content.Server.Ghost.Roles.Components
{
    public abstract class GhostRoleComponent : Component
    {
        [DataField("name")] private string _roleName = "Unknown";

        [DataField("description")] private string _roleDescription = "Unknown";

        [DataField("rules")] private string _roleRules = "";

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
        public bool Taken { get; protected set; }

        [ViewVariables]
        public uint Identifier { get; set; }

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
