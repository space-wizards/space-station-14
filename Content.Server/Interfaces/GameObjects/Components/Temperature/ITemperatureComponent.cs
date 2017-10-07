using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    public interface ITemperatureComponent : IComponent
    {
        float CurrentTemperature { get; }
    }
}
