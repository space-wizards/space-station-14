namespace Content.Server.Xray.Components
{
    [RegisterComponent]
    public sealed class XRayGlassesComponent : Component
    {
        [DataField("activationSlot")]
        public string ActivationSlot = "eyes";

    }
}
