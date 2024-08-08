using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, ComponentProtoName("Computer")]
    public sealed partial class ComputerComponent : Component
    {
        [DataField("board")]
        public EntProtoId? BoardPrototype;
    }
}
