namespace Content.Shared.Procedural.Dungeons;

public sealed class NoiseDunGen : IDungeonGenerator
{
    [DataField("bounds")]
    public Box2i Bounds = new Box2i(0, 0, 70, 70);
}
