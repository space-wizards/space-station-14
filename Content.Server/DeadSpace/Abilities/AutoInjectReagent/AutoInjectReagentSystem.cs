// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.AutoInjectReagent.Components;
using Content.Server.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Actions;
using Content.Shared.DeadSpace.Abilities.AutoInjectReagent;

namespace Content.Server.DeadSpace.Abilities.AutoInjectReagent;

public sealed partial class AutoInjectReagentSystem : SharedReagentSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoInjectReagentComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AutoInjectReagentComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AutoInjectReagentComponent, AutoInjectReagentActionEvent>(OnInject);
    }

    private void OnComponentInit(EntityUid uid, AutoInjectReagentComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.AutoInjectReagentActionEntity, component.AutoInjectReagentAction);
    }

    private void OnShutdown(EntityUid uid, AutoInjectReagentComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.AutoInjectReagentActionEntity);
    }

    private void OnInject(EntityUid uid, AutoInjectReagentComponent component, AutoInjectReagentActionEvent args)
    {

        if (args.Handled)
            return;

        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return;

        if (mobState.CurrentState != MobState.Dead)
        {
            Inject(component.Reagents, uid);
            _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), uid, uid);
            _audio.PlayPvs(component.InjectSound, uid);
        }

        args.Handled = true;
    }
}
