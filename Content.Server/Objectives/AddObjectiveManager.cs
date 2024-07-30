using System.Linq;
using Content.Shared.Objectives.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives;

public sealed class AddObjectiveManager : IPostInjectInit
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private IEnumerable<string>? _objectives;

    public void PostInject()
    {
        _prototypes.PrototypesReloaded += CreateCompletions;
    }

    public void Shutdown()
    {
        _prototypes.PrototypesReloaded -= CreateCompletions;
    }

    private void CreateCompletions(PrototypesReloadedEventArgs unused)
    {
        CreateCompletions();
    }

    public IEnumerable<string> Objectives()
    {
        if (_objectives == null)
            CreateCompletions();

        return _objectives!;
    }

    private void CreateCompletions()
    {
        _objectives = _prototypes.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.HasComponent<ObjectiveComponent>())
            .Select(p => p.ID)
            .Order();
    }
}
