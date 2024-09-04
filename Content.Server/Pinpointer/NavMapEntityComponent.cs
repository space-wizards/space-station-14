using Content.Shared.Atmos;
using Content.Shared.Pinpointer;

namespace Content.Server.Pinpointer;

/// <summary>
/// Used to mark entities that interact with the nav map or its regions on the client UI
/// </summary>
[RegisterComponent]
[Access(typeof(NavMapSystem))]
public sealed partial class NavMapEntityComponent : Component
{
    /// <summary>
    /// Determines what chunk type this entitiy should associated with and
    /// how the drawing of the entity will be handled by the nav map (if applicable).
    /// </summary>
    [DataField]
    public NavMapChunkType Category { get; private set; } = NavMapChunkType.Invalid;

    /// <summary>
    /// Determines what edges of the tile the entity initially covers.
    /// Must match the default facing of the entity.
    /// </summary>
    [DataField]
    public AtmosDirection InitBlockedDirection { get; private set; } = AtmosDirection.All;
}
