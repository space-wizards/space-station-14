using Content.Shared.Cards;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Cards;

/// <inheritdoc />
[UsedImplicitly]
public sealed partial class CardSystem : SharedCardSystem
{
    private int _inxCounter = 0;

    protected override void OnCardsInit(Entity<CardsComponent> ent, ref ComponentInit args)
    {
        base.OnCardsInit(ent, ref args);
        for (var i = 0; i < ent.Comp.Cards.Count; i++)
        {
            var card = ent.Comp.Cards[i];
            card.CardInx = _inxCounter;
            _inxCounter++;
            ent.Comp.Cards[i] = card;
        }
        Dirty(ent.Owner, ent.Comp);
    }
}
