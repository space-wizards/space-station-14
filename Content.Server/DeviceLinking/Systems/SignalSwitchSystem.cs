using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceLinking;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignalSwitchSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SignalSwitchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalSwitchComponent, ActivateInWorldEvent>(OnActivated);

        base.Initialize();
    }

    private void OnInit(EntityUid uid, SignalSwitchComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, comp.OnPort, comp.OffPort, comp.StatusPort);
        _appearance.SetData(uid, SignalSwitchVisuals.State, comp.State);
    }

    private void OnActivated(EntityUid uid, SignalSwitchComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        comp.State = !comp.State;
        _appearance.SetData(uid, SignalSwitchVisuals.State, comp.State);
        _deviceLink.InvokePort(uid, comp.State ? comp.OnPort : comp.OffPort);
        _audio.PlayPvs(comp.ClickSound, uid);

        // Invoke status port
        var data = new NetworkPayload
        {
            [DeviceNetworkConstants.LogicState] = comp.State ? SignalState.High : SignalState.Low
        };

        // only send status if it's a toggle switch and not a button
        if (comp.OnPort != comp.OffPort)
        {
            _deviceLink.InvokePort(uid, comp.StatusPort, data);
        }
    }
}
