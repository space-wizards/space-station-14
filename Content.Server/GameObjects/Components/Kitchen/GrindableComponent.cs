using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Kitchen
{

    /// <summary>
    /// Tag component that denotes an entity as Grindable by the reagentgrinder.
    /// </summary>
    [RegisterComponent]

    public class GrindableComponent : Component
    {
        public override string Name => "Grindable";
    }
}
