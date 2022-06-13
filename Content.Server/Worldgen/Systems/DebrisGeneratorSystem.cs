using Content.Server.Worldgen.Prototypes;
using Robust.Shared.Map;

namespace Content.Server.Worldgen.Systems;

public sealed class DebrisGeneratorSystem : EntitySystem
{

}

/// <summary>
/// Broadcast ref event fired when
/// </summary>
[ByRefEventg]
public ref struct GenerateDebrisEvent
{
    public readonly MapCoordinates Coordinates;
    public readonly DebrisPrototype Prototype;
    public EntityUid? GeneratedDebris;

    public GenerateDebrisEvent(MapCoordinates coordinates, DebrisPrototype prototype)
    {
        Coordinates = coordinates;
        Prototype = prototype;
    }
}
