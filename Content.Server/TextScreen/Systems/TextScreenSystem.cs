using Content.Server.DeviceLinking;
using Content.Server.TextScreen.Components;
using Content.Server.TextScreen.Events;

using Content.Shared.TextScreen;

using Robust.Shared.Timing;


namespace Content.Server.TextScreen;

public sealed class TextScreenSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TextScreenComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<TextScreenComponent, TextScreenTimerEvent>(OnTimer);
        SubscribeLocalEvent<TextScreenComponent, TextScreenTextEvent>(OnText);
    }

    private void OnInit(EntityUid uid, TextScreenComponent component, ComponentInit args)
    {
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Label);
    }

    private void OnTimer(EntityUid uid, TextScreenComponent component, ref TextScreenTimerEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var activeTimer = EnsureComp<ActiveTextScreenTimerComponent>(uid);

        if (appearance != null)
        {
            activeTimer.Remaining = _gameTiming.CurTime + args.Duration;
            _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Timer, appearance);
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, activeTimer.Remaining, appearance);
        }
    }

    private void OnText(EntityUid uid, TextScreenComponent component, ref TextScreenTextEvent args)
    {
        RemComp<ActiveTextScreenTimerComponent>(uid);

        component.Label = args.Label[..Math.Min(5, args.Label.Length)];

        _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Text);
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Label);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ActiveTextScreenTimerComponent, TextScreenComponent>();
        while (query.MoveNext(out var uid, out var active, out var timer))
        {
            if (active.Remaining > _gameTiming.CurTime)
                continue;

            Finish(uid);

            if (timer.DoneSound == null)
                continue;
            _audio.PlayPvs(timer.DoneSound, uid);
        }
    }

    private void Finish(EntityUid uid)
    {
        RemComp<ActiveTextScreenTimerComponent>(uid);
        _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Text);
    }
}
