using Robust.Shared.Player;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Items.Cards;
using Content.Shared.DeadSpace.Items.Cards.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.Components;

namespace Content.Server.DeadSpace.Items.Cards;

public sealed class CardSystem : SharedCardSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardComponent, GetVerbsEvent<Verb>>(OnSetTransferVerbs);
        SubscribeLocalEvent<CardComponent, UseInHandEvent>(OnAfterInteract);
    }
    private void OnSetTransferVerbs(EntityUid uid, CardComponent component, GetVerbsEvent<Verb> args)
    {

        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (HasComp<HandsComponent>(args.User))
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("Перевернуть карту"),
                ClientExclusive = true,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                Act = () => TurnOver(uid, component),
                Impact = LogImpact.Medium
            });
        }
    }
    private void OnAfterInteract(EntityUid uid, CardComponent component, UseInHandEvent args)
    {
        TurnOver(uid, component);
    }
}
