#nullable enable
using Content.Server.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class GlassBeakerComponent : Component
    {
        public override string Name => "GlassBeaker";
        [ComponentDependency] protected readonly DrinkComponent? DrinkComponent = default!;

        public override void Initialize()
        {
            base.Initialize();
            if (DrinkComponent != null)
            {
                DrinkComponent.Opened = true;
            }
        }
    }
}
