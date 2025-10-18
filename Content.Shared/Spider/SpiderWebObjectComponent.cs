using Robust.Shared.GameStates;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderWebObjectComponent : Component
{
}
