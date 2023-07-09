using Content.Server.DeviceLinking.Components;
using Content.Shared.Audio;
using Content.Shared.DeviceLinking;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.DeviceLinking.Systems
{
    public sealed class SignalSwitchSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DeviceLinkSystem _signal = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalSwitchComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalSwitchComponent, ActivateInWorldEvent>(OnActivated);
        }

        private void OnInit(EntityUid uid, SignalSwitchComponent component, ComponentInit args)
        {
            _signal.EnsureSourcePorts(uid, component.OnPort, component.OffPort);
            _appearance.SetData(uid, SignalSwitchVisuals.State, component.State);
        }

        private void OnActivated(EntityUid uid, SignalSwitchComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            component.State = !component.State;

            _appearance.SetData(uid, SignalSwitchVisuals.State, component.State);
            _signal.InvokePort(uid, component.State ? component.OnPort : component.OffPort);
            _audio.PlayPvs(component.ClickSound, uid);

            args.Handled = true;
        }
    }
}
