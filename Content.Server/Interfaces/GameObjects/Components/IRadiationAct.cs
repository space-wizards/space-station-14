using Content.Server.GameObjects.Components.StationEvents;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects.Components
{
    public interface IRadiationAct : IComponent
    {
        void RadiationAct(float frameTime, RadiationPulseComponent radiation);
    }
}
