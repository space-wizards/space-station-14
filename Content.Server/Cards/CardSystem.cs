using Content.Shared.Cards;
using JetBrains.Annotations;

namespace Content.Server.Cards;

/// <inheritdoc />
[UsedImplicitly]
public sealed partial class CardSystem : SharedCardSystem
{
    // Server-side index counter. Needed so that all cards have unique ids. Basically a EntityUid but for cards.
    private int _inxCounter = 0;

    protected override void OnCardsInit(Entity<CardsComponent> ent, ref ComponentInit args)
    {
        base.OnCardsInit(ent, ref args);
        for (var i = 0; i < ent.Comp.Cards.Count; i++)
        {
            var card = ent.Comp.Cards[i];
            if (card.CardIndex != 0)
                continue;
            card.CardIndex = _inxCounter;
            _inxCounter++;
            ent.Comp.Cards[i] = card;
        }
        Dirty(ent.Owner, ent.Comp);
    }
}
