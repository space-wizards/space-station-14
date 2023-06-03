using Content.Server.Destructible;
using Content.Server.Gatherable.Components;
using Content.Shared.DoAfter;
using Content.Shared.EntityList;
using Content.Shared.Interaction;
using Content.Shared.Spaceshroom;
using Robust.Shared.Random;

namespace Content.Server.Gatherable;

public sealed partial class GatherableByHandSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherableByHandComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<GatherableByHandComponent, GatherableByHandDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractHand(EntityUid uid, GatherableByHandComponent component, InteractHandEvent args)
    {
        var doAfter = new DoAfterArgs(args.User, TimeSpan.FromSeconds(component.HarvestTime), new GatherableByHandDoAfterEvent(), uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.25f,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, GatherableByHandComponent component, GatherableByHandDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        Gather(uid, component);
        args.Handled = true;
    }

    private void Gather(EntityUid uid, GatherableByHandComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (component.Loot == null)
        {
            return;
        }

        var dropCount = _random.Next(component.MinDropCount, component.MaxDropCount + 1);
        var pos = Transform(uid).MapPosition;

        for (var i = 0; i < dropCount; i++)
        {
            var spawnPos = pos.Offset(_random.NextVector2(component.DropRadius));

            Spawn(component.Loot, spawnPos);
        }

        QueueDel(uid);
    }
}
