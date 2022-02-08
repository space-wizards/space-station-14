using Content.Server.Body.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(BrainSystem))]
    public class BrainComponent : Component
    {
    }
}
