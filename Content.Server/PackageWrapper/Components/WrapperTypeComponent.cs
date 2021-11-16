using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.PackageWrapper.Components
{
    public class WrapperTypeComponent : Component
    {
        public override string Name => "WrapType";

        [DataField("WrapType")]
        public string WrapType = string.Empty;
    }
}
