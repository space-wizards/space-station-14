
namespace Content.Shared.GPS.Components
{
    [RegisterComponent]
    public sealed partial class HandheldGPSComponent : Component
    {
        [DataField]
        public float UpdateRate = 0.2f;
    }
}
