using Content.Shared.DeviceLinking.Components;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class SignalSwitchSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalSwitchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalSwitchComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnInit(EntityUid uid, SignalSwitchComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, comp.OnPort, comp.OffPort, comp.StatusPort);
    }

    private void OnActivated(EntityUid uid, SignalSwitchComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (_lock.IsLocked(uid))
            return;

        comp.State = !comp.State;
        _deviceLink.InvokePort(uid, comp.State ? comp.OnPort : comp.OffPort);

        // only send status if it's a toggle switch and not a button
        if (comp.OnPort != comp.OffPort)
        {
            _deviceLink.SendSignal(uid, comp.StatusPort, comp.State);
            _appearance.SetData(uid, SwitchVisuals.Visuals, comp.State);
        }

        var audioParams = comp.ClickSound?.Params ?? AudioParams.Default;
        audioParams = audioParams.WithVariation(0.125f).AddVolume(8f);
        _audio.PlayPvs(comp.ClickSound, uid, audioParams);

        args.Handled = true;
    }
}
