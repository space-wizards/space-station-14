using Content.Client.Examine;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Power.Generation.Teg;

/// <summary>
/// Handles client-side logic for the thermo-electric generator (TEG).
/// </summary>
/// <remarks>
/// <para>
/// TEG circulators show which direction the in- and outlet port is by popping up two floating arrows when examined.
/// </para>
/// </remarks>
/// <seealso cref="TegCirculatorComponent"/>
/// <seealso cref="TegCirculatorArrowComponent"/>
public sealed class TegSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TegCirculatorComponent, ClientExaminedEvent>(CirculatorExamined);

        SubscribeLocalEvent<TegCirculatorArrowComponent, ComponentStartup>(ArrowStartup);
        SubscribeLocalEvent<TegCirculatorArrowComponent, ExamineAttemptEvent>(ArrowExamineAttempt);
    }

    private static void ArrowExamineAttempt(EntityUid uid, TegCirculatorArrowComponent component, ExamineAttemptEvent args)
    {
        // Avoid showing in right-click menu.
        args.Cancel();
    }

    private void ArrowStartup(EntityUid uid, TegCirculatorArrowComponent component, ComponentStartup args)
    {
        component.DestroyTime = _gameTiming.CurTime + TimeSpan.FromSeconds(2);
    }

    private void CirculatorExamined(EntityUid uid, TegCirculatorComponent component, ClientExaminedEvent args)
    {
        Spawn("TegCirculatorArrow", new EntityCoordinates(uid, 0, 0));
    }

    public override void Update(float frameTime)
    {
        var query = AllEntityQuery<TegCirculatorArrowComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.DestroyTime < _gameTiming.CurTime)
                QueueDel(uid);
        }
    }
}
