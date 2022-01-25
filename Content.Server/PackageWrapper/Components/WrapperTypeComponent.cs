using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.PackageWrapper.Components
{
    [RegisterComponent]
    public class WrappableComponent : Component
    {
        public override string Name => "Wrappable";

        [DataField("WrapType")]
        public string WrapType = string.Empty;
    }
}
