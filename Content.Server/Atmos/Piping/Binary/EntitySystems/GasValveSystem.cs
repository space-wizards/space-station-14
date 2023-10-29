using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Atmos.Piping;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Nodes;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasValveSystem : EntitySystem
    {
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasValveComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GasValveComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<GasValveComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(Entity<GasValveComponent> ent, ref ExaminedEvent args)
        {
            var valve = ent.Comp;
            if (!Comp<TransformComponent>(ent).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
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
            _audioSystem.PlayPvs(component.ValveSound, uid, AudioParams.Default.WithVariation(0.25f));
        }

        public void Set(EntityUid uid, GasValveComponent component, bool value)
        {
            component.Open = value;
            if (!TryComp<PolyNodeComponent>(uid, out var poly) ||
                !_nodeSystem.TryGetNode((uid, poly), component.InletName, out var inlet) ||
                !_nodeSystem.TryGetNode((uid, poly), component.OutletName, out var outlet)
            )
                return;

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, FilterVisuals.Enabled, component.Open, appearance);

            if (component.Open)
            {
                _nodeSystem.TrySetEdge(inlet, outlet, EdgeFlags.None, inlet, outlet);
                _ambientSoundSystem.SetAmbience(uid, true);
            }
            else
            {
                _nodeSystem.TrySetEdge(inlet, outlet, EdgeFlags.NoMerge, inlet, outlet);
                _ambientSoundSystem.SetAmbience(uid, false);
            }
        }

        public void Toggle(EntityUid uid, GasValveComponent component)
        {
            Set(uid, component, !component.Open);
        }
    }
}
