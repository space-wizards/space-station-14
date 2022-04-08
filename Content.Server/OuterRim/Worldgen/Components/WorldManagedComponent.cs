using Content.Server.OuterRim.Worldgen.Systems.Overworld;

namespace Content.Server.OuterRim.Worldgen.Components;

[RegisterComponent]
public class WorldManagedComponent : Component
{
    public override string Name => "WorldManaged";

    public Vector2i CurrentChunk;
    public DebrisData DebrisData = default!;
}
