using Content.Shared.Actions;
using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, GetItemActionsEvent>(GetImplantAction);
    }

    private void GetImplantAction(EntityUid uid, SubdermalImplantComponent component, GetItemActionsEvent args)
    {
        //TODO: Determine if this can work since you need to add it on implant, rather than on pickup
        //TODO: Something like implant > check this component > add action instead of from here
    }
}
