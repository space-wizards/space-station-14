using System.Threading;
using Content.Server.Popups;
using Content.Server.Speech;
using Content.Shared.LegallyDistinctSpaceFerret;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.LegallyDistinctSpaceFerret;

public struct TooCloseToLDSFEvent(EntityUid target, EntityUid cause, float afflictionRadius)
{
    public EntityUid Cause = cause;
    public EntityUid Target = target;
    public float AfflictionRadius = afflictionRadius;
}

public sealed class BrainrotSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TooCloseToLDSFEvent>(OnTooCloseToLDSF);
        SubscribeLocalEvent<BrainrotComponent, AccentGetEvent>(ApplyBrainRot);
    }

    private void OnTooCloseToLDSF(TooCloseToLDSFEvent args)
    {
        if (HasComp<BrainrotComponent>(args.Target))
        {
            return;
        }

        var comp = EnsureComp<BrainrotComponent>(args.Target);

        var cancel = new CancellationTokenSource();

        Timer.SpawnRepeating(TimeSpan.FromSeconds(comp.Duration), () =>
        {
            var mobs = new HashSet<Entity<LegallyDistinctSpaceFerretComponent>>();
            _lookup.GetEntitiesInRange(Transform(args.Target).Coordinates, args.AfflictionRadius, mobs);

            if (mobs.Count > 0)
                return;

            RemComp<BrainrotComponent>(args.Target);
            _popup.PopupEntity(Loc.GetString(comp.BrainRotLost), args.Target, args.Target);

            cancel.Cancel();
        }, cancel.Token);

        _popup.PopupEntity(Loc.GetString(comp.BrainRotApplied), args.Target, args.Target);
    }

    private void ApplyBrainRot(EntityUid entity, BrainrotComponent comp, AccentGetEvent args)
    {
        args.Message = Loc.GetString(_random.Pick(comp.BrainRotReplacementStrings));
    }
}
