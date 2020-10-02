using System.Threading.Tasks;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Construction
{
    public interface IStepCompleted : IExposeData
    {
        Task StepCompleted(IEntity entity);
    }
}
