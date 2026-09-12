using Content.Shared.DeviceLinking.Components;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class SignalSwitchSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private LockSystem _lock = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SignalSwitchComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.OnPort, ent.Comp.OffPort, ent.Comp.StatusPort);
    }

    [SubscribeLocalEvent]
    private void OnActivated(Entity<SignalSwitchComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (_lock.IsLocked(ent.Owner))
            return;

        ent.Comp.State = !ent.Comp.State;
        _deviceLink.InvokePort(ent.Owner, ent.Comp.State ? ent.Comp.OnPort : ent.Comp.OffPort);

        // only send status if it's a toggle switch and not a button
        if (ent.Comp.OnPort != ent.Comp.OffPort)
        {
            _deviceLink.SendSignal(ent.Owner, ent.Comp.StatusPort, ent.Comp.State);
        }

        var audioParams = ent.Comp.ClickSound?.Params ?? AudioParams.Default;
        audioParams = audioParams.WithVariation(0.125f).AddVolume(8f);
        _audio.PlayPredicted(ent.Comp.ClickSound, ent.Owner, args.User, audioParams);

        args.Handled = true;
    }
}
