using Content.Shared.Destructible;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.BluespaceHarvester;

public sealed partial class BluespaceHarvesterBundleSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceHarvesterBundleComponent, StorageBeforeOpenEvent>(OnOpen);
        SubscribeLocalEvent<BluespaceHarvesterBundleComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnOpen(EntityUid uid, BluespaceHarvesterBundleComponent component, StorageBeforeOpenEvent args)
    {
        CreateLoot(uid, component);
    }

    private void OnDestruction(EntityUid uid, BluespaceHarvesterBundleComponent component, DestructionEventArgs args)
    {
        CreateLoot(uid, component);
    }

    private void CreateLoot(EntityUid uid, BluespaceHarvesterBundleComponent component)
    {
        if (component.Spawned)
            return;

        var content = _random.Pick(component.Contents);
        var xfrom = Transform(uid);
        var position = xfrom.Coordinates;

        for (var i = 0; i < content.Amount; i++)
        {
            Spawn(content.PrototypeId, position);
        }

        component.Spawned = true;
    }
}
