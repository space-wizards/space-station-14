using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MarkerLocationSpawnRule))]
public sealed partial class MarkerSpawnLocationComponent : Component
{
    [DataField(required: true)]
    public List<string> MarkerStrings = new();
}
