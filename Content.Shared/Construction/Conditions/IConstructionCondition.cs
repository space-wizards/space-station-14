using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    public interface IConstructionCondition
    {
        ConstructionGuideEntry? GenerateGuideEntry();
        bool Condition(EntityUid user, EntityCoordinates location, Direction direction);
    }
}
