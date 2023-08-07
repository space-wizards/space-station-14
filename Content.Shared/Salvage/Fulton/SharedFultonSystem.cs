using System.Numerics;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Salvage.Fulton;

public abstract class SharedFultonSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly SharedTransformSystem _transform = default!;

    public static readonly TimeSpan FultonDuration = TimeSpan.FromSeconds(30);

    /*
     * TODO: Support stacks
     * do_after
     * Audio on being applied
     * Implement beacon
     * Beacon needs anchoring or something
     * Verb to remove fulton
     * Examine indicates how long until fulton.
     */

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FultonedComponent, EntityUnpausedEvent>(OnFultonUnpaused);

        SubscribeLocalEvent<FultonComponent, AfterInteractEvent>(OnFultonInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<FultonedComponent>();
        var curTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextFulton > curTime)
                continue;

            Fulton(uid, comp);
        }
    }

    private void Fulton(EntityUid uid, FultonedComponent component)
    {
        if (!Deleted(component.Beacon))
        {
            _transform.SetCoordinates(uid, new EntityCoordinates(component.Beacon, Vector2.Zero));
        }

        RemCompDeferred<FultonedComponent>(uid);
    }

    private void OnFultonUnpaused(EntityUid uid, FultonedComponent component, ref EntityUnpausedEvent args)
    {
        component.NextFulton += args.PausedTime;
    }

    private void OnFultonInteract(EntityUid uid, FultonComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !Timing.IsFirstTimePredicted)
            return;

        if (TryComp<FultonBeaconComponent>(args.Target, out var beacon))
        {
            return;
        }

        // TODO: Check if it's a fulton-valid target.
        if (!CanFulton(args.Target.Value, uid, component))
        {
            return;
        }

        if (TryComp<FultonedComponent>(args.Target, out var fultoned))
        {
            return;
        }

        args.Handled = true;
        fultoned = AddComp<FultonedComponent>(args.Target.Value);
        fultoned.Beacon = component.Beacon;
        fultoned.NextFulton = Timing.CurTime + FultonDuration;
        UpdateAppearance(uid, fultoned);
        Dirty(uid, fultoned);
        // TODO: BEEP
    }

    protected virtual void UpdateAppearance(EntityUid uid, FultonedComponent fultoned)
    {
        return;
    }

    private bool CanFulton(EntityUid targetUid, EntityUid uid, FultonComponent component)
    {
        return true;
    }
}
