using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.PackageWrapper.Components
{
    [RegisterComponent]
    public class WrapperTypeComponent : Component
    {
        public override string Name => "WrapType";

        [DataField("Wrap")]
        public string Wrap = "string.Empty";
    }
}
