using Content.Shared.GameObjects.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    [RequiresExplicitImplementation]
    public interface IRadiationAct : IComponent
    {
        void RadiationAct(float frameTime, SharedRadiationPulseComponent radiation);
    }
}
