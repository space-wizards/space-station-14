using Content.Shared.SharedAirlockPainter;

namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAirlockPainterComponent))]
    public sealed class AirlockPainterComponent : SharedAirlockPainterComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("index")]
        public int Index = 0;
    }
}
