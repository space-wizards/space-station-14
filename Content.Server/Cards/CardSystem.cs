using Content.Shared.Cards;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Cards;

/// <inheritdoc />
[UsedImplicitly]
public sealed partial class CardSystem : SharedCardSystem
{
    // Server-side index counter. Needed so that all cards have unique ids. Basically a EntityUid but for cards.
    private int _indexCounter = 0;

    protected override void OnCardsInit(Entity<CardsComponent> ent, ref ComponentInit args)
    {
        base.OnCardsInit(ent, ref args);
        for (var i = 0; i < ent.Comp.Cards.Count; i++)
        {
            var card = ent.Comp.Cards[i];
            if (card.CardIndex != 0)
                continue;
            card.CardIndex = _indexCounter;
            _indexCounter++;
            ent.Comp.Cards[i] = card;
        }
        Dirty(ent.Owner, ent.Comp);
    }

    public override EntityUid? SplitDeck(Entity<CardsComponent> ent, EntityCoordinates spawnPosition, List<int> cardIndexes = default!)
    {
        if (cardIndexes.Count != GetCardFromIndex(ent.Comp.Cards, cardIndexes).Count)
            return null;
        if (!ProtoMan.Resolve(ent.Comp.CardStackType, out var cardStack))
            return null;

        var split = SpawnAtPosition(cardStack.Spawn, spawnPosition);

        if (!TryComp<CardsComponent>(split, out var splitComp))
        {
            QueueDel(split);
            return null;
        }

        MoveCards((split, splitComp), ent, cardIndexes);
        splitComp.Flipped = ent.Comp.Flipped;
        splitComp.Fanned = ent.Comp.Fanned;

        UpdateVisualState(ent);
        UpdateVisualState((split, splitComp));

        Dirty(ent.Owner, ent.Comp);
        Dirty(split, splitComp);

        return split;
    }
}
