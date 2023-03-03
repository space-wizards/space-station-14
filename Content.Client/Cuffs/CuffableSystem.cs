using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Client.Cuffs;

public sealed class CuffableSystem : SharedCuffableSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CuffableComponent, ComponentHandleState>(OnCuffableHandleState);
        SubscribeLocalEvent<HandcuffComponent, ComponentHandleState>(OnHandcuffHandleState);
    }

    private void OnShutdown(EntityUid uid, CuffableComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
    }

    private void OnHandcuffHandleState(EntityUid uid, HandcuffComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HandcuffComponentState state)
            return;

        if (state.IconState == string.Empty)
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            // TODO: safety check to see if RSI contains the state?
            sprite.LayerSetState(0, new RSI.StateId(state.IconState));
        }
    }

    private void OnCuffableHandleState(EntityUid uid, CuffableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CuffableComponentState cuffState)
        {
            return;
        }

        component.CanStillInteract = cuffState.CanStillInteract;
        _actionBlocker.UpdateCanMove(uid);

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, cuffState.NumHandsCuffed > 0);
            sprite.LayerSetColor(HumanoidVisualLayers.Handcuffs, cuffState.Color);

            if (cuffState.NumHandsCuffed > 0)
            {
                if (component.CurrentRSI != cuffState.RSI) // we don't want to keep loading the same RSI
                {
                    component.CurrentRSI = cuffState.RSI;

                    if (component.CurrentRSI != null)
                    {
                        sprite.LayerSetState(HumanoidVisualLayers.Handcuffs, new RSI.StateId(cuffState.IconState), new ResourcePath(component.CurrentRSI));
                    }
                }
                else
                {
                    // TODO: safety check to see if RSI contains the state?
                    sprite.LayerSetState(HumanoidVisualLayers.Handcuffs, new RSI.StateId(cuffState.IconState));
                }
            }
        }

        var ev = new CuffedStateChangeEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}

