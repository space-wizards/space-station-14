using Content.Shared.Actions;
using Content.Server.DeadSpace.Components.NightVision;
using Content.Shared.DeadSpace.NightVision;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.NightVision;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<NightVisionComponent, ComponentGetState>(OnNightVisionGetState);
        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionActionEvent>(OnToggleNightVision);
    }

    private void OnNightVisionGetState(EntityUid uid, NightVisionComponent component, ref ComponentGetState args)
    {
        args.State = new NightVisionComponentState(component.Color, component.IsNightVision, _timing.CurTick.Value, component.ActivateSound);
    }

    private void OnComponentStartup(EntityUid uid, NightVisionComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionToggleNightVisionEntity, component.ActionToggleNightVision);
    }

    private void OnComponentRemove(EntityUid uid, NightVisionComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ActionToggleNightVisionEntity);
    }

    private void OnToggleNightVision(EntityUid uid, NightVisionComponent component, ToggleNightVisionActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleNightVision(uid, component);
    }

    private void ToggleNightVision(EntityUid uid, NightVisionComponent component)
    {
        component.IsNightVision = !component.IsNightVision;

        Dirty(uid, component);
    }

}
