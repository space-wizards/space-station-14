using Content.Server.Body.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(BrainSystem))]
    public class BrainComponent : Component
    {
        public override string Name { get; } = "Brain";
    }
}
