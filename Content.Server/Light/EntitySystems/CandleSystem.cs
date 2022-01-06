using Content.Server.Light.Components;
using Content.Server.Ignitable;
using Content.Shared.Item;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Light;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.EntitySystems;
public class CandleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CandleComponent, AfterInteractEvent>(OnAfterInteractEvent);
        SubscribeLocalEvent<CandleComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<CandleComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Check each candle's time left vs icon and set the appearance data if not already set
        var ents = EntityManager.EntityQuery<CandleComponent, IgnitableComponent>();
        foreach (var ent in ents)
        {
            //If IsFirstLight is true then this candle has never been lit/initalized and we can't trust DurationLeft
            if (ent.Item2.IsFirstLight)
                continue;

            SmokableState ignitableState = ent.Item2.CurrentState;
            float candlePercent = ent.Item2.DurationLeft / ent.Item2.Duration;
            CandleState currentCandleIcon = ent.Item1.CurrentCandleIcon;

            //Only care about actually lit candles because their sprite can't shouldn't while not lit
            if (ignitableState == SmokableState.Lit)
            {
                if (candlePercent <= 0.5 && candlePercent > 0.33 && currentCandleIcon != CandleState.Half)
                {
                    ent.Item1.CurrentCandleIcon = CandleState.BrandNew;
                    SetState(ent.Item1, ignitableState);
                    continue;
                }

                if (candlePercent <= 0.33 && candlePercent > 0 && currentCandleIcon != CandleState.AlmostOut)
                {
                    ent.Item1.CurrentCandleIcon = CandleState.AlmostOut;
                    SetState(ent.Item1, ignitableState);
                    continue;
                }

            }

            //Since we randomize
            if (ent.Item2.DurationLeft <= 0 && currentCandleIcon != CandleState.Dead)
            {
                ent.Item1.CurrentCandleIcon = CandleState.Dead;
                SetState(ent.Item1, ignitableState);
            }
        }
    }

    private void OnAfterInteractEvent(EntityUid uid, CandleComponent component, AfterInteractEvent args)
    {
        //Check our current Ignitable state and Set Candle state based on that
        if (TryComp<IgnitableComponent>(uid, out var ignitableComponent))
        {
            SetState(component, ignitableComponent.CurrentState);
        }

    }


    private void OnUseInHandEvent(EntityUid uid, CandleComponent component, UseInHandEvent args)
    {
        if (component.CurrentState == SmokableState.Lit)
        {
            SetState(component, SmokableState.Unlit);
            args.Handled = true;
        }
    }


    private void OnInteractUsing(EntityUid uid, CandleComponent component, InteractUsingEvent args)
    {
        //Check our current Ignitable state and Set Candle state based on that
        if (TryComp<IgnitableComponent>(uid, out var ignitableComponent))
        {
            SetState(component, ignitableComponent.CurrentState);
        }
    }


    private void SetState(CandleComponent component, SmokableState currentSmokableState)
    {
        component.CurrentState = currentSmokableState;

        if (component.PointLightComponent != null)
        {
            component.PointLightComponent.Enabled = component.CurrentState == SmokableState.Lit;
        }

        if (EntityManager.TryGetComponent(component.Owner, out SharedItemComponent? item))
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
            appearance.SetData(CandleVisuals.Behaviour, CandleStateToBehaviorID(component));
        }
    }


    //These behaviour ID's control the LightBehavior animation that is being played.
    //Used to increase flicker/dimming as the candle gets more worn out
    private string CandleStateToBehaviorID(CandleComponent component)
    {
        switch (component.CurrentCandleIcon)
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

