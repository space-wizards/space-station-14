using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Client.Light.EntitySystems;

public sealed class MatchstickSystem : SharedMatchstickSystem
{
    // Noop on client
    protected override void CreateMatchstickHotspot(Entity<MatchstickComponent> ent) {}
}
