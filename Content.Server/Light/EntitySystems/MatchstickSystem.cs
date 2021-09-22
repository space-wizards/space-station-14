using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Items;
using Content.Server.Light.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    public class MatchstickSystem : EntitySystem
    {
        private HashSet<MatchstickComponent> _litMatches = new();
        [Dependency]
        private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<MatchstickComponent, IsHotEvent>(OnIsHotEvent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var match in _litMatches)
            {
                if (match.CurrentState != SharedBurningStates.Lit)
                    continue;

                _atmosphereSystem.HotspotExpose(match.Owner.Transform.Coordinates, 400, 50, true);
            }
        }

        private void OnInteractUsing(EntityUid uid, MatchstickComponent component, InteractUsingEvent args)
        {
            if (args.Handled || component.CurrentState != SharedBurningStates.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used.Uid, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            Ignite(component, args.User);
            args.Handled = true;
        }

        private void OnIsHotEvent(EntityUid uid, MatchstickComponent component, IsHotEvent args)
        {
            args.IsHot = component.CurrentState == SharedBurningStates.Lit;
        }

        public void Ignite(MatchstickComponent component, IEntity user)
        {
            // Play Sound
            SoundSystem.Play(
                Filter.Pvs(component.Owner), component.IgniteSound.GetSound(), component.Owner,
                AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));

            // Change state
            SetState(component, SharedBurningStates.Lit);
            _litMatches.Add(component);
            component.Owner.SpawnTimer(component.Duration * 1000, delegate
            {
                SetState(component, SharedBurningStates.Burnt);
                _litMatches.Remove(component);
            });
        }

        private void SetState(MatchstickComponent component, SharedBurningStates value)
        {
            component.CurrentState = value;

            if (component.PointLightComponent != null)
            {
                component.PointLightComponent.Enabled = component.CurrentState == SharedBurningStates.Lit;
            }

            if (component.Owner.TryGetComponent(out ItemComponent? item))
            {
                switch (component.CurrentState)
                {
                    case SharedBurningStates.Lit:
                        item.EquippedPrefix = "lit";
                        break;
                    default:
                        item.EquippedPrefix = "unlit";
                        break;
                }
            }

            if (component.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(SmokingVisuals.Smoking, component.CurrentState);
            }
        }
    }
}
