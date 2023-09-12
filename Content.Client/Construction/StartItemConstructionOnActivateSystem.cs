using Content.Shared.Interaction;
using Content.Client.Construction;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction;

public sealed class StartItemConstructionOnActivateSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _constructionSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StartItemConstructionOnActivateComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, StartItemConstructionOnActivateComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        TryStartConstruction(uid, component);
        args.Handled = true;
    }

    public bool TryStartConstruction(EntityUid uid, StartItemConstructionOnActivateComponent component)
    {
        if (component.Prototype == null || _constructionSystem == null) return false;

        if ((_prototypeManager.Index<ConstructionPrototype>(component.Prototype)).Type == ConstructionType.Item)
        {
            _constructionSystem.TryStartItemConstruction(component.Prototype);
            return true;
        }

        return false;
    }
}
