
namespace Content.Shared.GPS
{
    public abstract partial class SharedHandheldGPSComponent : Component
    {
        [DataField("updateRate")]
        public float UpdateRate = 1.5f;
    }
}
