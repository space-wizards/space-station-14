using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Items;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Ignitable;
using Content.Shared.Temperature;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Ignitable
{
    public class IgnitableSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private HashSet<IgnitableComponent> _litItems = new();
        [Dependency]
        private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IgnitableComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<IgnitableComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<IgnitableComponent, AfterInteractEvent>(OnAfterInteractEvent);
            SubscribeLocalEvent<IgnitableComponent, UseInHandEvent>(OnUseInHandEvent);
            SubscribeLocalEvent<IgnitableComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnShutdown(EntityUid uid, IgnitableComponent component, ComponentShutdown args)
        {
            _litItems.Remove(component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var litIgnitable in _litItems)
            {
                if (litIgnitable.CurrentState != SmokableState.Lit || Paused(litIgnitable.Owner) || litIgnitable.Deleted)
                    continue;

                litIgnitable.DurationLeft -= frameTime;

                if (litIgnitable.DurationLeft <= 0)
                {
                    //Call Extinguish?
                    Extinguish(litIgnitable);
                    _litItems.Remove(litIgnitable);
                    continue;
                }

                _atmosphereSystem.HotspotExpose(EntityManager.GetComponent<TransformComponent>(litIgnitable.Owner).Coordinates, litIgnitable.Temperature, 50, true);
            }
        }

        private void OnAfterInteractEvent(EntityUid uid, IgnitableComponent component, AfterInteractEvent args)
        {
            if (component.CurrentState != SmokableState.Unlit)
                return;

            var targetEntity = args.Target;
            if (targetEntity == null)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent);

            if (!isHotEvent.IsHot)
                return;

            Ignite(component);
        }

        private void OnUseInHandEvent(EntityUid uid, IgnitableComponent component, UseInHandEvent args)
        {
            if (component.CurrentState == SmokableState.Lit)
                Extinguish(component);
        }

        private void OnInteractUsing(EntityUid uid, IgnitableComponent component, InteractUsingEvent args)
        {
            if (args.Handled || component.CurrentState != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            Ignite(component);
        }

        private void Extinguish(IgnitableComponent component)
        {
            if(!component.CanRelight || component.DurationLeft <= 0)
            {
                SetState(component, SmokableState.Burnt);
                return;
            }

            SetState(component, SmokableState.Unlit);
        }

        private void OnIsHotEvent(EntityUid uid, IgnitableComponent component, IsHotEvent args)
        {
            args.IsHot = component.CurrentState == SmokableState.Lit;
        }

        public void Ignite(IgnitableComponent component)
        {
            if (component.IsFirstLight)
            {
                IgnitableInit(component);
            }

            //If ShouldPlaySound is true then we've specified an ignition sound to play
            if(component.ShouldPlaySound && component.IgniteSound.GetSound() != string.Empty)
            {
                SoundSystem.Play(
                    Filter.Pvs(component.Owner), component.IgniteSound.GetSound(), component.Owner,
                    AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));
            }


            //If this component has been set to 'Burnt' instead of Unlit then it is not possible to relight this component
            if (component.CurrentState != SmokableState.Burnt)
            {
                SetState(component, SmokableState.Lit);
                _litItems.Add(component);
            }
        }

        private void SetState(IgnitableComponent component, SmokableState smokableState)
        {
            component.CurrentState = smokableState;

            if (component.PointLightComponent != null)
            {
                component.PointLightComponent.Enabled = component.CurrentState == SmokableState.Lit;
            }

            if (EntityManager.TryGetComponent(component.Owner, out ItemComponent? item))
            {
                switch (component.CurrentState)
                {
                    case SmokableState.Lit:
                        item.EquippedPrefix = "lit";
                        break;
                    default:
                        item.EquippedPrefix = "unlit";
                        break;
                }
            }

            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                //Set Ignitable AND Smoking Visuals as some ignitable things might have visualizers that make use of SmokingVisuals instead of IgnitableVisuals (Cigs)
                appearance.SetData(IgnitableVisuals.SmokableState, component.CurrentState);
                appearance.SetData(SmokingVisuals.Smoking, component.CurrentState);
            }

        }

        private void IgnitableInit(IgnitableComponent component)
        {
            if(component.ShouldRandomize)
            {
                float numSecondsToChangeBy = _random.Next(-component.RandomizeMaxTime, component.RandomizeMaxTime);
                component.Duration += numSecondsToChangeBy;
            }

            component.DurationLeft = component.Duration;
            component.IsFirstLight = false;
        }

    }
}

