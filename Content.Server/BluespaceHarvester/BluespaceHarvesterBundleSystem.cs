using Content.Shared.Destructible;
using Content.Shared.Storage.Components;
using Robust.Shared.Random;

namespace Content.Server.BluespaceHarvester;

public sealed class BluespaceHarvesterBundleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceHarvesterBundleComponent, StorageBeforeOpenEvent>(OnOpen);
        SubscribeLocalEvent<BluespaceHarvesterBundleComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnOpen(Entity<BluespaceHarvesterBundleComponent> bundle, ref StorageBeforeOpenEvent args)
    {
        CreateLoot(bundle);
    }

    private void OnDestruction(Entity<BluespaceHarvesterBundleComponent> bundle, ref DestructionEventArgs args)
    {
        CreateLoot(bundle);
    }

    private void CreateLoot(Entity<BluespaceHarvesterBundleComponent> bundle)
    {
        if (bundle.Comp.Spawned)
            return;

        var content = _random.Pick(bundle.Comp.Contents);
        var position = Transform(bundle.Owner).Coordinates;

        for (var i = 0; i < content.Amount; i++)
        {
            Spawn(content.PrototypeId, position);
        }

        bundle.Comp.Spawned = true;
    }
}
