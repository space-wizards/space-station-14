using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Movement
{
    public interface IClimbable { };

    public class SharedClimbableComponent : Component, IClimbable
    {
        public sealed override string Name => "Climbable";
    }
}
