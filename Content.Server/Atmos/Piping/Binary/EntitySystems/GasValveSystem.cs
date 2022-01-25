using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos.Piping;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasValveSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasValveComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GasValveComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<GasValveComponent, ExaminedEvent>(OnExamined);
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
            if (args.User.InRangeUnobstructed(args.Target) && _actionBlockerSystem.CanInteract(args.User))
            {
                Toggle(uid, component);
                SoundSystem.Play(Filter.Pvs(component.Owner), component.ValveSound.GetSound(), component.Owner, AudioHelpers.WithVariation(0.25f));
            }
        }

        public void Set(EntityUid uid, GasValveComponent component, bool value)
        {
            component.Open = value;
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
                }
                else
                {
                    inlet.RemoveAlwaysReachable(outlet);
                    outlet.RemoveAlwaysReachable(inlet);
                }
            }
        }

        public void Toggle(EntityUid uid, GasValveComponent component)
        {
            Set(uid, component, !component.Open);
        }
    }
}
