using Robust.Shared.Prototypes;

namespace Content.Server._Craft.StationGoals.Scipts;

[ImplicitDataDefinitionForInheritors]
public interface IStationGoalScript
{
    void PerformAction(StationGoalPrototype stationGoal, IPrototypeManager prototypeManager, EntitySystem entitySystem);

    void Cleanup();
}
