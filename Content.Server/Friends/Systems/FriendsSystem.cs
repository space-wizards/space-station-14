using Content.Server.Chemistry.Components;
using Content.Server.Friends.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server.Friends.Systems;

public sealed class FriendsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PettableFriendComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<FriendsComponent, GotRehydratedEvent>(OnRehydrated);
    }

    private void OnUseInHand(EntityUid uid, PettableFriendComponent comp, UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !TryComp<FriendsComponent>(uid, out var friends))
            return;

        if (IsFriends(friends, user))
        {
            _popup.PopupEntity(Loc.GetString(comp.FailureString, ("target", uid)), user, user);
            return;
        }

        // you have made a new friend :)
        _popup.PopupEntity(Loc.GetString(comp.SuccessString, ("target", uid)), user, user);
        friends.Friends.Add(user);
        args.Handled = true;
    }

    private void OnRehydrated(EntityUid uid, FriendsComponent comp, ref GotRehydratedEvent args)
    {
        // can only pet before hydrating, after that the fish cannot be negotiated with
        AddComp<FriendsComponent>(args.Target).Friends = comp.Friends;
    }

    /// <summary>
    /// Returns whether the entity is friends with the target or not.
    /// </summary>
    public bool IsFriends(FriendsComponent comp, EntityUid target)
    {
        return comp.Friends.Contains(target);
    }
}
