using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Construction
{
    public interface IConstructionCondition : IExposeData
    {
        bool Condition(IEntity user, EntityCoordinates location, Direction direction);
    }
}
