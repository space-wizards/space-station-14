using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignalSwitchSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
        if (args.Handled)
            return;

        comp.State = !comp.State;
        _deviceLink.InvokePort(uid, comp.State ? comp.OnPort : comp.OffPort);

        // only send status if it's a toggle switch and not a button
        if (comp.OnPort != comp.OffPort)
        {
            _deviceLink.SendSignal(uid, comp.StatusPort, comp.State);
        }

        _audio.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));

        args.Handled = true;
    }
}
