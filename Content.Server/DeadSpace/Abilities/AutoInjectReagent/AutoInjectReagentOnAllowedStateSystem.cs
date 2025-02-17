using Content.Shared.DeadSpace.Abilities.AutoInjectReagent.Components;
using Content.Server.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.DeadSpace.Abilities.AutoInjectReagent;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Abilities.AutoInjectReagentOnAllowedState;

public sealed partial class AutoInjectReagentOnAllowedStateSystem : SharedReagentSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoInjectReagentOnAllowedStateComponent, MobStateChangedEvent>(OnState);
        SubscribeLocalEvent<AutoInjectReagentOnAllowedStateComponent, ComponentInit>(OnMapInit);
        SubscribeLocalEvent<AutoInjectReagentOnAllowedStateComponent, EntityUnpausedEvent>(OnRegenUnpause);
    }

    private void OnMapInit(EntityUid uid, AutoInjectReagentOnAllowedStateComponent component, ComponentInit args)
    {
        component.TimeUntilRegen = TimeSpan.FromSeconds(component.DurationRegenReagents) + _timing.CurTime;
    }

    private void OnRegenUnpause(EntityUid uid, AutoInjectReagentOnAllowedStateComponent component, ref EntityUnpausedEvent args)
    {
        component.TimeUntilRegen += args.PausedTime;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var autoInjectReagentOnAllowedStateQuery = EntityQueryEnumerator<AutoInjectReagentOnAllowedStateComponent>();
        while (autoInjectReagentOnAllowedStateQuery.MoveNext(out var comp))
        {
            if (comp.IsReady)
                continue;

            if (curTime > comp.TimeUntilRegen)
            {
                comp.IsReady = true;
            }
        }
    }

    private void OnState(EntityUid uid, AutoInjectReagentOnAllowedStateComponent component, MobStateChangedEvent args)
    {

        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return;

        if (!component.IsReady)
            return;

        foreach (var allowedState in component.AllowedStates)
        {
            if (allowedState == mobState.CurrentState)
            {
                Inject(component.Reagents, uid);
                _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), uid, uid);
                _audio.PlayPvs(component.InjectSound, uid);
            }
        }

        component.TimeUntilRegen = TimeSpan.FromSeconds(component.DurationRegenReagents) + _timing.CurTime;
        component.IsReady = false;
    }
}
