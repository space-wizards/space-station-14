using System.Threading;
using Content.Server.Popups;
using Content.Server.Speech;
using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.LegallyDistinctSpaceFerret;

/// <summary>
/// Raised locally when a mind gets too close to a Brainrot-causer.
/// </summary>
/// <param name="target">Who got brainrot</param>
/// <param name="cause">Who caused brainrot</param>
/// <param name="time">How long will they have it for</param>
public struct EntityGivenBrainrotEvent(EntityUid target, EntityUid cause, float time)
{
    public EntityUid Cause = cause;
    public EntityUid Target = target;
    public float Time = time;
}

/// <summary>
/// Raised locally when a mind is freed from brainrot
/// </summary>
/// <param name="target">Who had brainrot</param>
public struct EntityLostBrainrotEvent(EntityUid target)
{
    public EntityUid Target = target;
}

public sealed class BrainrotSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public const string BrainrotFixture = "brainrot";
    public const string BrainRotApplied = "brainrot-applied";
    public const string BrainRotLost = "brainrot-lost";
    public readonly string[] BrainRotReplacementStrings =
    [
        "brainrot-replacement-1",
        "brainrot-replacement-2",
        "brainrot-replacement-3",
        "brainrot-replacement-4",
        "brainrot-replacement-5",
        "brainrot-replacement-6"
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainrotAuraComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BrainrotAuraComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BrainrotAuraComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<EntityGivenBrainrotEvent>(EntityGivenBrainrot);
        SubscribeLocalEvent<EntityLostBrainrotEvent>(EntityLostBrainrot);
        SubscribeLocalEvent<BrainrotComponent, AccentGetEvent>(ApplyBrainRot);
    }

    private void OnStartup(EntityUid uid, BrainrotAuraComponent component, ComponentStartup args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var body))
            return;

        _fixtures.TryCreateFixture(
            uid,
            new PhysShapeCircle(component.Range),
            BrainrotFixture,
            collisionLayer: (int) CollisionGroup.None,
            collisionMask: (int) CollisionGroup.MidImpassable,
            hard: false,
            body: body
        );
    }

    private void OnShutdown(EntityUid uid, BrainrotAuraComponent component, ComponentShutdown args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var body) ||
            MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
        {
            return;
        }

        _fixtures.DestroyFixture(uid, BrainrotFixture, body: body);
    }

    private void OnCollide(EntityUid uid, BrainrotAuraComponent component, ref StartCollideEvent args)
    {
        // No effect if the mob is mindless
        if (!_mind.TryGetMind(args.OtherEntity, out _, out _))
        {
            return;
        }

        // No effect if the mob has brainrot or causes brainrot
        if (HasComp<BrainrotAuraComponent>(args.OtherEntity) || HasComp<BrainrotComponent>(args.OtherEntity))
        {
            return;
        }

        RaiseLocalEvent(new EntityGivenBrainrotEvent(args.OtherEntity, uid, component.Time));
    }

    private void EntityGivenBrainrot(EntityGivenBrainrotEvent args)
    {
        if (TryComp<BrainrotComponent>(args.Target, out var comp))
        {
            comp.Duration += args.Time;
        }
        else
        {
            comp = EnsureComp<BrainrotComponent>(args.Target);
        }

        var cancel = new CancellationTokenSource();

        Timer.Spawn(TimeSpan.FromSeconds(comp.Duration), () =>
        {
            comp.Duration -= args.Time;

            if (comp.Duration <= 0.0f)
            {
                RaiseLocalEvent(new EntityLostBrainrotEvent(args.Target));
            }
        }, cancel.Token);

        comp.Cancels.Add(cancel);

        _popup.PopupEntity(Loc.GetString(BrainRotApplied), args.Target, args.Target);
    }

    private void EntityLostBrainrot(EntityLostBrainrotEvent args)
    {
        if (TryComp<BrainrotComponent>(args.Target, out var comp))
        {
            foreach (var cancel in comp.Cancels)
            {
                cancel.Cancel();
            }
        }

        RemComp<BrainrotComponent>(args.Target);
        _popup.PopupEntity(Loc.GetString(BrainRotLost), args.Target, args.Target);
    }

    private void ApplyBrainRot(EntityUid entity, BrainrotComponent comp, AccentGetEvent args)
    {
        args.Message = Loc.GetString(_random.Pick(BrainRotReplacementStrings));
    }
}
