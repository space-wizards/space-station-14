using Content.Server.OuterRim.Worldgen.Systems.Overworld;

namespace Content.Server.OuterRim.Worldgen.Components;

[RegisterComponent]
public sealed class WorldManagedComponent : Component
{
    public Vector2i CurrentChunk;
    public DebrisData DebrisData = default!;
}
