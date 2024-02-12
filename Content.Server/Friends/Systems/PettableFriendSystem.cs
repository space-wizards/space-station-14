using Content.Server.Friends.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server.Friends.Systems;

public sealed class PettableFriendSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _factionException = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PettableFriendComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PettableFriendComponent, GotRehydratedEvent>(OnRehydrated);
    }

    private void OnUseInHand(EntityUid uid, PettableFriendComponent comp, UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !TryComp<FactionExceptionComponent>(uid, out var factionException))
            return;

        if (_factionException.IsIgnored(uid, user, factionException))
        {
            _popup.PopupEntity(Loc.GetString(comp.FailureString, ("target", uid)), user, user);
            return;
        }

        // you have made a new friend :)
        _popup.PopupEntity(Loc.GetString(comp.SuccessString, ("target", uid)), user, user);
        _factionException.IgnoreEntity(uid, user, factionException);
        args.Handled = true;
    }

    private void OnRehydrated(EntityUid uid, PettableFriendComponent _, ref GotRehydratedEvent args)
    {
        // can only pet before hydrating, after that the fish cannot be negotiated with
        if (!TryComp<FactionExceptionComponent>(uid, out var comp))
            return;

        var targetComp = AddComp<FactionExceptionComponent>(args.Target);
        _factionException.IgnoreEntities(args.Target, comp.Ignored, targetComp);
    }
}
