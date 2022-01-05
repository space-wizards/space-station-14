using Content.Server.Procedural.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Procedural.Components;

[RegisterComponent]
public class WorldManagedComponent : Component
{
    public override string Name => "WorldManaged";

    public Vector2i CurrentChunk;
    public DebrisData DebrisData = default!;
}
