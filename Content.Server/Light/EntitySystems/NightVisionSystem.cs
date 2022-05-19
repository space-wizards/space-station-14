using Content.Shared.Actions;
using Content.Shared.Light.Component;
using Content.Shared.Light.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Light.EntitySystems;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentGetState>(OnState);
        SubscribeLocalEvent<NightVisionComponent, NightVisionToggleEvent>(OnToggle);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(EntityUid uid, NightVisionComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, component.Action, null);
    }

    private void OnState(EntityUid uid, NightVisionComponent component, ref ComponentGetState args)
    {
        args.State = new NightVisionComponentState(component.IsEnabled);
    }

    private void OnToggle(EntityUid uid, NightVisionComponent component, NightVisionToggleEvent args)
    {
        component.IsEnabled = !component.IsEnabled;
        _actions.SetToggled(component.Action, component.IsEnabled);
        Dirty(component);
    }

    private void OnRemove(EntityUid uid, NightVisionComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.Action);
    }
}
