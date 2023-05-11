using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Light.Component;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Light;

public abstract class SharedToggleLightSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleLightComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnGetActions(EntityUid uid, ToggleLightComponent component, GetItemActionsEvent args)
    {
        if (component.ToggleAction == null
            && _proto.TryIndex(component.ToggleActionId, out InstantActionPrototype? act))
        {
            component.ToggleAction = new InstantAction(act);
        }

        if (component.ToggleAction != null)
            args.Actions.Add(component.ToggleAction);
    }
}
