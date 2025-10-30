using Content.Shared.DeadSpace.Ports.Jukebox;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.DeadSpace.Ports.Jukebox;

public sealed class TapeCreatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TapeCreatorComponent, ComponentHandleState>(OnStateChanged);
        SubscribeLocalEvent<TapeComponent, ComponentHandleState>(OnTapeStateChanged);
    }

    private void OnTapeStateChanged(EntityUid uid, TapeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TapeComponentState state) return;

        component.Songs = state.Songs;
    }

    private void OnStateChanged(EntityUid uid, TapeCreatorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TapeCreatorComponentState state) return;

        component.Recording = state.Recording;
        component.CoinBalance = state.CoinBalance;
        component.InsertedTape = state.InsertedTape;

        SetTapeLayerVisible(component, state.InsertedTape.HasValue);
    }

    private void SetTapeLayerVisible(TapeCreatorComponent component, bool visible)
    {
        var spriteComponent = Comp<SpriteComponent>(component.Owner);
        spriteComponent.LayerMapTryGet("tape", out var layer);
        spriteComponent.LayerSetVisible(layer, visible);
    }
}
