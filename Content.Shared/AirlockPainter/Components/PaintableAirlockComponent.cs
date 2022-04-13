namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    public sealed class PaintableAirlockComponent : Component
    {
        [DataField("group", customTypeSerializer:typeof(PrototypeIdSerializer<AirlockGroupPrototype>))]
        public string Group = default!;
    }
}
