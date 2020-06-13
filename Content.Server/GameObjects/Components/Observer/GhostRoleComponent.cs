using Content.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Observer
{
    public abstract class GhostRoleComponent : Component
    {
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public bool Taken { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => RoleName, "name", "Unknown");
            serializer.DataField(this, x => RoleDescription, "description", "Unknown");
        }

        public override void Initialize()
        {
            base.Initialize();

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
