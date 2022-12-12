using Content.Shared.Pinpointer;

namespace Content.Client.Pinpointer;

[RegisterComponent]
public sealed class NavMapComponent : SharedNavMapComponent
{
    [ViewVariables]
    public readonly Dictionary<Vector2i, List<Vector2[]>> Chunks = new();
}
