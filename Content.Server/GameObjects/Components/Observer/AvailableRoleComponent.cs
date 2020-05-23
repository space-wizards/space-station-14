using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Observer
{
    public class AvailableRoleComponent : Component
    {
        public override string Name => "AvailableRole";

        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public bool Taken { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => RoleName, "name", "Unknown");
            serializer.DataField(this, x => RoleDescription, "description", "Unknown");
        }
    }
}
