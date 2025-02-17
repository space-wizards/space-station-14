using Robust.Shared.Player;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared.Storage.EntitySystems;
using System.Linq;
using Content.Shared.Storage.Components;
using Content.Shared.DeadSpace.Items.Cards.Components;
using Content.Shared.Hands.Components;

namespace Content.Server.DeadSpace.Items.Cards;

public sealed class CardBinSystem : EntitySystem
{

    [Dependency] private readonly BinSystem _bin = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardBinComponent, GetVerbsEvent<Verb>>(OnSetTransferVerbs);
    }

    private void OnSetTransferVerbs(EntityUid uid, CardBinComponent component, GetVerbsEvent<Verb> args)
    {

        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (HasComp<HandsComponent>(args.User))
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Перемешать карты"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => Mix(uid),
                Impact = LogImpact.Medium
            });
        }
    }

    private void Mix(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent(uid, out BinComponent? bin))
            return;

        var items = bin.ItemContainer.ContainedEntities.ToList();

        if (items != null)
        {
            var random = new Random();
            int n = items.Count;

            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                var value = items[k];
                items[k] = items[n];
                items[n] = value;
            }

            _bin.RefreshContainer(items, bin);

        }
    }
}
