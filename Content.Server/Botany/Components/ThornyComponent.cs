
using Robust.Shared.Containers;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public sealed partial class ThornyComponent : Component
    {
        [DataField]
        public string Sound = string.Empty;

        [DataField]
        public int ThrowStrength = 3;
    }
}
