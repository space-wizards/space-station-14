// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DeadSpace.Demons.Herald.Components;
using Content.Shared.DeadSpace.Demons.Herald;
using Content.Shared.DeadSpace.Demons.Herald.EntitySystems;

namespace Content.Server.DeadSpace.Demons.Herald;

public sealed class HeraldAbilitiesSystem : SharedHeraldSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeraldComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HeraldComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HeraldComponent, HeraldEnrageActionEvent>(DoEnrage);
    }

    private void OnComponentInit(EntityUid uid, HeraldComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionHeraldEnrageEntity, component.ActionHeraldEnrage, uid);
    }

    private void OnShutdown(EntityUid uid, HeraldComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionHeraldEnrageEntity);
    }

    private void DoEnrage(EntityUid uid, HeraldComponent component, HeraldEnrageActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        args.Handled = true;
        StartEnrage(uid, component);
    }
}
