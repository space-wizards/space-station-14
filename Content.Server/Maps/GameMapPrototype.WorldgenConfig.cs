namespace Content.Server.Maps;

public sealed partial class GameMapPrototype
{
    [DataField("worldgenConfig", required: true)]
    public string WorldgenConfig = default!;
}
