using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Tools.Systems;
using Robust.Shared.Random;

namespace Content.Server.Construction;

public sealed class RefiningSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WelderRefinableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<WelderRefinableComponent, WelderRefineDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(EntityUid uid, WelderRefinableComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _toolSystem.UseTool(
            args.Used,
            args.User,
            uid,
            component.RefineTime,
            component.QualityNeeded,
            new WelderRefineDoAfterEvent(),
            fuel: component.RefineFuel);
    }

    private void OnDoAfter(EntityUid uid, WelderRefinableComponent component, WelderRefineDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var xform = Transform(uid);
        var spawns = EntitySpawnCollection.GetSpawns(component.RefineResult, _random);
        foreach (var spawn in spawns)
        {
            SpawnNextToOrDrop(spawn, uid, xform);
        }

        Del(uid);
    }
}
