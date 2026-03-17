using Content.Server.Vocalization.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <inheritdoc cref="DatasetVocalizerComponent"/>
public sealed class DatasetVocalizationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DatasetVocalizerComponent, TryVocalizeEvent>(OnTryVocalize);
    }

    private void OnTryVocalize(Entity<DatasetVocalizerComponent> ent, ref TryVocalizeEvent args)
    {
        if (args.Handled)
            return;

        var dataset = _protoMan.Index(ent.Comp.Dataset);

        args.Message = _random.Pick(dataset);
        args.Handled = true;
    }
}
