using Content.Shared.Matchstick.Components;
using Content.Shared.Matchstick.EntitySystems;

namespace Content.Client.Matchstick.EntitySystems;

public sealed class MatchstickSystem : SharedMatchstickSystem
{
    // Noop on client
    protected override void CreateMatchstickHotspot(Entity<MatchstickComponent> ent) {}
}
