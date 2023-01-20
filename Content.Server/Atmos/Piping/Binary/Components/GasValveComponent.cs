using Robust.Shared.Audio;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed class GasValveComponent : Component
    {
        [DataField("open")]
        public bool Open { get; set; } = true;

        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [DataField("valveSound")]
        public SoundSpecifier ValveSound { get; } = new SoundCollectionSpecifier("valveSqueak");
    }
}
