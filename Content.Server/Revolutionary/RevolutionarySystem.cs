using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Content.Shared.Antag;
using Content.Shared.Mind.Components;

namespace Content.Server.Revolutionary;

public sealed class RevolutionarySystem : SharedRevolutionarySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // session specific networking
        SubscribeLocalEvent<RevolutionaryComponent, ComponentGetStateAttemptEvent>(OnRevCompGetStateAttempt);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentGetStateAttemptEvent>(OnRevCompGetStateAttempt);

        SubscribeLocalEvent<RevolutionaryComponent, ComponentStartup>((_, _, _) => DirtyRevComps());
        SubscribeLocalEvent<RevolutionaryComponent, MindAddedMessage>((_, _, _) => DirtyRevComps());
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentStartup>((_, _, _) => DirtyRevComps());
        SubscribeLocalEvent<HeadRevolutionaryComponent, MindAddedMessage>((_, _, _) => DirtyRevComps());
    }

    /// <summary>
    /// Determines if a HeadRev component should be sent to the client.
    /// </summary>
    private void OnRevCompGetStateAttempt(EntityUid uid, HeadRevolutionaryComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// Determines if a Rev component should be sent to the client.
    /// </summary>
    private void OnRevCompGetStateAttempt(EntityUid uid, RevolutionaryComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// The criteria that determine whether a Rev/HeadRev component should be sent to a client.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not { } uid)
            return true;

        if (HasComp<RevolutionaryComponent>(uid)
            || HasComp<HeadRevolutionaryComponent>(uid)
            || HasComp<ShowAntagIconsComponent>(uid))
            return true;

        return false;
    }

    /// <summary>
    /// Dirties all the Rev components so they are sent to clients.
    ///
    /// We need to do this because if a rev component was not earlier sent to a client and for example the client
    /// becomes a rev then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    ///
    /// TODO: Make the session specific networking API sane, this is way to much boilerplate.
    /// </summary>
    public void DirtyRevComps()
    {
        var revComps = AllEntityQuery<RevolutionaryComponent>();
        while (revComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var headRevComps = AllEntityQuery<HeadRevolutionaryComponent>();
        while (headRevComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }
}
