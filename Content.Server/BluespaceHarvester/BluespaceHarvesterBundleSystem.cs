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
    }

    private void OnOpen(EntityUid uid, BluespaceHarvesterBundleComponent component, StorageBeforeOpenEvent args)
    {
        var content = _random.Pick(component.Contents);
        var xfrom = Transform(uid);
        var position = xfrom.Coordinates;

        for (var i = 0; i < content.Amount; i++)
        {
            Spawn(content.PrototypeId, position);
        }

        RemComp<BluespaceHarvesterBundleComponent>(uid);
    }
}
