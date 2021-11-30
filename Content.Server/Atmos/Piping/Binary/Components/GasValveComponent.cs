using Content.Shared.Interaction;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public class GasValveComponent : Component
    {
        public override string Name => "GasValve";

        [ViewVariables]
        [DataField("open")]
        public bool Open { get; set; } = true;

        [DataField("pipe")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string PipeName { get; } = "pipe";

        [DataField("valveSound")]
        public SoundSpecifier _valveSound { get; } = new SoundCollectionSpecifier("valveSqueak");
    }
}
