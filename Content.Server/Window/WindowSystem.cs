using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Window;

public class WindowSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WindowComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, WindowComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.KnockDelay.TotalSeconds <= 0)
            return;

        if (_gameTiming.CurTime < component.LastKnockTime + component.KnockDelay)
            return;

        SoundSystem.Play(Filter.Pvs(args.Target), component.KnockSound.GetSound(),
            Transform(args.Target).Coordinates, AudioHelpers.WithVariation(0.05f));

        var msg = Loc.GetString("comp-window-knock");
        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));

        component.LastKnockTime = _gameTiming.CurTime;
        args.Handled = true;
    }
}
