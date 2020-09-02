using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class FireExtinguisherComponent : Component
    {
        public override string Name => "FireExtinguisher";
    }
}
