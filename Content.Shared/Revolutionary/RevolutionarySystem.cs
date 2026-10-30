using Content.Shared.Revolutionary.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;

namespace Content.Shared.Revolutionary;

public sealed partial class RevolutionarySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, ComponentStartup>(DirtyRevComps);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentStartup>(DirtyRevComps);
        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentStartup>(DirtyRevComps);
    }

    /// <summary>
    /// Determines if a HeadRev component should be sent to the client.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRevCompGetStateAttempt(EntityUid uid, HeadRevolutionaryComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// Determines if a Rev component should be sent to the client.
    /// </summary>
    [SubscribeLocalEvent]
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
        if (player?.AttachedEntity is not {} uid)
            return true;

        if (HasComp<RevolutionaryComponent>(uid) || HasComp<HeadRevolutionaryComponent>(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }

    /// <summary>
    /// Dirties all the Rev components so they are sent to clients.
    ///
    /// We need to do this because if a rev component was not earlier sent to a client and for example the client
    /// becomes a rev then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    /// </summary>
    private void DirtyRevComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
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

    [SubscribeLocalEvent]
    private void OnAttemptConvert(Entity<RevolutionaryComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttemptConvertImmune(Entity<RevolutionaryImmuneComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void GetRevIcon(Entity<RevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<HeadRevolutionaryComponent>(ent))
            return;

        if (ProtoMan.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    [SubscribeLocalEvent]
    private void GetHeadRevIcon(Entity<HeadRevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
