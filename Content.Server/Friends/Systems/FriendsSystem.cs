using Content.Server.Chemistry.Components;
using Content.Server.Friends.Components;
using Content.Shared.Interaction.Events;

namespace Content.Server.Friends.Systems;

public sealed class FriendsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FriendsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<FriendsComponent, GotRehydratedEvent>(OnRehydrated);
    }

    private void OnUseInHand(EntityUid uid, FriendsComponent comp, UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !comp.Pettable || IsFriends(comp, user))
            return;

        // you have made a new friend :)
        comp.Friends.Add(user);
        args.Handled = true;
    }

    protected void OnRehydrated(EntityUid uid, FriendsComponent comp, ref GotRehydratedEvent args)
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
