namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    public sealed class PaintableAirlockComponent : Component
    {
        [DataField("group")]
        public AirlockGroup Group = default!;
    }


    public enum AirlockGroup
    {
        Standard,
        Glass,
    }

    public enum AirlockStyle
    {
        Basic,
        Cargo,
        Command,
        Engineering,
        External,
        Firelock,
        Freezer,
        Maintenance,
        Medical,
        Science,
        Security,
        Shuttle,
    }

}
