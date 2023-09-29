using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Cuffs;

public sealed class CuffableSystem : SharedCuffableSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffableComponent, ComponentShutdown>(OnCuffableShutdown);
        SubscribeLocalEvent<CuffableComponent, ComponentHandleState>(OnCuffableHandleState);
    }

    private void OnCuffableShutdown(EntityUid uid, CuffableComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, false);
    }

    private void OnCuffableHandleState(EntityUid uid, CuffableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CuffableComponentState cuffState)
            return;

        component.CanStillInteract = cuffState.CanStillInteract;
        _actionBlocker.UpdateCanMove(uid);

        var ev = new CuffedStateChangeEvent();
        RaiseLocalEvent(uid, ref ev);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        var cuffed = cuffState.NumHandsCuffed > 0;
        sprite.LayerSetVisible(HumanoidVisualLayers.Handcuffs, cuffed);

        // if they are not cuffed, that means that we didn't get a valid color,
        // iconstate, or RSI. that also means we don't need to update the sprites.
        if (!cuffed)
            return;
        sprite.LayerSetColor(HumanoidVisualLayers.Handcuffs, cuffState.Color!.Value);

        if (!Equals(component.CurrentRSI, cuffState.RSI) && cuffState.RSI != null) // we don't want to keep loading the same RSI
        {
            component.CurrentRSI = cuffState.RSI;
            sprite.LayerSetState(HumanoidVisualLayers.Handcuffs, cuffState.IconState, component.CurrentRSI);
        }
        else
        {
            sprite.LayerSetState(HumanoidVisualLayers.Handcuffs, cuffState.IconState);
        }
    }
}

