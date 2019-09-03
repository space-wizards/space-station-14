using System.Security.Cryptography;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    public class IdCardComponent : Component
    {
        public override string Name => "IdCard";
        [ViewVariables(VVAccess.ReadWrite)]
        private string _fullName;
        [ViewVariables(VVAccess.ReadWrite)]
        private string _jobTitle;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fullName, "fullName", string.Empty);
            serializer.DataField(ref _jobTitle, "jobTitle", string.Empty);
        }
    }
}
