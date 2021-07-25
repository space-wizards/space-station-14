using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Radiation
{
    [RequiresExplicitImplementation]
    public interface IRadiationAct : IComponent
    {
        void RadiationAct(float frameTime, SharedRadiationPulseComponent radiation);
    }
}
