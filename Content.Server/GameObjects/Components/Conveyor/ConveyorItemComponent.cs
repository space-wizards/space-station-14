using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Conveyor
{
    /// <summary>
    /// Dummy component for construction graph
    /// </summary>
    [RegisterComponent]
    public class ConveyorItemComponent : Component
    {
        public override string Name => "ConveyorItem";
    }
}
