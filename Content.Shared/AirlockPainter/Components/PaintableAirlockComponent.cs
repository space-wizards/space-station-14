namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    public sealed class PaintableAirlockComponent : Component
    {
        [DataField("group")]
        public string Group = default!;
    }
}
