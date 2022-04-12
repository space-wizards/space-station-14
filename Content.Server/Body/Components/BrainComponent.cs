using Content.Server.Body.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(BrainSystem))]
    public sealed class BrainComponent : Component
    {
    }
}
