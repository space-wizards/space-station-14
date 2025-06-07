using System.Linq;
using Content.Shared._EstacaoPirata.Cards.Card;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._EstacaoPirata.Cards.Card;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly CardSpriteSystem _cardSpriteSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, ComponentStartup>(OnComponentStartupEvent);
        SubscribeNetworkEvent<CardFlipUpdatedEvent>(OnFlip);
    }

    private void OnComponentStartupEvent(EntityUid uid, CardComponent comp, ComponentStartup args)
    {
        if (!TryComp(uid, out SpriteComponent? spriteComponent))
            return;

        for (var i = 0; i < spriteComponent.AllLayers.Count(); i++)
        {
            //Log.Debug($"Layer {i}");
            if (!spriteComponent.TryGetLayer(i, out var layer) || layer.State.Name == null)
                continue;

            var rsi = layer.RSI ?? spriteComponent.BaseRSI;
            if (rsi == null)
                continue;

            //Log.Debug("FOI");
            comp.FrontSprite.Add(new SpriteSpecifier.Rsi(rsi.Path, layer.State.Name));
        }

        comp.BackSprite ??= comp.FrontSprite;
        DirtyEntity(uid);
        UpdateSprite(uid, comp);
    }

    private void OnFlip(CardFlipUpdatedEvent args)
    {
        if (!TryComp(GetEntity(args.Card), out CardComponent? comp))
            return;
        UpdateSprite(GetEntity(args.Card), comp);
    }

    private void UpdateSprite(EntityUid uid, CardComponent comp)
    {
        var newSprite = comp.Flipped ? comp.BackSprite : comp.FrontSprite;
        //if (newSprite == null)
        //    return;

        if (!TryComp(uid, out SpriteComponent? spriteComponent))
            return;
        var layerCount = newSprite.Count();

        //inserts Missing Layers
        if (spriteComponent.AllLayers.Count() < layerCount)
        {
            for (var i = spriteComponent.AllLayers.Count(); i < layerCount; i++)
            {
                spriteComponent.AddBlankLayer(i);
            }
        }
        //Removes extra layers
        else if (spriteComponent.AllLayers.Count() > layerCount)
        {
            for (var i = spriteComponent.AllLayers.Count() - 1; i >= layerCount; i--)
            {
                spriteComponent.RemoveLayer(i);
            }
        }

        for (var i = 0; i < newSprite.Count(); i++)
        {
            var layer = newSprite[i];
            spriteComponent.LayerSetSprite(i, layer);
        }
    }
}
