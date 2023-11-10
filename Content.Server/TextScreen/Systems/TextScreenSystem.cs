// using Content.Server.TextScreen.Components;
using Content.Shared.TextScreen.Components;
using Content.Shared.TextScreen.Events;

using Content.Shared.TextScreen;

using Robust.Shared.Timing;


namespace Content.Server.TextScreen;

/// <summary>
/// Base system for rendering text and timers on screens without networking or a BUI.
/// </summary>
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

    /// <summary>
    /// Enables component.Label to be displayed at roundstart without a <see cref="TextScreenTextEvent"/>.
    /// </summary>
    private void OnInit(EntityUid uid, TextScreenComponent component, ComponentInit args)
    {
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Label);
    }

    /// <summary>
    /// Overrides the screen display with a <see cref="TextScreenTimerEvent"/>.
    /// </summary>
    private void OnTimer(EntityUid uid, TextScreenComponent component, ref TextScreenTimerEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (appearance != null)
        {
            // component.Remaining = _gameTiming.CurTime + args.Duration;
            // _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Timer, appearance);
            // _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, _gameTiming.CurTime + args.Duration[1].Value, appearance);
            var timer = EnsureComp<TextScreenTimerComponent>(uid);
            timer.Target = _gameTiming.CurTime + args.Duration;
        }
    }

    /// <summary>
    /// Overrides the screen display with a <see cref="TextScreenTextEvent"/>.
    /// </summary>
    private void OnText(EntityUid uid, TextScreenComponent component, ref TextScreenTextEvent args)
    {
        // component.Remaining = null;
        // component.Label = args.Label[..Math.Min(5, args.Label.Length)].ToArray();

        // // _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Text);
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, args.Label);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TextScreenTimerComponent>();
        while (query.MoveNext(out var uid, out var timer))
        {
            if (timer.Target > _gameTiming.CurTime)
                continue;

            RemComp<TextScreenTimerComponent>(uid);

            if (!TryComp<TextScreenComponent>(uid, out var screen))
                continue;

            if (screen.DoneSound != null)
                _audio.PlayPvs(screen.DoneSound, uid);
        }
    }
}
