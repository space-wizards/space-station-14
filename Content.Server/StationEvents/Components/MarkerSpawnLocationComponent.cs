using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Makes the owner entity an eligible spawner location for <see cref="MarkerLocationSpawnRule"/>.
/// </summary>
[RegisterComponent, Access(typeof(MarkerLocationSpawnRule))]
public sealed partial class MarkerSpawnLocationComponent : Component
{
    [DataField(required: true)]
    public List<string> MarkerStrings = new();
}
