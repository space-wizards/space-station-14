using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public interface IConstructionCondition : IExposeData
    {
        bool Condition(IEntity user, EntityCoordinates location, Direction direction);
    }
}
