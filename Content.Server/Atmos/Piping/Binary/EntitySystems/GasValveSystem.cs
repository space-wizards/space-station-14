using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasValveSystem : EntitySystem
    {
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasValveComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasValveComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GasValveComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<GasValveComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<GasValveComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnInit(EntityUid uid, GasValveComponent component, ComponentInit args)
        {
            _signalSystem.EnsureReceiverPorts(uid, component.OnPort, component.OffPort, component.TogglePort);
        }

        private void OnExamined(EntityUid uid, GasValveComponent valve, ExaminedEvent args)
        {
            if (!Comp<TransformComponent>(valve.Owner).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
                return;

            if (Loc.TryGetString("gas-valve-system-examined", out var str,
                        ("statusColor", valve.Open ? "green" : "orange"),
                        ("open", valve.Open)
            ))
                args.PushMarkup(str);
        }

        private void OnStartup(EntityUid uid, GasValveComponent component, ComponentStartup args)
        {
            // We call set in startup so it sets the appearance, node state, etc.
            Set(uid, component, component.Open);
        }

        private void OnActivate(EntityUid uid, GasValveComponent component, ActivateInWorldEvent args)
        {
            Toggle(uid, component);
        }

        private void OnSignalReceived(EntityUid uid, GasValveComponent component, SignalReceivedEvent args)
        {
            if (args.Port == component.OffPort)
                Set(uid, component, false);
            else if (args.Port == component.OnPort)
                Set(uid, component, true);
            else if (args.Port == component.TogglePort)
                Toggle(uid, component);
        }

        public void Set(EntityUid uid, GasValveComponent component, bool value)
        {
            if (component.Open != value)
            {
                SoundSystem.Play(component.ValveSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioHelpers.WithVariation(0.25f));
                component.Open = value;
            }
            if (TryComp(uid, out NodeContainerComponent? nodeContainer)
                && nodeContainer.TryGetNode(component.InletName, out PipeNode? inlet)
                && nodeContainer.TryGetNode(component.OutletName, out PipeNode? outlet))
            {
                if (TryComp<AppearanceComponent>(component.Owner,out var appearance))
                {
                    appearance.SetData(FilterVisuals.Enabled, component.Open);
                }
                if (component.Open)
                {
                    inlet.AddAlwaysReachable(outlet);
                    outlet.AddAlwaysReachable(inlet);
                    _ambientSoundSystem.SetAmbience(component.Owner, true);
                }
                else
                {
                    inlet.RemoveAlwaysReachable(outlet);
                    outlet.RemoveAlwaysReachable(inlet);
                    _ambientSoundSystem.SetAmbience(component.Owner, false);
                }
            }
        }

        public void Toggle(EntityUid uid, GasValveComponent component)
        {
            Set(uid, component, !component.Open);
        }
    }
}
