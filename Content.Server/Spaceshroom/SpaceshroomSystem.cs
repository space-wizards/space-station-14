using Content.Server.Construction.Completions;
using Content.Server.Destructible;
using Content.Server.Mining.Components;
using Content.Server.Spaceshroom.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Spaceshroom;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Spaceshroom;

public sealed partial class SpaceshroomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceshroomComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SpaceshroomComponent, SpaceshroomDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractHand(EntityUid uid, SpaceshroomComponent component, InteractHandEvent args)
    {
        var doAfter = new DoAfterArgs(args.User, TimeSpan.FromSeconds(component.HarvestTime), new SpaceshroomDoAfterEvent(), uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BlockDuplicate = true,
            RequireCanInteract = true,
            MovementThreshold = 0.25f,
        };

        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, SpaceshroomComponent component, SpaceshroomDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled) return;

        var dropCount = _random.Next(component.MinDropCount, component.MaxDropCount + 1);

        for (var i = 0; i < dropCount; i++)
        {
            var pos = Transform(uid).MapPosition;
            var spawnPos = pos.Offset(_random.NextVector2(component.DropRadius));
            Spawn("FoodSpaceshroom", spawnPos);
        }

        _destructible.DestroyEntity(uid);
        args.Handled = true;
    }
}
