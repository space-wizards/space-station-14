using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Part.Property
{
    public interface IBodyPartProperty : IComponent
    {
        bool Active { get; set; }
    }
}
