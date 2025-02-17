// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Server.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;
using Content.Shared.StatusEffect;
using Content.Shared.Speech.Muting;
using Content.Shared.Examine;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class StunSlaveSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunSlaveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StunSlaveComponent, ComponentShutdown>(OnShutDown);
        SubscribeLocalEvent<StunSlaveComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, StunSlaveComponent component, ExaminedEvent args)
    {
        var time = _timing.CurTime - component.TimeUtil;
        double seconds = Math.Abs(time.TotalSeconds);
        int roundedSeconds = (int)Math.Round(seconds);

        if (HasComp<UnitologyComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString($"Парализованность спадёт через [color=red]{roundedSeconds} секунд[/color]."));
        }
        else
        {
            if (args.Examiner == args.Examined)
            {
                args.PushMarkup(Loc.GetString($"Парализованность спадёт через [color=red]{roundedSeconds} секунд[/color]."));
            }
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Heal
        var query = EntityQueryEnumerator<StunSlaveComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (curTime > component.TimeUtil)
            {
                RemComp<StunnedComponent>(uid);
            }
        }
    }
    private void OnComponentInit(EntityUid uid, StunSlaveComponent component, ComponentInit args)
    {
        _statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", TimeSpan.FromSeconds(component.Duration), true);

        _popup.PopupEntity(Loc.GetString("Вас парализовали."), uid, uid);

        if (!HasComp<MutedComponent>(uid))
            AddComp<MutedComponent>(uid);

        component.TimeUtil = _timing.CurTime + TimeSpan.FromSeconds(component.Duration);
    }

    private void OnShutDown(EntityUid uid, StunSlaveComponent component, ComponentShutdown args)
    {
        _statusEffect.TryRemoveStatusEffect(uid, "Stun");

        _popup.PopupEntity(Loc.GetString("Вы снова можете двигаться."), uid, uid);

        if (HasComp<MutedComponent>(uid))
            RemComp<MutedComponent>(uid);

    }
}
