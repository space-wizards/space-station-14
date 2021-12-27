using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Items;
using Content.Server.Light.Components;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Light;
using Content.Shared.Temperature;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Light.EntitySystems
{
    public class CandleSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private HashSet<CandleComponent> _litCandles = new();
        [Dependency]
        private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CandleComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CandleComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<CandleComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<CandleComponent, AfterInteractEvent>(OnAfterInteractEvent);
            SubscribeLocalEvent<CandleComponent, UseInHandEvent>(OnUseInHandEvent);
        }

        private void OnShutdown(EntityUid uid, CandleComponent component, ComponentShutdown args)
        {
            _litCandles.Remove(component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var candle in _litCandles)
            {
                if (candle.CurrentState != SmokableState.Lit || Paused(candle.Owner) || candle.Deleted)
                    continue;

                candle.WaxLeft -= frameTime;

                //There has to be a cleaner way to handle this
                float candlePercent = candle.WaxLeft / candle.WaxTotal;
                if (candlePercent <= 0.5 && candlePercent > 0.33 && candle.CurrentCandleIcon != CandleState.Half)
                {
                    SetState(candle, CandleState.Half, candle.CurrentState);
                }
                else if(candlePercent <= 0.33 && candlePercent > 0 && candle.CurrentCandleIcon != CandleState.AlmostOut)
                {
                    SetState(candle, CandleState.AlmostOut, candle.CurrentState);
                }

                if (candle.WaxLeft <= 0)
                {
                    SetState(candle, CandleState.Dead, SmokableState.Burnt);
                    _litCandles.Remove(candle);
                    continue;
                }

                _atmosphereSystem.HotspotExpose(EntityManager.GetComponent<TransformComponent>(candle.Owner).Coordinates, 400, 50, true);
            }
        }

        private void OnInteractUsing(EntityUid uid, CandleComponent component, InteractUsingEvent args)
        {
            if (args.Handled || component.CurrentState != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            Ignite(component, args.User);
            args.Handled = true;
        }

        private void OnAfterInteractEvent(EntityUid uid, CandleComponent component, AfterInteractEvent args)
        {
            var targetEntity = args.Target;
            if (targetEntity == null)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(targetEntity.Value, isHotEvent);

            if (!isHotEvent.IsHot)
                return;

            Ignite(component, args.User);
            args.Handled = true;
        }

        private void OnUseInHandEvent(EntityUid uid, CandleComponent component, UseInHandEvent args)
        {
            if(component.CurrentState == SmokableState.Lit)
            SetState(component, component.CurrentCandleIcon, SmokableState.Unlit);
            args.Handled = true;
        }

        private void OnIsHotEvent(EntityUid uid, CandleComponent component, IsHotEvent args)
        {
            args.IsHot = component.CurrentState == SmokableState.Lit;
        }

        public void Ignite(CandleComponent component, EntityUid user)
        {
            if(component.IsFirstLight)
            {
                CandleInit(component);
            }
            SetState(component, component.CurrentCandleIcon, SmokableState.Lit);
            _litCandles.Add(component);
        }

        private void SetState(CandleComponent component, CandleState value, SmokableState smokableState)
        {
            component.CurrentCandleIcon = value;
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
                appearance.SetData(CandleVisuals.CandleState, component.CurrentCandleIcon);
                appearance.SetData(CandleVisuals.SmokableState, component.CurrentState);
                appearance.SetData(CandleVisuals.Behaviour, CandleStateToBehaviorID(component));
            }
        }


        private void CandleInit(CandleComponent component)
        {
            float numSecondsToChangeBy = _random.Next(-120, 120);
            component.WaxLeft += numSecondsToChangeBy;
            component.WaxTotal = component.WaxLeft;
            component.IsFirstLight = false;
        }

        private string CandleStateToBehaviorID(CandleComponent component)
        {
            switch(component.CurrentCandleIcon)
            {
                case CandleState.BrandNew:
                    return component.BrandNewBehaviourID;
                case CandleState.Half:
                    return component.HalfNewBehaviorID;
                default:
                    return component.AlmostOutBehaviourID;
            }
        }









    }
}

