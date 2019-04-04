using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects.Components.Movement
{
    // Does nothing except ensure uniqueness between mover components.
    // There can only be one.
    public interface IMoverComponent : IComponent
    {

    }
}
