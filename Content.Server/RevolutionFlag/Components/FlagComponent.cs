namespace Content.Server.RevolutionFlag.Components
{
    [RegisterComponent, Access(typeof(FlagSystem))]
    public sealed class FlagComponent : Component
    {
        [DataField("accumulator")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float accumulator { get; set; } = 1.0f;

        [DataField("range")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float range { get; set; } = 2.5f;

        [DataField("timespan")]
        public TimeSpan timespan = new TimeSpan(0,0,5);

        [ViewVariables(VVAccess.ReadOnly)]
        public bool active = false;
    }
}