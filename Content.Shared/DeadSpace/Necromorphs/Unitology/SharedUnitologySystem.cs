// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Content.Shared.Antag;

namespace Content.Shared.DeadSpace.Necromorphs.Unitology;

public abstract class SharedUnitologySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyEnslavedComponent, ComponentGetStateAttemptEvent>(OnUniCompGetStateAttempt);
        SubscribeLocalEvent<UnitologyHeadComponent, ComponentGetStateAttemptEvent>(OnUniCompGetStateAttempt);
        SubscribeLocalEvent<UnitologyComponent, ComponentGetStateAttemptEvent>(OnUniCompGetStateAttempt);
        SubscribeLocalEvent<UnitologyEnslavedComponent, ComponentStartup>(DirtyUniComps);
        SubscribeLocalEvent<UnitologyHeadComponent, ComponentStartup>(DirtyUniComps);
        SubscribeLocalEvent<UnitologyComponent, ComponentStartup>(DirtyUniComps);
    }

    /// <summary>
    /// Determines if a Enslaved component should be sent to the client.
    /// </summary>
    private void OnUniCompGetStateAttempt(EntityUid uid, UnitologyEnslavedComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// Determines if a Head uni component should be sent to the client.
    /// </summary>
    private void OnUniCompGetStateAttempt(EntityUid uid, UnitologyHeadComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// Determines if a Uni component should be sent to the client.
    /// </summary>
    private void OnUniCompGetStateAttempt(EntityUid uid, UnitologyComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// The criteria that determine whether a Uni/HeadUni/Enslaved component should be sent to a client.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not {} uid)
            return true;

        if (HasComp<UnitologyComponent>(uid) || HasComp<UnitologyEnslavedComponent>(uid) || HasComp<UnitologyHeadComponent>(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }
    /// <summary>
    /// Dirties all the Uni components so they are sent to clients.
    ///
    /// We need to do this because if a uni component was not earlier sent to a client and for example the client
    /// becomes a uni then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    /// </summary>
    private void DirtyUniComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var uniComps = AllEntityQuery<UnitologyComponent>();
        while (uniComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var enslavedComps = AllEntityQuery<UnitologyEnslavedComponent>();
        while (enslavedComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }

        var headUniComps = AllEntityQuery<UnitologyHeadComponent>();
        while (headUniComps.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }

}
