using Content.Shared._Starlight.Railroading;
using Content.Shared.Objectives;

namespace Content.Server._Starlight.Objectives.Events;

[ByRefEvent]
public record struct CollectObjectivesEvent(Dictionary<string, List<ObjectiveInfo>> Groups)
{ }
[ByRefEvent]
public record struct CollectObjectiveInfoEvent(List<ObjectiveInfo> Objectives)
{ }