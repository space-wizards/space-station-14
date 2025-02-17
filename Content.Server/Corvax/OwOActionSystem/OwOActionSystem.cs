using Content.Server.Speech.Components;
using Content.Shared.Actions;
using Content.Shared.ADT.OwOAccent;
using Content.Shared.Mobs.Components;

namespace Content.Server.Corvax.OwOAction;

public sealed class OwOActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent, OwOAccentActionEvent>(OnOwOAction);
        SubscribeLocalEvent<OwOActionComponent, OwOAccentActionEvent>(OnChange);
        SubscribeLocalEvent<OwOActionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OwOActionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, OwOActionComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.OwOActionEntity, component.OwOAction);
    }

    private void OnChange(EntityUid uid, OwOActionComponent component, OwOAccentActionEvent args)
    {
        component.IsON = !component.IsON;
    }

    private void OnShutdown(EntityUid uid, OwOActionComponent component, ComponentShutdown args)
    {
        if (component.OwOActionEntity != null)
            _actionsSystem.RemoveAction(uid, component.OwOActionEntity);
    }
    private void OnOwOAction(EntityUid uid, MobStateComponent component, OwOAccentActionEvent ev)
    {
        if (ev.Handled)
            return;

        var enabled = EntityManager.HasComponent<OwOAccentComponent>(uid);

        if (enabled)
            EntityManager.RemoveComponent<OwOAccentComponent>(uid);
        else
            EntityManager.AddComponent<OwOAccentComponent>(uid);

        ev.Handled = true;
    }
}
